using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

// Class with parameterless constructor and property setters
[DynamoModel(PK = "PRODUCT#<Id>", SK = "INFO")]
public class ProductInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ProductCategory Category { get; set; }
    public List<string> Tags { get; set; } = new();
}

// Class with constructor parameters

// Abstract base class to test inheritance

// Concrete implementations