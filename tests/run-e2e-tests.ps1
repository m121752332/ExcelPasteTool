<#
.SYNOPSIS
  端到端 smoke 測試一鍵執行：建置 → 用 FlaUI 啟動真實 DataSplitter.exe → 點按鈕 / 讀值 / 截圖 → 結構化報告。

.DESCRIPTION
  給 AI Agent 在 Windows 本機除錯用（會開出真實視窗、暫時改寫剪貼簿，測完會還原）。
  執行完一律產生 TestResults/e2e/summary.json，結束代碼：
    0 = 全部通過（或因非 Windows 而略過，summary.json 的 status 會是 skipped）
    1 = 有測試失敗
    2 = 建置失敗（summary.json 的 build.errors 會列出檔案、行號與錯誤訊息）

  產出物（TestResults/ 已在 .gitignore）：
    TestResults/e2e/summary.json                 彙整結果（AI 先讀這個）
    TestResults/e2e/e2e-tests.trx                原始 TRX
    TestResults/e2e/<測試名稱>/summary.json       該測試每一步的狀態、耗時、錯誤
    TestResults/e2e/<測試名稱>/steps.log          同上，純文字
    TestResults/e2e/<測試名稱>/screenshots/*.png  依序編號的截圖（失敗時多一張 NN-failure.png）
    TestResults/e2e/<測試名稱>/trees/*.txt        UI Automation 控制項樹
    TestResults/e2e/<測試名稱>/app.log            程式本身的 Trace 與未處理例外
    TestResults/e2e/<測試名稱>/exception.txt      失敗時的完整例外
    TestResults/e2e/_app/                        測試用的程式副本（保留給下次增量複製）

.EXAMPLE
  pwsh tests/run-e2e-tests.ps1
.EXAMPLE
  pwsh tests/run-e2e-tests.ps1 -Filter "FullyQualifiedName~SmokeTests"
#>
param(
    [string]$Filter = "",
    [string]$Configuration = "Debug"
)

# Windows PowerShell 5.1 會把原生程式寫到 stderr 的內容當成錯誤；這裡一律靠結束代碼判斷
$ErrorActionPreference = "Continue"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "tests/ExcelPasteTool.E2ETests/ExcelPasteTool.E2ETests.csproj"
$resultsDir = Join-Path $repoRoot "TestResults/e2e"
$summaryPath = Join-Path $resultsDir "summary.json"
$utf8 = New-Object System.Text.UTF8Encoding($false)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$env:DOTNET_CLI_UI_LANGUAGE = "en"   # 讓錯誤格式固定，方便解析
$env:DOTNET_NOLOGO = "1"
$env:E2E_RESULTS_DIR = $resultsDir
# 這支腳本是明確要跑端到端測試，就算環境變數裡有 CI 也照跑（需要互動桌面）
$env:DATASPLITTER_E2E = "1"

# 清掉上次的結果，但保留 _app（程式副本，下次只複製有變動的檔案）
if (Test-Path $resultsDir) {
    Get-ChildItem $resultsDir -Force | Where-Object { $_.Name -ne "_app" } | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $resultsDir | Out-Null
}

function Write-Summary($summary) {
    $json = $summary | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($summaryPath, $json, $utf8)
}

function Get-RelativePath([string]$path) {
    if (-not $path) { return $null }
    $full = [System.IO.Path]::GetFullPath($path)
    if ($full.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
    }
    return $full
}

$started = Get-Date

if (-not $IsWindows -and $PSVersionTable.PSEdition -eq "Core") {
    Write-Summary ([ordered]@{ status = "skipped"; exitCode = 0; reason = "端到端測試只在 Windows 執行（FlaUI / UI Automation）" })
    Write-Host "略過：端到端測試只在 Windows 執行" -ForegroundColor Yellow
    exit 0
}

# ---------- 1. 建置（會一併建置主程式） ----------
Write-Host "[1/3] 建置端到端測試專案與主程式 ($Configuration)..."
$buildOutput = & dotnet build $project -c $Configuration -nologo -v q "-clp:NoSummary" 2>&1 | ForEach-Object { "$_" }
$buildExit = $LASTEXITCODE

$pattern = '^\s*(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s*(?<sev>error|warning)\s+(?<code>[A-Z]+\d+):\s*(?<msg>.+?)(\s+\[.+\])?$'
$diagnostics = @($buildOutput | ForEach-Object {
    if ($_ -match $pattern) {
        [ordered]@{
            severity = $Matches.sev; code = $Matches.code
            file = Get-RelativePath $Matches.file; line = [int]$Matches.line; column = [int]$Matches.col
            message = $Matches.msg.Trim()
        }
    }
} | Sort-Object { "$($_.severity)|$($_.file)|$($_.line)|$($_.code)" } -Unique)
$buildErrors = @($diagnostics | Where-Object { $_.severity -eq "error" })

if ($buildExit -ne 0) {
    Write-Summary ([ordered]@{
        status = "build-failed"; exitCode = 2
        startedAt = $started.ToString("o"); durationSeconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
        build = [ordered]@{ succeeded = $false; errors = $buildErrors; rawTail = @($buildOutput | Select-Object -Last 30) }
        tests = $null; runs = @()
    })
    Write-Host "建置失敗（$($buildErrors.Count) 個錯誤），詳見 $summaryPath" -ForegroundColor Red
    $buildErrors | ForEach-Object { Write-Host "  $($_.file)($($_.line)): $($_.code) $($_.message)" }
    exit 2
}

# ---------- 2. 測試 ----------
Write-Host "[2/3] 啟動真實程式執行端到端測試（測試期間請不要操作滑鼠鍵盤）..."
$testArgs = @("test", $project, "-c", $Configuration, "--no-build", "--nologo",
    "--logger", "trx;LogFileName=e2e-tests.trx", "--results-directory", $resultsDir)
if ($Filter) { $testArgs += @("--filter", $Filter) }
$testOutput = & dotnet @testArgs 2>&1 | ForEach-Object { "$_" }
$testExit = $LASTEXITCODE

# ---------- 3. 彙整報告 ----------
Write-Host "[3/3] 產生結構化報告..."
$trxPath = Join-Path $resultsDir "e2e-tests.trx"
$tests = [ordered]@{ total = 0; passed = 0; failed = 0; skipped = 0; failures = @(); trx = $null }
if (Test-Path $trxPath) {
    [xml]$trx = Get-Content $trxPath -Raw -Encoding UTF8
    $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
    $ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
    $results = @($trx.SelectNodes("//t:UnitTestResult", $ns))
    $tests.total = $results.Count
    $tests.passed = @($results | Where-Object { $_.outcome -eq "Passed" }).Count
    $tests.failed = @($results | Where-Object { $_.outcome -eq "Failed" }).Count
    $tests.skipped = $tests.total - $tests.passed - $tests.failed
    $tests.trx = Get-RelativePath $trxPath
    $tests.failures = @($results | Where-Object { $_.outcome -eq "Failed" } | ForEach-Object {
        $err = $_.SelectSingleNode("t:Output/t:ErrorInfo", $ns)
        [ordered]@{
            name = $_.testName
            duration = $_.duration
            message = if ($err -and $err.Message) { "$($err.Message)".Trim() } else { $null }
            stackTrace = if ($err -and $err.StackTrace) { (("$($err.StackTrace)" -split "`n") | Select-Object -First 8 | ForEach-Object { $_.Trim() }) -join "`n" } else { $null }
        }
    })
}

# 每個測試自己的 summary.json（步驟、截圖、控制項樹、app.log）
$runs = @(Get-ChildItem $resultsDir -Directory | Where-Object { $_.Name -ne "_app" } | Sort-Object Name | ForEach-Object {
    $file = Join-Path $_.FullName "summary.json"
    if (Test-Path $file) { Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json }
} | Where-Object { $_ })

$status = if ($testExit -eq 0) { "passed" } else { "failed" }
$exitCode = if ($testExit -eq 0) { 0 } else { 1 }
if ($testExit -ne 0 -and $tests.total -eq 0) { $status = "test-run-error" }

Write-Summary ([ordered]@{
    status = $status; exitCode = $exitCode
    startedAt = $started.ToString("o"); durationSeconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
    filter = $Filter
    build = [ordered]@{ succeeded = $true; errors = @(); warnings = @($diagnostics | Where-Object { $_.severity -eq "warning" }).Count }
    tests = $tests
    runs = $runs
    rawTail = if ($status -eq "test-run-error") { @($testOutput | Select-Object -Last 30) } else { @() }
})

$color = if ($exitCode -eq 0) { "Green" } else { "Red" }
Write-Host ("結果：{0}（通過 {1} / 失敗 {2} / 略過 {3}）" -f $status, $tests.passed, $tests.failed, $tests.skipped) -ForegroundColor $color
foreach ($f in $tests.failures) { Write-Host "  ✗ $($f.name)`n    $($f.message)" -ForegroundColor Red }
foreach ($r in $runs) { Write-Host "  $($r.status)  $($r.test) → $($r.outputDir)" }
Write-Host "報告：$summaryPath"
exit $exitCode
