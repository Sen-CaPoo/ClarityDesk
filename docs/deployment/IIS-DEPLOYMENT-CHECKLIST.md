# IIS 部署檢查清單

## ⚠️ 緊急診斷步驟 (遇到 500 錯誤時)

### 1. 檢查詳細錯誤日誌

```powershell
# 檢查 stdout 日誌 (最重要!)
Get-Content C:\inetpub\wwwroot\ClarityDesk\logs\stdout_*.log -Tail 50

# 如果沒有日誌檔案,檢查目錄權限
icacls C:\inetpub\wwwroot\ClarityDesk\logs

# 檢查 Windows 事件檢視器
Get-EventLog -LogName Application -Source "IIS AspNetCore Module V2" -Newest 10 | Format-List
```

### 2. 啟用詳細錯誤 (僅用於診斷!)

暫時修改 `web.config`:

```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
</environmentVariables>
```

**記得診斷完改回 `Production`!**

### 3. 常見錯誤快速修復

#### ❌ 錯誤: "An error occurred while starting the application"

**可能原因**:
- 缺少 `appsettings.Production.json`
- 資料庫連線字串錯誤
- LINE Login 設定缺失

**解決方案**:
1. 確認 `appsettings.Production.json` 存在且格式正確
2. 測試資料庫連線
3. 檢查環境變數

#### ❌ 錯誤: "Failed to load ASP.NET Core runtime"

**解決方案**:
```powershell
# 下載並安裝 ASP.NET Core 8.0 Hosting Bundle
# https://dotnet.microsoft.com/download/dotnet/8.0

# 安裝後重啟 IIS
net stop was /y
net start w3svc
```

#### ❌ 錯誤: "SqlException: Cannot open database"

**解決方案**:
```powershell
# 測試資料庫連線
Test-NetConnection -ComputerName your-server.database.windows.net -Port 1433

# 檢查防火牆規則 (Azure SQL)
# 在 Azure Portal 新增 IIS 伺服器的公開 IP
```

---

## 📋 完整部署步驟

### 前置準備

#### 1. 安裝必要軟體 (在 IIS 伺服器上執行)

```powershell
# 檢查是否已安裝 ASP.NET Core Runtime
dotnet --list-runtimes

# 應該看到:
# Microsoft.AspNetCore.App 8.0.x
# Microsoft.NETCore.App 8.0.x
```

如果沒有,請下載安裝:
- [ASP.NET Core 8.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)

#### 2. 建立應用程式集區

```powershell
# 方法 1: 使用 PowerShell
Import-Module WebAdministration

New-WebAppPool -Name "ClarityDesk" -Force
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name startMode -Value "AlwaysRunning"
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name processModel.idleTimeout -Value ([TimeSpan]::FromMinutes(0))
```

#### 3. 建立網站目錄與權限

```powershell
# 建立目錄
$sitePath = "C:\inetpub\wwwroot\ClarityDesk"
New-Item -Path $sitePath -ItemType Directory -Force
New-Item -Path "$sitePath\logs" -ItemType Directory -Force

# 設定權限 (重要!)
$acl = Get-Acl $sitePath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS",
    "FullControl",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $sitePath $acl

# 設定 logs 目錄權限
$logsAcl = Get-Acl "$sitePath\logs"
$logsRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS AppPool\ClarityDesk",
    "Modify",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$logsAcl.SetAccessRule($logsRule)
Set-Acl "$sitePath\logs" $logsAcl
```

#### 4. 建立 IIS 網站

```powershell
# 建立網站 (HTTP)
New-Website -Name "ClarityDesk" `
            -ApplicationPool "ClarityDesk" `
            -PhysicalPath $sitePath `
            -Port 80

# 新增 HTTPS 綁定
New-WebBinding -Name "ClarityDesk" -Protocol https -Port 443

# 綁定 SSL 憑證 (請替換成您的憑證指紋)
$cert = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*yourdomain.com*"}
if ($cert) {
    $binding = Get-WebBinding -Name "ClarityDesk" -Protocol https
    $binding.AddSslCertificate($cert.Thumbprint, "my")
}
```

---

### 應用程式部署

#### 1. 發佈應用程式 (在開發機器執行)

```powershell
# 切換到專案目錄
cd C:\Users\SenChen-陳俊宇\Desktop\PR\ClarityDesk

