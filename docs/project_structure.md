# 專案結構說明

倉庫採用 .NET 慣例的 `src/` + `tests/` 配置：主程式在 `src/ExcelPasteTool/`，測試在 `tests/`，打包腳本在 `packaging/`，根目錄只放方案檔與共用設定（`Directory.Packages.props`、`global.json` 由 MSBuild 與 dotnet 自動往上層找到）。

```text
ExcelPasteTool/                     # 倉庫根目錄
├── ExcelPasteTool.sln              # 方案檔（主程式＋兩個測試專案）
├── Directory.Packages.props        # NuGet 套件版本集中管理
├── global.json                     # 指定 .NET SDK 版本
├── README.md、README.en.md、AI_CONTEXT.md
│
├── src/ExcelPasteTool/             # 主程式專案（組件名稱 DataSplitter）
│   ├── ExcelPasteTool.csproj       # 專案檔（版本號在這裡）
│   ├── Program.cs                  # 程式進入點
│   ├── App.axaml(.cs)              # 應用程式初始化（載入設定、主題、字型）
│   ├── Views/                      # 畫面（主視窗、資料工具、設定、關於、說明、更新、改版資訊、重複項）
│   ├── ViewModels/                 # 設定頁與側邊欄的畫面邏輯
│   ├── Models/                     # 資料模型（DataItem、SidebarItem）
│   ├── Services/                   # 設定檔、路徑、版本檢查與自動更新、說明、改版資訊
│   ├── Helpers/                    # 主題、語言、字型、圖示、Toast、貼上與分隔處理
│   ├── Controls/                   # 自訂控制項（導覽遮罩 TourOverlay）
│   ├── Converters/                 # Binding 轉換器
│   ├── Assets/                     # Themes、Styles、Fonts（Noto Sans TC 一般／粗體）、Help、Images、Logos
│   ├── Resources/                  # Languages/*.json、Changelog.json
│   └── Properties/                 # Resources.resx、PublishProfiles/（四平台發佈設定，VS 與 CI 共用）
│
├── tests/
│   ├── ExcelPasteTool.Tests/       # 單元測試（xUnit）
│   ├── ExcelPasteTool.UITests/     # Avalonia Headless 畫面測試與基準圖
│   └── run-ui-tests.ps1            # UI 測試一鍵執行與報告
│
├── packaging/
│   ├── publish.sh                  # 單一平台發佈＋壓 zip（release.yml 呼叫）
│   ├── config.json                 # 預設設定範本（打包時放進 zip）
│   └── macos/                      # .app 的 Info.plist、圖示、相容啟動腳本
│
├── docs/                           # 補充文件
└── .github/workflows/              # build.yml（建置＋單元測試）、release.yml（打包發佈）
```

## 常用指令（都在倉庫根目錄執行）

| 目的 | 指令 |
| --- | --- |
| 建置整個方案 | `dotnet build` |
| 執行程式 | `dotnet run --project src/ExcelPasteTool` |
| 單元測試 | `dotnet test tests/ExcelPasteTool.Tests` |
| UI 畫面測試 | `pwsh tests/run-ui-tests.ps1` |
| 發佈單一平台 | `dotnet publish src/ExcelPasteTool -c Release -p:PublishProfile=win-x64` |
| 打包單一平台 zip | `bash packaging/publish.sh win-x64 1.7.0` |

## 搬檔時要一起改的地方

- `ExcelPasteTool.sln` 的專案路徑。
- 測試專案的 `ProjectReference`，以及 `ExcelPasteTool.Tests.csproj` 直接讀取的 `Changelog.json` 與 `ExcelPasteTool.csproj`。
- `tests/ExcelPasteTool.UITests/Infrastructure/UiTestPaths.cs` 以 `ExcelPasteTool.sln` 定位倉庫根目錄。
- `packaging/publish.sh` 的專案路徑、`bin/` 路徑與 `config.json` 路徑，以及 `.github/workflows/*.yml` 的專案路徑。
- `avares://DataSplitter/...` 資源路徑相對於主程式專案資料夾，只要資料夾內的結構不變就不用改。
