<#
.SYNOPSIS
  UI 自動測試一鍵執行：建置 → Avalonia Headless 無畫面測試 → 截圖 PNG → 視覺比對 → 結構化報告。

.DESCRIPTION
  給 AI Agent 與 CI 用。執行完一律產生 TestResults/ui/summary.json，結束代碼：
    0 = 全部通過
    1 = 有測試失敗（含視覺比對失敗、缺基準圖）
    2 = 建置失敗（summary.json 的 build.errors 會列出檔案、行號與錯誤訊息）

  產出物（TestResults/ 已在 .gitignore）：
    TestResults/ui/summary.json   彙整結果（建置錯誤、失敗測試、視覺比對）
    TestResults/ui/ui-tests.trx   原始 TRX
    TestResults/ui/actual/*.png   本次擷取的畫面
    TestResults/ui/diff/*.png     比對失敗時的差異圖（紅色 = 不同的像素）
    TestResults/ui/visual/*.json  每張圖的比對數據

.EXAMPLE
  pwsh tests/run-ui-tests.ps1
.EXAMPLE
  pwsh tests/run-ui-tests.ps1 -Filter "FullyQualifiedName~ThemeVisualTests"
.EXAMPLE
  # 確認 UI 改動是預期的之後，重建基準圖
  pwsh tests/run-ui-tests.ps1 -UpdateBaselines
#>
param(
    [string]$Filter = "",
    [string]$Configuration = "Debug",
    [switch]$UpdateBaselines
)

# Windows PowerShell 5.1 會把原生程式寫到 stderr 的內容當成錯誤；這裡一律靠結束代碼判斷
$ErrorActionPreference = "Continue"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "tests/ExcelPasteTool.UITests/ExcelPasteTool.UITests.csproj"
$resultsDir = Join-Path $repoRoot "TestResults/ui"
$summaryPath = Join-Path $resultsDir "summary.json"
$utf8 = New-Object System.Text.UTF8Encoding($false)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$env:DOTNET_CLI_UI_LANGUAGE = "en"   # 讓錯誤格式固定，方便解析
$env:DOTNET_NOLOGO = "1"
$env:UI_TEST_RESULTS_DIR = $resultsDir
if ($UpdateBaselines) { $env:UPDATE_BASELINES = "1" } else { Remove-Item Env:UPDATE_BASELINES -ErrorAction SilentlyContinue }

if (Test-Path $resultsDir) { Remove-Item $resultsDir -Recurse -Force }
New-Item -ItemType Directory -Path $resultsDir | Out-Null

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

# ---------- 1. 建置 ----------
Write-Host "[1/3] 建置 UI 測試專案 ($Configuration)..."
$buildOutput = & dotnet build $project -c $Configuration -nologo -v q "-clp:NoSummary" 2>&1 | ForEach-Object { "$_" }
$buildExit = $LASTEXITCODE

# 例：C:\...\Views\Foo.axaml.cs(12,5): error CS0103: The name 'x' does not exist [C:\...\X.csproj]
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
    $summary = [ordered]@{
        status = "build-failed"; exitCode = 2
        startedAt = $started.ToString("o"); durationSeconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
        build = [ordered]@{ succeeded = $false; errors = $buildErrors; rawTail = @($buildOutput | Select-Object -Last 30) }
        tests = $null; visual = @()
    }
    Write-Summary $summary
    Write-Host "建置失敗（$($buildErrors.Count) 個錯誤），詳見 $summaryPath" -ForegroundColor Red
    $buildErrors | ForEach-Object { Write-Host "  $($_.file)($($_.line)): $($_.code) $($_.message)" }
    exit 2
}

# ---------- 2. 測試 ----------
Write-Host "[2/3] 執行 Headless UI 測試..."
$testArgs = @("test", $project, "-c", $Configuration, "--no-build", "--nologo",
    "--logger", "trx;LogFileName=ui-tests.trx", "--results-directory", $resultsDir)
if ($Filter) { $testArgs += @("--filter", $Filter) }
$testOutput = & dotnet @testArgs 2>&1 | ForEach-Object { "$_" }
$testExit = $LASTEXITCODE

# ---------- 3. 彙整報告 ----------
Write-Host "[3/3] 產生結構化報告..."
$trxPath = Join-Path $resultsDir "ui-tests.trx"
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

$visualDir = Join-Path $resultsDir "visual"
$visual = @()
if (Test-Path $visualDir) {
    $visual = @(Get-ChildItem $visualDir -Filter *.json | Sort-Object Name | ForEach-Object {
        Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    })
}

$status = if ($testExit -eq 0) { "passed" } else { "failed" }
$exitCode = if ($testExit -eq 0) { 0 } else { 1 }
if ($testExit -ne 0 -and $tests.total -eq 0) { $status = "test-run-error" }

$summary = [ordered]@{
    status = $status; exitCode = $exitCode
    startedAt = $started.ToString("o"); durationSeconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
    updateBaselines = [bool]$UpdateBaselines; filter = $Filter
    build = [ordered]@{ succeeded = $true; errors = @(); warnings = @($diagnostics | Where-Object { $_.severity -eq "warning" }).Count }
    tests = $tests
    visual = $visual
    rawTail = if ($status -eq "test-run-error") { @($testOutput | Select-Object -Last 30) } else { @() }
}
Write-Summary $summary

$color = if ($exitCode -eq 0) { "Green" } else { "Red" }
Write-Host ("結果：{0}（通過 {1} / 失敗 {2} / 略過 {3}，視覺比對 {4} 張）" -f $status, $tests.passed, $tests.failed, $tests.skipped, $visual.Count) -ForegroundColor $color
foreach ($f in $tests.failures) { Write-Host "  ✗ $($f.name)`n    $($f.message)" -ForegroundColor Red }
Write-Host "報告：$summaryPath"
exit $exitCode
