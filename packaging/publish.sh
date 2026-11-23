#!/usr/bin/env bash
# 打包單一平台的發佈檔並壓成 zip，由 .github/workflows/release.yml 呼叫。
#
# 用法：packaging/publish.sh <rid> <version> [readytorun]
#   rid         win-x64 / linux-x64 / osx-x64 / osx-arm64
#   version     例如 1.6.1
#   readytorun  預設 true（正式發佈）；PR 試打包傳 false 以省下約一分鐘的預先編譯
#
# 產出：儲存庫根目錄的 DataSplitter_<平台>_<架構>.zip
# 程式內的自動更新會依這個檔名挑選對應的更新檔，請勿任意更名。
set -euo pipefail

rid="$1"
v="$2"
r2r="${3:-true}"
root="$(pwd)"
asset="DataSplitter_${rid//-/_}"
out="out/$rid"

# 變數一律寫成 ${name}：macOS 內建的 bash 3.2 會把緊接在後的全形字元當成變數名稱的一部分
echo "::group::Publish ${rid}（ReadyToRun=${r2r}）"
extra=""
# 正式發佈不額外傳參數，ReadyToRun 照 csproj 的設定開啟；只有試打包才關掉
if [ "$r2r" != "true" ]; then
  extra="-p:PublishReadyToRun=false"
fi
dotnet publish ExcelPasteTool.csproj -c Release -r "$rid" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:Version=$v -p:AssemblyVersion=$v.0 -p:FileVersion=$v.0 -p:InformationalVersion=$v \
  $extra \
  -o "$out"
cp config.json "$out/"
rm -f "$out"/*.pdb
echo "::endgroup::"

# 確認發佈檔是「自包含」且內含 .NET 11 runtime，使用者免安裝 .NET。
# 若有人誤改成 --self-contained false 或目標框架不是 net11.0，這一步會讓 Release 失敗。
cfg="bin/Release/net11.0/$rid/DataSplitter.runtimeconfig.json"
cat "$cfg"
ver=$(jq -r '.runtimeOptions.includedFrameworks[]? | select(.name=="Microsoft.NETCore.App") | .version' "$cfg")
case "$ver" in
  11.*) echo "OK: 內含 Microsoft.NETCore.App $ver" ;;
  *) echo "::error::發佈檔未內含 .NET 11 runtime（取得：'${ver}'），請確認 --self-contained true 與 net11.0"; exit 1 ;;
esac

if [[ "$rid" == osx-* ]]; then
  # Apple Silicon 不接受完全沒簽章的執行檔（會直接被系統終止）。
  # .NET SDK 會在發佈時自動加上 ad-hoc 簽章；這一步確認簽章確實存在，避免日後 SDK 行為改變時默默發出不能執行的版本。
  python3 - "$out/DataSplitter" <<'PY'
import struct, sys
d = open(sys.argv[1], 'rb').read(64 * 1024)
if struct.unpack('<I', d[:4])[0] != 0xfeedfacf:
    sys.exit('::error::不是 64 位元 Mach-O 執行檔')
ncmds = struct.unpack('<I', d[16:20])[0]
off = 32
for _ in range(ncmds):
    cmd, size = struct.unpack('<II', d[off:off + 8])
    if cmd == 0x1d:  # LC_CODE_SIGNATURE
        print('OK: 執行檔含有程式碼簽章（LC_CODE_SIGNATURE）')
        sys.exit(0)
    off += size
sys.exit('::error::macOS 執行檔沒有程式碼簽章，Apple Silicon 上會無法執行')
PY

  # macOS 版包成 DataSplitter.app，Finder 雙擊時才不會先開「終端機」。
  # zip 根目錄結構（自動更新依賴，請勿任意更動）：
  #   DataSplitter.app/      程式本體（Info.plist、圖示、ad-hoc 簽章）
  #   DataSplitter           相容啟動腳本：1.4.1 以前的舊版更新只認根目錄的 DataSplitter
  #   README_macOS.txt       首次開啟說明
  # 設定檔改放 ~/Library/Application Support/DataSplitter，不放進 .app（避免破壞簽章封裝）。
  pkg="pkg/$rid"
  app="$pkg/DataSplitter.app"
  mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
  mv "$out/DataSplitter" "$app/Contents/MacOS/DataSplitter"
  chmod +x "$app/Contents/MacOS/DataSplitter"
  cp packaging/macos/DataSplitter.icns "$app/Contents/Resources/DataSplitter.icns"
  sed "s/__VERSION__/$v/g" packaging/macos/Info.plist > "$app/Contents/Info.plist"
  plutil -lint "$app/Contents/Info.plist"
  codesign --force --sign - "$app"
  codesign --verify --strict --verbose=2 "$app"
  cp packaging/macos/DataSplitter "$pkg/DataSplitter"
  chmod +x "$pkg/DataSplitter"
  cat > "$pkg/README_macOS.txt" <<'TXT'
DataSplitter Pro - macOS 首次開啟說明
=====================================

請雙擊「DataSplitter.app」開啟程式（可以拖到「應用程式」資料夾）。
資料夾裡另一個名為 DataSplitter 的檔案是給舊版自動更新用的，不用理它。

本程式沒有 Apple 開發者簽章與公證，用瀏覽器下載後第一次開啟會出現：
「無法打開『DataSplitter』，因為它來自未識別的開發者」或「Apple 無法驗證」。
請先做一次下面任一種方式，之後就能直接雙擊開啟。

方法一：終端機（建議）
  1. 打開「終端機」
  2. 輸入 cd 加一個空格，把這個資料夾拖進終端機視窗，按 Enter
  3. 執行：
       xattr -dr com.apple.quarantine .
  4. 回到 Finder 雙擊 DataSplitter.app

方法二：系統設定
  1. 先雙擊 DataSplitter.app 一次，讓它被擋下
  2. 到「系統設定 > 隱私權與安全性」，往下找到「已阻擋『DataSplitter』」
  3. 按「強制打開」（舊版 macOS 為「仍要打開」），輸入密碼確認

設定檔位置：~/Library/Application Support/DataSplitter/config.json
之後透過程式內的「檢查更新」取得的新版不會再被擋。

---------------------------------------------------------------
Open DataSplitter.app. It is not notarized by Apple; on first run either
open Terminal in this folder and run:  xattr -dr com.apple.quarantine .
or use System Settings > Privacy & Security > "Open Anyway".
TXT
  zipdir="$pkg"
else
  zipdir="$out"
fi

(
  cd "$zipdir"
  chmod +x DataSplitter 2>/dev/null || true
  zip -r -X "$root/$asset.zip" .
)
unzip -l "$root/$asset.zip"
