# 專案說明
ExcelPasteTool 是一個用於快速處理 Excel 資料的桌面應用處理工具，支援多種資料格式的貼上與轉換，並提供直觀的使用者介面。

## 發布檔案指令
```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# macOS ARM64 (M1/M2)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```


## 一鍵發布（有結構輸出）
建議使用發佈設定檔，輸出到 artifacts 目錄，區分平台：

- Windows x64 -> artifacts/win-x64/
- Linux x64 -> artifacts/linux-x64/
- macOS x64 -> artifacts/osx-x64/
- macOS ARM64 (M1/M2/M3) -> artifacts/osx-arm64/

指令：
```bash
# Windows x64
 dotnet publish -c Release -p:PublishProfile=Properties/PublishProfiles/Windows-x64.pubxml

# Linux x64
 dotnet publish -c Release -p:PublishProfile=Properties/PublishProfiles/Linux-x64.pubxml

# macOS x64
 dotnet publish -c Release -p:PublishProfile=Properties/PublishProfiles/macOS-x64.pubxml

# macOS ARM64 (M1/M2/M3)
 dotnet publish -c Release -p:PublishProfile=Properties/PublishProfiles/macOS-arm64.pubxml
```


## 專案目錄結構
```
ExcelPasteTool/
│
├── App.axaml                   # App 主入口（樣式、資源字典）
├── App.axaml.cs                # App 主程式碼（初始化、資源設定）
├── config.json                 # 設定檔（儲存使用者設定、偏好等）
├── Directory.Packages.props    # NuGet 套件管理設定（版本鎖定）
├── ExcelPasteTool.csproj       # 專案檔
├── global.json                 # .NET SDK 指定版本設定
├── Program.cs                  # 程式入口
├── README.md                   # 專案說明文件
│
├── 📂 Assets/                  # 靜態資源（會在 build 時一併打包）
│   ├── 📂 Images/             # 一般圖片 (png/jpg/svg)
│   ├── 📂 Icons/              # App icon / Toolbar icon / 系統托盤圖示
│   ├── 📂 Logos/              # 專案 LOGO 或品牌相關圖
│   │   └── logo.ico            # 應用程式圖示
│   ├── 📂 Fonts/              # 自訂字型檔
│   └── 📂 Styles/             # 全局樣式（Theme.axaml、字型設定等）
│       ├── DarkTheme.axaml     # 黑暗主題樣式
│       ├── LightTheme.axaml    # 光線主題樣式
│       └── SidebarTheme.axaml  # 側邊欄主題樣式
│
├── 📂 Controls/                # 自訂可重用控件（UserControl）
│   └── ...
│
├── 📂 Views/                   # 畫面（View）定義
│   ├── MainWindow.axaml        # 主視窗框架定義UI
│   ├── MainWindow.axaml.cs    # 主視窗程式碼
│   ├── DataToolView.axaml     # 資料處理工具視窗UI
│   ├── DataToolView.axaml.cs  # 資料處理工具視窗程式碼
│   ├── SettingsView.axaml     # 設定畫面UI
│   └── SettingsView.axaml.cs  # 設定畫面UI與程式碼
│
├── 📂 ViewModels/              # 畫面邏輯（ViewModel）
│   ├── SettingsViewModel.cs    # 設定畫面邏輯
│   └── SidebarViewModel.cs     # 側邊欄畫面邏輯
│
├── 📂 Models/                  # 資料模型（Model）
│   ├── DataItem.cs            # 資料項目模型
│   ├── [SidebarItem.cs](docs/SidebarItem.md)         # 側邊欄項目模型
│   └── ...
│
├── 📂 Services/                # 後端服務（存取資料庫、API、設定檔等）
│   ├── [ConfigServices.cs](docs/ConfigService.md)      # 設定服務（讀取/寫入 config.json）
│   └── ...
│
├── 📂 Converters/              # IValueConverter 與 Binding 轉換器
│   ├── SidebarConverters.cs
│   ├── SidebarPageToContentConverter.cs
│   ├── EvenRowBackgroundConverter.cs
│   └── ...
│
├── 📂 Helpers/                  # 工具類（靜態輔助方法）
│   ├── FontManager.cs          # 字型管理
│   ├── Global.cs               # 全域靜態方法與常數
│   ├── IconManager.cs          # 圖示管理
│   ├── LanguageController.cs   # 語言控制器
│   ├── LanguageManager.cs      # 語言管理
│   ├── ThemeManager.cs         # 主題管理
│   ├── ToastQueueHelper.cs     # 提示訊息佇列
│   └── ...
│
├── 📂 Resources/               # 資源字串（多語系、文字等）
│   └── 📂 Languages/           # 語言包
│       ├── en.json             # 英文語言包
│       ├── zh-TW.json          # 繁體中文語言包
│       └── zh-CN.json          # 簡體中文語言包
│
└── 📂 Properties/              # 專案屬性
    └── Resources.Designer.cs    # 資源設計器（自動生成）