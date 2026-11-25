// 每個測試都會啟動同一份 TestResults/e2e/_app 裡的程式（啟動前還會結束上次殘留的程序）、共用系統剪貼簿，
// 所以測試類別之間不能平行執行，否則後啟動的會把先啟動的程式關掉
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ExcelPasteTool.E2ETests.Infrastructure;

/// <summary>
/// 決定端到端測試要不要跑：會開真實視窗、動到剪貼簿，所以只在 Windows 本機跑。
///
/// - 非 Windows：跳過。
/// - CI（環境變數 CI / GITHUB_ACTIONS / TF_BUILD 有值）：預設跳過，CI 機器多半沒有可互動的桌面。
/// - <c>DATASPLITTER_E2E=1</c>：強制執行（例如在有桌面的自架 runner）。
/// - <c>DATASPLITTER_E2E=0</c>：強制跳過（例如本機跑整個方案的測試、不想跳出視窗時）。
/// </summary>
public static class E2EGuard
{
    public const string SwitchVariable = "DATASPLITTER_E2E";

    public static void SkipUnlessEnabled()
    {
        var reason = GetSkipReason();
        if (reason != null) Assert.Skip(reason);
    }

    public static string? GetSkipReason()
    {
        if (!OperatingSystem.IsWindows())
            return "端到端測試只在 Windows 執行（FlaUI / UI Automation）";

        var flag = Environment.GetEnvironmentVariable(SwitchVariable)?.Trim();
        if (flag == "0") return $"{SwitchVariable}=0，略過端到端測試";
        if (flag == "1") return null;

        foreach (var ci in new[] { "CI", "GITHUB_ACTIONS", "TF_BUILD" })
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ci)))
                return $"偵測到 CI（{ci}），端到端測試需要互動桌面，預設略過；要強制執行請設 {SwitchVariable}=1";
        }
        return null;
    }
}
