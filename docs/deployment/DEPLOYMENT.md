# ClarityDesk 部署指南

本文件提供 ClarityDesk 顧客問題紀錄追蹤系統的完整部署說明,涵蓋 Windows Server + IIS 環境的部署步驟。

## 📋 目錄

- [系統需求](#系統需求)
- [部署前準備](#部署前準備)
- [IIS 環境設定](#iis-環境設定)
- [應用程式部署](#應用程式部署)
- [環境變數設定](#環境變數設定)
- [SSL 憑證配置](#ssl-憑證配置)
- [效能調校](#效能調校)
- [監控與日誌](#監控與日誌)
- [故障排除](#故障排除)

## 系統需求

### 硬體需求

- **CPU**: 2 核心以上
- **記憶體**: 4 GB RAM (建議 8 GB)
- **硬碟**: 10 GB 可用空間
- **網路**: 穩定的網際網路連線

### 軟體需求

- **作業系統**: Windows Server 2016 或更新版本
- **IIS**: 10.0 或更新版本
- **ASP.NET Core Runtime**: 8.0.x
- **ASP.NET Core Hosting Bundle**: 8.0.x
- **SQL Server**: 2016 或更新版本 (或 Azure SQL Database)
- **.NET SDK**: 8.0.x (僅開發環境需要)

## 部署前準備

### 1. 安裝 IIS

開啟 PowerShell (管理員權限):

```powershell
# 安裝 IIS 及必要功能
Install-WindowsFeature -name Web-Server -IncludeManagementTools

# 安裝 URL Rewrite Module
# 下載並安裝: https://www.iis.net/downloads/microsoft/url-rewrite
```

### 2. 安裝 ASP.NET Core Hosting Bundle

```powershell
# 下載 ASP.NET Core 8.0 Hosting Bundle
# https://dotnet.microsoft.com/download/dotnet/8.0

# 執行安裝程式
# 安裝後重新啟動 IIS
net stop was /y
net start w3svc
```

### 3. 驗證安裝

```powershell
# 檢查 .NET Runtime 版本
dotnet --list-runtimes

# 應該看到:
# Microsoft.AspNetCore.App 8.0.x
# Microsoft.NETCore.App 8.0.x
```

### 4. 設定資料庫

#### 使用 Azure SQL Database

1. 登入 [Azure Portal](https://portal.azure.com/)
2. 建立新的 SQL Database
3. 設定防火牆規則,允許 IIS 伺服器 IP
4. 取得連線字串

#### 使用本地 SQL Server

```sql
-- 建立資料庫
CREATE DATABASE ClarityDesk;
GO

-- 建立登入帳號
CREATE LOGIN clarity_user WITH PASSWORD = 'YourStrongPassword123!';
GO

USE ClarityDesk;
GO

-- 建立使用者並授予權限
CREATE USER clarity_user FOR LOGIN clarity_user;
GO

ALTER ROLE db_owner ADD MEMBER clarity_user;
GO
```

## IIS 環境設定

### 1. 建立應用程式集區

開啟 IIS 管理員:

```powershell
# 使用 PowerShell 建立應用程式集區
Import-Module WebAdministration

New-WebAppPool -Name "ClarityDesk" -Force
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name enable32BitAppOnWin64 -Value $false
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name processModel.identityType -Value 4
```

或透過 IIS 管理員手動設定:

1. 開啟 IIS 管理員
2. 點擊「應用程式集區」
3. 點擊「新增應用程式集區」
4. 名稱: `ClarityDesk`
5. .NET CLR 版本: **無受管理的程式碼**
6. 受管理的管線模式: **整合式**
7. 進階設定:
   - 一般 → 啟用 32 位元應用程式: **False**
   - 處理序模型 → 識別: **ApplicationPoolIdentity**

### 2. 建立網站

```powershell
# 建立網站目錄
$sitePath = "C:\inetpub\wwwroot\ClarityDesk"
New-Item -Path $sitePath -ItemType Directory -Force

# 建立日誌目錄
New-Item -Path "$sitePath\logs" -ItemType Directory -Force

# 設定權限
$acl = Get-Acl $sitePath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $sitePath $acl

# 建立 IIS 網站
New-Website -Name "ClarityDesk" `
            -ApplicationPool "ClarityDesk" `
            -PhysicalPath $sitePath `
            -Port 80
```

### 3. 設定 HTTPS (建議)

```powershell
# 綁定 HTTPS (假設已有 SSL 憑證)
New-WebBinding -Name "ClarityDesk" -Protocol https -Port 443

# 設定 SSL 憑證
$cert = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*yourdomain.com*"}
(Get-WebBinding -Name "ClarityDesk" -Protocol https).AddSslCertificate($cert.Thumbprint, "my")
```

## 應用程式部署

### 1. 發佈應用程式

在開發機器上執行:

```bash
# 切換到專案目錄
cd D:\Project_01\ClarityDesk

# 發佈 Release 版本
dotnet publish -c Release -o ./publish

# 壓縮發佈檔案
Compress-Archive -Path ./publish/* -DestinationPath ClarityDesk.zip
```

### 2. 上傳檔案

將 `ClarityDesk.zip` 上傳至 IIS 伺服器,解壓縮到網站目錄:

```powershell
# 解壓縮至網站目錄
Expand-Archive -Path C:\Temp\ClarityDesk.zip -DestinationPath C:\inetpub\wwwroot\ClarityDesk -Force
```

### 3. 設定組態檔

編輯 `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.database.windows.net;Database=ClarityDesk;User Id=clarity_user;Password=YourStrongPassword123!;Encrypt=True;"
  },
  "LineLogin": {
    "ChannelId": "your-production-channel-id",
    "ChannelSecret": "your-production-channel-secret"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**⚠️ 重要**: 絕對不要將敏感資訊提交到版本控制!

### 4. 執行 Migration

```powershell
# 方法 1: 使用 dotnet CLI (需要 .NET SDK)
cd C:\inetpub\wwwroot\ClarityDesk
dotnet ef database update --project ClarityDesk.csproj

# 方法 2: 使用預先產生的 SQL 腳本 (建議)
# 在開發環境產生腳本:
# dotnet ef migrations script --idempotent --output migration.sql
# 然後在 SQL Server 執行該腳本
```

### 5. 重新啟動 IIS

```powershell
# 重新啟動應用程式集區
Restart-WebAppPool -Name "ClarityDesk"

# 或重新啟動整個 IIS
iisreset
```

## 環境變數設定

### 設定環境變數 (可選)

如果不想將敏感資訊寫在 `appsettings.json`,可使用環境變數:

```powershell
# 設定應用程式集區環境變數
Set-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
    -filter "system.applicationHost/applicationPools/add[@name='ClarityDesk']/environmentVariables" `
    -name "." `
    -value @{name='ConnectionStrings__DefaultConnection';value='your-connection-string'}

Set-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
    -filter "system.applicationHost/applicationPools/add[@name='ClarityDesk']/environmentVariables" `
    -name "." `
    -value @{name='LineLogin__ChannelId';value='your-channel-id'}
```

## SSL 憑證配置

### 使用 Let's Encrypt (免費)

```powershell
# 安裝 win-acme
Invoke-WebRequest -Uri "https://github.com/win-acme/win-acme/releases/latest" -OutFile wacs.zip
Expand-Archive wacs.zip -DestinationPath C:\Tools\win-acme

# 執行憑證申請
cd C:\Tools\win-acme
.\wacs.exe

# 選擇選項:
# N: 建立新憑證
# 1: 單一站台
# 選擇 ClarityDesk 網站
# 輸入 email
```

### 使用商業憑證

1. 購買 SSL 憑證 (如 DigiCert, GoDaddy)
2. 產生 CSR (憑證簽署要求)
3. 提交 CSR 給憑證頒發機構
4. 下載憑證檔案
5. 匯入憑證到 Windows 憑證存放區
6. 在 IIS 綁定 HTTPS

## 效能調校

### IIS 應用程式集區設定

```powershell
# 設定應用程式集區進階選項
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name recycling.periodicRestart.time -Value ([TimeSpan]::FromHours(0))
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name recycling.periodicRestart.privateMemory -Value 0
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name processModel.idleTimeout -Value ([TimeSpan]::FromMinutes(20))
Set-ItemProperty IIS:\AppPools\ClarityDesk -Name cpu.limit -Value 0
```

### 啟用輸出快取

編輯 `web.config`,已包含在專案中:

```xml
<staticContent>
  <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="365.00:00:00" />
</staticContent>
```

### 資料庫連線池

連線字串中加入連線池參數:

```
Server=...;Min Pool Size=10;Max Pool Size=100;Connection Timeout=30;
```

## 監控與日誌

### 設定 Application Insights (可選)

1. 建立 Azure Application Insights 資源
2. 取得 Instrumentation Key
3. 在 `appsettings.Production.json` 加入:

```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-instrumentation-key"
  }
}
```

### IIS 日誌

日誌位置: `C:\inetpub\wwwroot\ClarityDesk\logs\`

設定日誌輪替:

```powershell
# 建立排程任務清理舊日誌 (保留 30 天)
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' -Argument '-Command "Get-ChildItem -Path C:\inetpub\wwwroot\ClarityDesk\logs -Recurse -File | Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} | Remove-Item -Force"'
$trigger = New-ScheduledTaskTrigger -Daily -At 2am
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "ClarityDesk_LogCleanup" -Description "清理 30 天前的 ClarityDesk 日誌"
```

## 故障排除

### 常見問題

#### 1. HTTP Error 500.31 - Failed to load ASP.NET Core runtime

**原因**: 未安裝 ASP.NET Core Hosting Bundle

**解決**:
```powershell
# 下載並安裝 Hosting Bundle
# https://dotnet.microsoft.com/download/dotnet/8.0
iisreset
```

#### 2. HTTP Error 500.30 - ASP.NET Core app failed to start

**原因**: 應用程式組態錯誤或相依性問題

**解決**:
```powershell
# 檢查 stdout 日誌
Get-Content C:\inetpub\wwwroot\ClarityDesk\logs\stdout_*.log -Tail 50

# 檢查事件檢視器
Get-EventLog -LogName Application -Source "IIS AspNetCore Module V2" -Newest 10
```

#### 3. 資料庫連線失敗

**原因**: 連線字串錯誤或防火牆阻擋

**解決**:
```powershell
# 測試資料庫連線
Test-NetConnection -ComputerName your-server.database.windows.net -Port 1433

# 檢查連線字串格式
# 確保 SQL Server 允許遠端連線
```

#### 4. LINE Login 失敗

**原因**: Callback URL 設定錯誤

**解決**:
1. 檢查 LINE Developers Console 的 Callback URL
2. 確保格式為: `https://your-domain.com/signin-line`
3. 確認 Channel ID 和 Secret 正確

### 效能問題診斷

```powershell
# 檢查應用程式集區記憶體使用
Get-Counter '\Process(w3wp*)\Working Set - Private'

# 檢查 CPU 使用率
Get-Counter '\Processor(_Total)\% Processor Time'

# 檢查 IIS 請求數
Get-Counter '\Web Service(_Total)\Current Connections'
```

### 啟用詳細錯誤訊息 (僅開發/偵錯)

編輯 `web.config`:

```xml
<aspNetCore processPath="dotnet" 
            arguments=".\ClarityDesk.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
  </environmentVariables>
</aspNetCore>
```

**⚠️ 警告**: 正式環境務必設為 `Production`,避免洩漏敏感資訊!

## 備份與復原

### 自動備份資料庫

```powershell
# 建立備份腳本
$backupScript = @"
`$date = Get-Date -Format 'yyyyMMdd_HHmmss'
`$backupFile = "C:\Backups\ClarityDesk_`$date.bak"
Invoke-Sqlcmd -Query "BACKUP DATABASE ClarityDesk TO DISK = '`$backupFile'"
"@

$backupScript | Out-File C:\Scripts\BackupClarityDesk.ps1

# 建立排程任務 (每天凌晨 1 點備份)
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' -Argument '-File C:\Scripts\BackupClarityDesk.ps1'
$trigger = New-ScheduledTaskTrigger -Daily -At 1am
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "ClarityDesk_Backup"
```

## 更新部署

### 零停機更新 (使用 App_Offline.htm)

```powershell
# 1. 建立維護頁面
@"
<!DOCTYPE html>
<html>
<head>
    <title>系統維護中</title>
</head>
<body>
    <h1>ClarityDesk 系統維護中</h1>
    <p>預計 10 分鐘內完成,請稍後再試。</p>
</body>
</html>
"@ | Out-File C:\inetpub\wwwroot\ClarityDesk\app_offline.htm

# 2. 部署新版本
Copy-Item -Path C:\Temp\ClarityDesk\* -Destination C:\inetpub\wwwroot\ClarityDesk -Recurse -Force

# 3. 移除維護頁面
Remove-Item C:\inetpub\wwwroot\ClarityDesk\app_offline.htm

# 4. 重新啟動應用程式集區
Restart-WebAppPool -Name "ClarityDesk"
```

## 安全性檢查清單

- [ ] HTTPS 已啟用且強制跳轉
- [ ] SSL 憑證有效且未過期
- [ ] `appsettings.Production.json` 不含敏感資訊
- [ ] 資料庫使用強密碼
- [ ] SQL Server 防火牆規則已設定
- [ ] IIS 已移除不必要的 HTTP 標頭
- [ ] 已設定安全標頭 (X-Frame-Options, X-Content-Type-Options 等)
- [ ] stdout 日誌權限已限制
- [ ] 已停用詳細錯誤訊息 (ASPNETCORE_ENVIRONMENT=Production)

## 結論

完成以上步驟後,ClarityDesk 應該已成功部署並正常運作。如有任何問題,請參考故障排除章節或聯絡技術支援。

---

**最後更新**: 2025-10-21  
**版本**: 1.0.0
