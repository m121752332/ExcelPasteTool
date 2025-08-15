# SidebarItem.cs 程式碼說明

此檔案定義了 SidebarItem 類別，主要用於側邊欄（Sidebar）每個選單項目的資料結構，並支援 MVVM 的屬性通知。

## 屬性
- `Page`：此選單項目所代表的頁面（SidebarPage 列舉）。
- `Name`：顯示名稱。
- `Icon`：圖示（通常是 SVG 或字串）。
- `SidebarViewModelInstance`：指向 SidebarViewModel 實例，可用於判斷選取狀態。
- `IsSelected`：判斷此項目是否被選取（SelectedPage 是否等於此 Page）。

## 建構子
- 初始化 Page、Name、Icon。

## INotifyPropertyChanged 實作
- `PropertyChanged` 事件：用於通知 UI 屬性變更。
- `OnPropertyChanged` 方法：觸發 PropertyChanged 事件，支援自動取得屬性名稱。

## 用途
SidebarItem 主要用於側邊欄的資料綁定，支援 UI 動態更新（如選取狀態切換），並可搭配 ViewModel 使用。
