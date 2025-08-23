using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Diagnostics;

/// <summary>
/// Centralized diagnostic reporting for the DynamoDB generator.
/// </summary>
public class DiagnosticReporter
{
    private readonly Action<Diagnostic> _reportDiagnostic;
    
    public DiagnosticReporter(Action<Diagnostic> reportDiagnostic)
    {
        _reportDiagnostic = reportDiagnostic;
    }
    
    public void ReportTooManyGSI(string typeName, int count, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.TooManyGSIAttributes,
            location ?? Location.None,
            typeName,
            count);
        _reportDiagnostic(diagnostic);
    }
    
    public void ReportInvalidPKPattern(string typeName, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.InvalidPKPattern,
            location ?? Location.None,
            typeName);
        _reportDiagnostic(diagnostic);
    }
    
    public void ReportInvalidSKPattern(string typeName, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.InvalidSKPattern,
            location ?? Location.None,
            typeName);
        _reportDiagnostic(diagnostic);
    }
    
    public void ReportPropertyNotFound(string propertyName, string pattern, string typeName, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.PropertyNotFound,
            location ?? Location.None,
            propertyName,
            pattern,
            typeName);
        _reportDiagnostic(diagnostic);
    }
    
    public void ReportInvalidUnixTimestampUsage(string propertyName, string typeName, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.InvalidUnixTimestampUsage,
            location ?? Location.None,
            propertyName,
            typeName);
        _reportDiagnostic(diagnostic);
    }
    
    public void ReportCodeGenerationError(string message, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.CodeGenerationError,
            location ?? Location.None,
            message);
        _reportDiagnostic(diagnostic);
    }
}