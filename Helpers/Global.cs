using System.Reflection;

namespace ExcelPasteTool;

// This class provides global constants and utility methods for the application.
public static class Global
{
    public static readonly string AssemblyName =
        Assembly.GetEntryAssembly()?.GetName().Name ?? "App";
}