# 清理之前的發佈
Remove-Item -Path .\publish -Recurse -Force -ErrorAction SilentlyContinue

# 發佈 Release 版本
dotnet publish -c Release -o ./publish

# 檢查發佈檔案
Get-ChildItem .\publish

# 應該看到:
# - ClarityDesk.dll
# - appsettings.json
# - appsettings.Development.json
# - appsettings.Production.json (重要!)
# - web.config
# - wwwroot/
```

#### 2. 設定 Production 配置

**⚠️ 重要**: 編輯 `publish/appsettings.Production.json`,填入實際值:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=ClarityDesk;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;Encrypt=True;"
  },
  "LineLogin": {
    "ChannelId": "YOUR_LINE_CHANNEL_ID",
    "ChannelSecret": "YOUR_LINE_CHANNEL_SECRET",
    "CallbackPath": "/signin-line"
  }
}
```

**必要資訊**:
- **SQL Server 連線字串**: 從 Azure Portal 或您的 SQL Server 管理員取得
- **LINE Channel ID**: 從 [LINE Developers Console](https://developers.line.biz/console/) 取得
- **LINE Channel Secret**: 從 LINE Developers Console 取得

#### 3. 更新 LINE Callback URL

在 LINE Developers Console 設定:
```
Callback URL: https://your-domain.com/signin-line
```

#### 4. 上傳檔案到 IIS 伺服器

```powershell
# 方法 1: 壓縮並上傳
Compress-Archive -Path .\publish\* -DestinationPath ClarityDesk.zip

# 將 ClarityDesk.zip 上傳到 IIS 伺服器
# 然後在 IIS 伺服器執行:
```

在 IIS 伺服器上:

```powershell
# 停止網站
Stop-Website -Name "ClarityDesk"
Stop-WebAppPool -Name "ClarityDesk"

# 備份舊版本 (如果存在)
$backupPath = "C:\Backups\ClarityDesk_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
if (Test-Path "C:\inetpub\wwwroot\ClarityDesk\ClarityDesk.dll") {
    New-Item -Path $backupPath -ItemType Directory -Force
    Copy-Item -Path "C:\inetpub\wwwroot\ClarityDesk\*" -Destination $backupPath -Recurse
}

# 解壓縮新版本
Expand-Archive -Path C:\Temp\ClarityDesk.zip -DestinationPath C:\inetpub\wwwroot\ClarityDesk -Force

# 確保 logs 目錄存在
New-Item -Path "C:\inetpub\wwwroot\ClarityDesk\logs" -ItemType Directory -Force

# 啟動網站
Start-WebAppPool -Name "ClarityDesk"
Start-Website -Name "ClarityDesk"
```

#### 5. 執行資料庫 Migration

```powershell
# 方法 1: 使用預先產生的 SQL 腳本 (推薦)
# 在開發機器產生:
cd C:\Users\SenChen-陳俊宇\Desktop\PR\ClarityDesk
dotnet ef migrations script --idempotent --output migration.sql

# 將 migration.sql 上傳到 IIS 伺服器
# 使用 SQL Server Management Studio 或 Azure Data Studio 執行

# 方法 2: 應用程式會自動執行 (Program.cs 已設定)
# 第一次啟動時會自動建立資料庫和表格
```

---

### 測試與驗證

#### 1. 檢查應用程式是否正常啟動

```powershell
# 檢查應用程式集區狀態
Get-WebAppPoolState -Name "ClarityDesk"
# 應該顯示: Started

# 檢查網站狀態
Get-Website -Name "ClarityDesk" | Select-Object Name, State, PhysicalPath
# State 應該是: Started

# 檢查 stdout 日誌
Get-Content C:\inetpub\wwwroot\ClarityDesk\logs\stdout_*.log -Tail 20
```

#### 2. 測試網站存取

```powershell
# 測試 HTTP (應該會跳轉到 HTTPS)
Invoke-WebRequest -Uri "http://localhost" -UseBasicParsing

# 測試 HTTPS
Invoke-WebRequest -Uri "https://localhost" -UseBasicParsing -SkipCertificateCheck
```

在瀏覽器開啟:
- `https://your-domain.com`
- 應該看到登入頁面

#### 3. 測試 LINE Login

1. 點擊「使用 LINE 登入」
2. 授權後應該跳轉回網站並自動登入
3. 檢查是否能存取「回報單管理」頁面

---

### 監控與維護

#### 設定自動日誌清理

```powershell
# 建立清理腳本
$cleanupScript = @"
`$logPath = "C:\inetpub\wwwroot\ClarityDesk\logs"
`$daysToKeep = 30
Get-ChildItem -Path `$logPath -Recurse -File | 
    Where-Object {`$_.LastWriteTime -lt (Get-Date).AddDays(-`$daysToKeep)} | 
    Remove-Item -Force
