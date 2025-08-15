# ConfigServices.cs 程式碼說明

此檔案包含應用程式設定的資料結構與存取邏輯，負責讀取與儲存 config.json 設定檔。

## 類別與成員

### AppConfig
- `Language`：語言設定（預設值："TraditionalChinese"）。
- `Theme`：主題設定（預設值："Dark"）。

### ConfigServices
- `ConfigPath`：設定檔路徑（config.json）。
- `Config`：目前的設定資料（AppConfig 實例）。

#### 方法
- `Load()`：讀取 config.json，反序列化為 AppConfig，並更新 Config 屬性。若檔案不存在則輸出警告。
- `Save()`：將 Config 物件序列化為 JSON 並寫入 config.json。

## 用途
ConfigServices 用於集中管理應用程式的語言與主題等設定，並提供簡單的讀寫介面，方便在程式中存取或更新使用者偏好。
