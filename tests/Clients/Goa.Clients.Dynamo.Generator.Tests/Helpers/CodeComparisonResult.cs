namespace Goa.Clients.Dynamo.Generator.Tests.Helpers;

/// <summary>
/// Result of comparing two pieces of code.
/// </summary>
public class CodeComparisonResult
{
    public bool IsEqual { get; set; }
    public string? Context { get; set; }
    public string? ExpectedCode { get; set; }
    public string? ActualCode { get; set; }
    public List<string> Differences { get; set; } = new();
    
    public string GetDifferenceReport()
    {
        var report = new List<string>();
        
        if (!string.IsNullOrEmpty(Context))
            report.Add($"Context: {Context}");
            
        if (IsEqual)
        {
            report.Add("✅ Code matches expected output");
        }
        else
        {
            report.Add("❌ Code does not match expected output");
            report.Add("");
            report.Add("Differences:");
            report.AddRange(Differences);
            
            if (!string.IsNullOrEmpty(ExpectedCode) && !string.IsNullOrEmpty(ActualCode))
            {
                report.Add("");
                report.Add("Expected:");
                report.Add(ExpectedCode);
                report.Add("");
                report.Add("Actual:");
                report.Add(ActualCode);
            }
        }
        
        return string.Join("\n", report);
    }
}