# 專案目錄結構
```
ExcelPasteTool/
│
├── 📂 Assets/                  # 靜態資源（會在 build 時一併打包）
│   ├── 📂 Images/             # 一般圖片 (png/jpg/svg)
│   ├── 📂 Icons/              # App icon / Toolbar icon / 系統托盤圖示
│   ├── 📂 Logos/              # 專案 LOGO 或品牌相關圖
│   ├── 📂 Fonts/              # 自訂字型檔
│   └── 📂 Styles/             # 全局樣式（Theme.axaml、字型設定等）
│
├── 📂 Controls/               # 自訂可重用控件（UserControl）
│   ├── SidebarControl.axaml   # 側邊欄控制 UI
│   ├── SidebarControl.cs      # 側邊欄控制程式邏輯
│   └── ...
│
├── 📂 Views/                  # 畫面（View）定義
│   ├── MainWindow.axaml
│   ├── MainWindow.axaml.cs
│   ├── SettingsView.axaml
│   ├── SettingsView.axaml.cs
│   └── ...
│
├── 📂 ViewModels/              # 畫面邏輯（ViewModel）
│   ├── MainWindowViewModel.cs
│   ├── SidebarViewModel.cs
│   └── ...
│
├── 📂 Models/                  # 資料模型（Model）
│   ├── UserModel.cs
│   ├── ConfigModel.cs
│   └── ...
│
├── 📂 Services/                # 後端服務（存取資料庫、API、設定檔等）
│   ├── ConfigService.cs
│   ├── IconService.cs
│   └── ...
│
├── 📂 Converters/              # IValueConverter 與 Binding 轉換器
│   └── SidebarConverters.cs
│   └── SidebarPageToContentConverter.cs
│   └── EvenRowBackgroundConverter.cs
│
├── 📂 Behaviors/               # 互動行為與事件觸發器
│   └── DragDropBehavior.cs
│
├── 📂 Helpers/                 # 工具類（靜態輔助方法）
│   └── ImageHelper.cs
│
├── 📂 Resources/               # 資源字串（多語系、文字等）
│   ├── Strings.en.resx
│   ├── Strings.zh-TW.resx
│   └── ...
│
├── App.axaml                   # App 主入口（樣式、資源字典）
├── App.axaml.cs
├── Program.cs                  # 程式入口
├── MyAvaloniaApp.csproj
└── README.md
```