"@

$cleanupScript | Out-File C:\Scripts\ClarityDesk_LogCleanup.ps1

# 建立排程任務
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' -Argument '-File C:\Scripts\ClarityDesk_LogCleanup.ps1'
$trigger = New-ScheduledTaskTrigger -Daily -At 2am
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "ClarityDesk_LogCleanup" -Description "清理 30 天前的日誌"
```

#### 設定效能監控

```powershell
# 監控應用程式集區記憶體
Get-Counter '\Process(w3wp*)\Working Set - Private' -Continuous

# 監控請求數
Get-Counter '\ASP.NET Applications(__Total__)\Requests/Sec' -Continuous
```

---

## 🔒 安全性檢查清單

部署完成後,請確認:

- [ ] **HTTPS 已啟用**: 所有 HTTP 流量強制跳轉到 HTTPS
- [ ] **SSL 憑證有效**: 瀏覽器無警告訊息
- [ ] **環境設定為 Production**: `web.config` 中 `ASPNETCORE_ENVIRONMENT=Production`
- [ ] **敏感資訊已移除**: `appsettings.Production.json` 不包含在版本控制中
- [ ] **資料庫使用強密碼**: 至少 12 字元,包含大小寫、數字、特殊符號
- [ ] **防火牆規則已設定**: Azure SQL 僅允許 IIS 伺服器 IP
- [ ] **stdout 日誌權限**: 僅管理員可讀取
- [ ] **應用程式集區隔離**: 使用獨立的應用程式集區身份

---

## 🆘 常見問題排除

### 問題: 無法連線到資料庫

```powershell
# 測試 SQL Server 連線
Test-NetConnection -ComputerName your-server.database.windows.net -Port 1433

# 如果失敗,檢查:
# 1. Azure SQL 防火牆規則
# 2. IIS 伺服器的公開 IP 是否在白名單
# 3. 連線字串格式是否正確
```

### 問題: LINE Login 失敗

**檢查清單**:
1. LINE Callback URL 是否設定為 `https://your-domain.com/signin-line`
2. Channel ID 和 Secret 是否正確
3. 是否使用 HTTPS (LINE 不支援 HTTP callback)

### 問題: 應用程式集區自動停止

```powershell
# 檢查事件檢視器
Get-EventLog -LogName Application -Source "Application Error" -Newest 10

# 可能原因:
# 1. 記憶體不足
# 2. 未處理的例外
# 3. 資料庫連線池耗盡

# 解決方案: 增加記憶體限制
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name recycling.periodicRestart.privateMemory -Value 0
```

### 問題: 靜態檔案 404

```powershell
# 檢查 wwwroot 目錄權限
icacls C:\inetpub\wwwroot\ClarityDesk\wwwroot

# 確保 IIS_IUSRS 有讀取權限
icacls C:\inetpub\wwwroot\ClarityDesk\wwwroot /grant "IIS_IUSRS:(OI)(CI)R"
```

---

## 📞 需要協助?

如果遇到無法解決的問題,請提供以下資訊:

1. **錯誤截圖**: 瀏覽器顯示的錯誤訊息
2. **stdout 日誌**: `C:\inetpub\wwwroot\ClarityDesk\logs\stdout_*.log` 最後 50 行
3. **事件檢視器**: Application Log 中的相關錯誤
4. **IIS 版本**: 執行 `Get-ItemProperty HKLM:\SOFTWARE\Microsoft\InetStp\ | Select-Object VersionString`
5. **.NET Runtime 版本**: 執行 `dotnet --list-runtimes`

---

**最後更新**: 2025-10-21  
**適用版本**: ClarityDesk v1.0.0
