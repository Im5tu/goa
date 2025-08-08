using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "ORDER#<OrderId>", SK = "ITEM#<ItemId>")]
[GlobalSecondaryIndex(Name = "ProductIndex", PK = "PRODUCT#<ProductId>", SK = "ORDER#<OrderId>")]
public class OrderItem
{
    public string OrderId { get; }
    public string ItemId { get; }
    public string ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal TotalPrice { get; }

    public OrderItem(string orderId, string itemId, string productId, string productName, 
        int quantity, decimal unitPrice, decimal totalPrice)
    {
        OrderId = orderId;
        ItemId = itemId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalPrice = totalPrice;
    }
}