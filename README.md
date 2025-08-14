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


## 專案目錄結構
```
ExcelPasteTool/
│
├── config.json                 # 設定檔（儲存使用者設定、偏好等）
├── App.axaml                   # App 主入口（樣式、資源字典）
├── App.axaml.cs                # App 主程式碼（初始化、資源設定）
├── Program.cs                  # 程式入口
├── ExcelPasteTool.csproj       # 專案檔
├── README.md                   # 專案說明文件
│
├── 📂 Assets/                  # 靜態資源（會在 build 時一併打包）
│   ├── 📂 Images/             # 一般圖片 (png/jpg/svg)
│   ├── 📂 Icons/              # App icon / Toolbar icon / 系統托盤圖示
│   ├── 📂 Logos/              # 專案 LOGO 或品牌相關圖
│   ├── 📂 Fonts/              # 自訂字型檔
│   └── 📂 Styles/             # 全局樣式（Theme.axaml、字型設定等）
│
├── 📂 Controls/                # 自訂可重用控件（UserControl）
│   └── ...
│
├── 📂 Views/                   # 畫面（View）定義
│   ├── MainWindow.axaml
│   ├── MainWindow.axaml.cs
│   ├── DataToolView.axaml
│   └── DataToolView.axaml.cs
│
├── 📂 ViewModels/              # 畫面邏輯（ViewModel）
│   ├── SidebarViewModel.cs
│   └── ...
│
├── 📂 Models/                  # 資料模型（Model）
│   ├── DataItem.cs
│   ├── SidebarItem.cs
│   └── ...
│
├── 📂 Services/                # 後端服務（存取資料庫、API、設定檔等）
│   ├── ConfigServices.cs
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
│   ├── IconManager.cs          # 圖示管理
│   ├── LanguageManager.cs      # 語言管理
│   ├── ThemeManager.cs         # 主題管理
│   ├── ToastQueueHelper.cs     # 提示訊息佇列
│   └── ...
│
├── 📂 Resources/               # 資源字串（多語系、文字等）
│   ├── 📂 Languages/          # 語言包
│
├── 📂 Themes/                  # 主題資源（黑暗、光線）
│   ├── DarkTheme.axaml         # 黑暗主題樣式
│   ├── LightTheme.axaml        # 光線主題樣式
│   └── SidebarTheme.axaml      # 側邊欄主題樣式
│
└── 📂 Properties/              # 專案屬性
    └── Resources.Designer.cs    # 資源設計器（自動生成）
```