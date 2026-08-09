namespace LegacyModernization.Analyzer.Models;

/// <summary>
/// Holds simple information about a class and its methods.
/// </summary>
public class ClassInfo
{
    public string Name { get; set; } = "";

    public string Namespace { get; set; } = "";

    public List<MethodInfo> Methods { get; set; } = new();
}
