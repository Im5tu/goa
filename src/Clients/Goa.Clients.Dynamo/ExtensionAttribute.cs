namespace Goa.Clients.Dynamo;

/// <summary>
/// Marks a DynamoDB model to generate a ToDynamoRecord() extension method.
/// When applied to a class with [DynamoModel], a ToDynamoRecord() extension method
/// will be generated that delegates to the DynamoMapper.
/// </summary>
/// <remarks>
/// This attribute can also be enabled globally for all [DynamoModel] types by setting
/// the MSBuild property &lt;GoaAutoGenerateExtensions&gt;true&lt;/GoaAutoGenerateExtensions&gt;
/// in your project file.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ExtensionAttribute : Attribute
{
}
