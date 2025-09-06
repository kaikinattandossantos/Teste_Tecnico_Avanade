namespace SalesService.Models
     {
         public class OrderDTO
         {
             public string CustomerId { get; set; } = string.Empty;
             public string Status { get; set; } = string.Empty;
             public List<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();
         }

         public class OrderItemDTO
         {
             public int ProductId { get; set; }
             public int Quantity { get; set; }
         }
     }