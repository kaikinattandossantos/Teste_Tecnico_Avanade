namespace Shared
     {
         public class Product
         {
             public int Id { get; set; }
             public string? Name { get; set; } // Propriedade anulável
             public string? Description { get; set; } // Propriedade anulável
             public decimal Price { get; set; }
             public int QuantityInStock { get; set; }
         }
     }