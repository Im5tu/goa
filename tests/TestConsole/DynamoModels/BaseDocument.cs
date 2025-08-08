namespace TestConsole.DynamoModels;

public abstract class BaseDocument
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
}