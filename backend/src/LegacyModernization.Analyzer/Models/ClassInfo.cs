namespace LegacyModernization.Analyzer.Models;

public class ClassInfo
{
    public string Name { get; set; } = "";

    public string Namespace { get; set; } = "";

    public List<MethodInfo> Methods { get; set; } = new();
}