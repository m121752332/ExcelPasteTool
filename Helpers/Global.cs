using System.Reflection;

namespace ExcelPasteTool;

// This class provides global constants and utility methods for the application.
public static class Global
{
    // 用本組件而不是 EntryAssembly：在 UI 自動測試裡 EntryAssembly 是 testhost，avares:// 路徑會找不到資源
    public static readonly string AssemblyName =
        typeof(Global).Assembly.GetName().Name ?? "App";
}