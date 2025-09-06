using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.Models;
using Shared;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SalesService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly SalesContext _context;
        private readonly RabbitMQProducer _producer;
        private readonly HttpClient _httpClient;

        public OrderController(SalesContext context, RabbitMQProducer producer, HttpClient httpClient)
        {
            _context = context;
            _producer = producer;
            _httpClient = httpClient;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDTO orderDTO)
        {
            Console.WriteLine("CreateOrder chamado com OrderDTO: " + JsonSerializer.Serialize(orderDTO));
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState inválido: " + JsonSerializer.Serialize(ModelState));
                return BadRequest(ModelState);
            }

            var order = new Order
            {
                CustomerId = orderDTO.CustomerId,
                Status = orderDTO.Status,
                OrderDate = DateTime.UtcNow,
                Items = orderDTO.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            foreach (var item in order.Items)
            {
                var response = await _httpClient.GetAsync($"http://localhost:5049/api/Product/{item.ProductId}");
                Console.WriteLine($"Resposta do StockService para ProductId {item.ProductId}: Status={response.StatusCode}, Content={await response.Content.ReadAsStringAsync()}");

                if (!response.IsSuccessStatusCode) return BadRequest("Produto não encontrado");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var product = JsonSerializer.Deserialize<Product>(await response.Content.ReadAsStringAsync(), options);
                if (product == null)
                {
                    Console.WriteLine("Falha ao deserializar o produto");
                    return BadRequest("Falha ao deserializar o produto");
                }
                Console.WriteLine($"Produto desserializado: Id={product.Id}, QuantityInStock={product.QuantityInStock}");
                if (product.QuantityInStock < item.Quantity)
                {
                    Console.WriteLine($"Estoque insuficiente: ProductId={item.ProductId}, QuantityInStock={product.QuantityInStock}, Requested={item.Quantity}");
                    return BadRequest($"Estoque insuficiente para o produto {item.ProductId}");
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in order.Items)
            {
                _producer.PublishMessage("stock_queue", $"Order {order.Id} created, reduce stock for product {item.ProductId} by {item.Quantity}");
            }

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders.Include(o => o.Items).ToListAsync();
            return Ok(orders);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderDTO orderDTO)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            order.CustomerId = orderDTO.CustomerId;
            order.Status = orderDTO.Status;
            order.OrderDate = DateTime.UtcNow; // Atualiza a data

            _context.OrderItems.RemoveRange(order.Items);
            order.Items = orderDTO.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList();

            foreach (var item in order.Items)
            {
                var response = await _httpClient.GetAsync($"http://localhost:5049/api/Product/{item.ProductId}");
                if (!response.IsSuccessStatusCode) return BadRequest("Produto não encontrado");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var product = JsonSerializer.Deserialize<Product>(await response.Content.ReadAsStringAsync(), options);
                if (product == null) return BadRequest("Falha ao deserializar o produto");
                if (product.QuantityInStock < item.Quantity)
                    return BadRequest($"Estoque insuficiente para o produto {item.ProductId}");
            }

            await _context.SaveChangesAsync();

            foreach (var item in order.Items)
            {
                _producer.PublishMessage("stock_queue", $"Order {order.Id} updated, reduce stock for product {item.ProductId} by {item.Quantity}");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            _producer.PublishMessage("stock_queue", $"Order {id} deleted");
            return NoContent();
        }

        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok("API SalesService funcionando!");
        }
    }
}

public class OrderDTO
{
    public string CustomerId { get; set; }
    public string Status { get; set; }
    public List<OrderItemDTO> Items { get; set; }
}

public class OrderItemDTO
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public int QuantityInStock { get; set; }
}