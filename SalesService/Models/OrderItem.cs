using System.Text.Json.Serialization;

     namespace SalesService.Models
     {
         public class OrderItem
         {
             public int Id { get; set; }
             public int OrderId { get; set; }
             public int ProductId { get; set; }
             public int Quantity { get; set; }
             [JsonIgnore]
             public Order Order { get; set; } = null!;
         }
     }