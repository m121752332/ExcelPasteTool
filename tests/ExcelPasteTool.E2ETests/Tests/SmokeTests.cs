using ExcelPasteTool.E2ETests.Infrastructure;

namespace ExcelPasteTool.E2ETests.Tests;

/// <summary>
/// 端到端 smoke 測試：啟動真實的 DataSplitter.exe，確認最核心的「資料貼上」流程能走完。
/// 每一步、截圖、控制項樹、程式記錄都在 TestResults/e2e/&lt;測試名稱&gt;/（說明見 docs/UI-TESTING.md）。
/// </summary>
[Trait("Category", "E2E")]
public class SmokeTests
{
    [Fact]
    public void Launch_PasteData_FillsTable()
    {
        E2EGuard.SkipUnlessEnabled();
        using var s = AppSession.Start(nameof(Launch_PasteData_FillsTable));

        s.Step("工具頁出現（找得到「資料貼上」按鈕）", () => s.Find("PasteDataButton"));
        s.Screenshot("launched");
        s.DumpTree("launched");

        // 三筆不重複 + 一筆重複：貼上後應該去重複、並標出重複筆數
        s.Step("把測試資料放進剪貼簿", () => s.SetClipboard("A01\r\nB02\r\nC03\r\nA01\r\n"));
        s.Step("點「資料貼上」", () => s.Invoke("PasteDataButton"));

        var top = s.Step("分隔資料出現貼上的內容", () =>
            s.WaitForText("TopBox", t => t.Contains("A01") && t.Contains("B02") && t.Contains("C03")));
        var sql = s.Step("SQL 條件同步產生", () =>
            s.WaitForText("BottomBox", t => t.Contains("'A01'") && t.Contains("'C03'")));
        s.Step("「重複資料」按鈕變成可以按", () =>
        {
            if (!s.Find("DuplicatesButton").IsEnabled)
                throw new InvalidOperationException("貼上含重複的資料後，DuplicatesButton 應該要啟用");
        });

        s.Screenshot("after-paste");
        s.DumpTree("after-paste");

        Assert.Equal(1, CountOccurrences(top, "A01"));
        Assert.Contains("A01", sql);

        s.Step("點「清除」回到初始狀態", () => s.Invoke("ClearDataButton"));
        s.Step("分隔資料被清空", () => s.WaitForText("TopBox", string.IsNullOrEmpty));
        s.Screenshot("after-clear");

        s.Complete();
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
