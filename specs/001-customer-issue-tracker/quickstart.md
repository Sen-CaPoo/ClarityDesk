# Quick Start Guide: 顧客問題紀錄追蹤系統

**Feature**: 001-customer-issue-tracker  
**Date**: 2025-10-20  
**Target Audience**: 開發人員

## 概述

本指南協助開發人員快速設定 ClarityDesk 顧客問題紀錄追蹤系統的開發環境,並開始進行功能開發。

---

## 前置需求

### 必備軟體

| 軟體 | 版本 | 用途 | 下載連結 |
|------|------|------|----------|
| .NET SDK | 8.0+ | 執行與建置專案 | [下載](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Visual Studio 2022 | 17.8+ | 整合開發環境 (IDE) | [下載](https://visualstudio.microsoft.com/) |
| SQL Server Management Studio (SSMS) | 19.0+ | 資料庫管理工具 | [下載](https://aka.ms/ssmsfullsetup) |
| Git | 2.40+ | 版本控制 | [下載](https://git-scm.com/) |

### 選用軟體

| 軟體 | 用途 |
|------|------|
| Visual Studio Code | 輕量級編輯器 |
| Postman | API 測試工具 |
| Azure Data Studio | 跨平台資料庫工具 |

### Azure 服務

- **Azure SQL Database**: 資料庫伺服器 (需要有 Azure 訂閱)
- **LINE Developers Console**: 註冊 LINE Login 應用程式

---

## 環境設定

### 步驟 1: 複製專案

```bash
git clone https://github.com/your-org/ClarityDesk.git
cd ClarityDesk
git checkout 001-customer-issue-tracker
```

### 步驟 2: 還原 NuGet 套件

```bash
dotnet restore
```

### 步驟 3: 設定 Azure SQL Database

#### 3.1 建立 Azure SQL Database

1. 登入 [Azure Portal](https://portal.azure.com/)
2. 建立新的 **SQL Database**
   - 資料庫名稱: `ClarityDeskDB`
   - 伺服器: 建立新伺服器或選擇現有伺服器
   - 定價層: **Basic** (開發環境) 或 **Standard S0** (測試環境)
3. 設定防火牆規則,允許本地 IP 連線

#### 3.2 取得連線字串

在 Azure Portal 中複製連線字串:

```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=ClarityDeskDB;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

#### 3.3 設定本地連線字串

編輯 `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=ClarityDeskDB;Persist Security Info=False;User ID={your-username};Password={your-password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

⚠️ **重要**: `appsettings.Development.json` 包含敏感資訊,請確認已加入 `.gitignore`

### 步驟 4: 設定 LINE Login

#### 4.1 註冊 LINE Login Channel

1. 前往 [LINE Developers Console](https://developers.line.biz/console/)
2. 建立新 Provider (若尚未建立)
3. 建立新 Channel,選擇 **LINE Login**
   - Channel Name: `ClarityDesk`
   - Channel Description: `顧客問題紀錄追蹤系統`
4. 設定 Callback URL:
   - 開發環境: `https://localhost:5001/signin-line`
   - 正式環境: `https://yourdomain.com/signin-line`

#### 4.2 取得 Channel ID 和 Channel Secret

在 Channel 設定頁面取得:
- **Channel ID**: `1234567890`
- **Channel Secret**: `abcdef1234567890abcdef1234567890`

#### 4.3 設定本地組態

編輯 `appsettings.Development.json`:

```json
{
  "LineLogin": {
    "ChannelId": "1234567890",
    "ChannelSecret": "abcdef1234567890abcdef1234567890",
    "CallbackPath": "/signin-line"
  }
}
```

### 步驟 5: 執行資料庫 Migration

```bash
# 安裝 EF Core CLI 工具 (若尚未安裝)
dotnet tool install --global dotnet-ef

# 建立初始 Migration
dotnet ef migrations add InitialCreate

# 套用至資料庫
dotnet ef database update
```

驗證資料庫:

```bash
# 檢視已套用的 Migrations
dotnet ef migrations list
```

### 步驟 6: 執行種子資料 (Seed Data)

種子資料會在應用程式首次啟動時自動執行,建立預設管理員與單位資料。

---

## 執行應用程式

### 開發環境執行

#### 使用 Visual Studio

1. 開啟 `ClarityDesk.sln`
2. 按 `F5` 或點擊 **▶ ClarityDesk** 執行
3. 瀏覽器自動開啟 `https://localhost:5001`

#### 使用命令列

```bash
dotnet run --project ClarityDesk
```

或使用 watch 模式 (自動重新編譯):

```bash
dotnet watch run --project ClarityDesk
```

### 驗證應用程式運作

1. 開啟瀏覽器訪問 `https://localhost:5001`
2. 應該會自動導向 **登入頁面**
3. 點擊 **使用 LINE 登入** (將導向 LINE 授權頁面)
4. 授權後應該成功登入並導向 **首頁**

---

## 開發工作流程

### 建立新功能

1. **建立功能分支**:
   ```bash
   git checkout -b feature/issue-list-filter
   ```

2. **撰寫測試** (TDD):
   ```bash
   # 建立測試檔案
   mkdir -p Tests/ClarityDesk.UnitTests/Services
   # 撰寫測試
   ```

3. **實作功能**:
   ```bash
   # 建立服務檔案
   # 實作業務邏輯
   ```

4. **執行測試**:
   ```bash
   dotnet test
   ```

5. **提交變更**:
   ```bash
   git add .
   git commit -m "feat(issues): 新增回報單篩選功能"
   git push origin feature/issue-list-filter
   ```

6. **建立 Pull Request** 並等待審查

### 資料庫 Migration 工作流程

#### 新增 Migration

```bash
# 修改 Entity 或新增 Entity 後
dotnet ef migrations add AddCustomerEmailField
```

#### 套用 Migration

```bash
dotnet ef database update
```

#### 復原 Migration

```bash
# 復原到上一個 Migration
dotnet ef database update PreviousMigrationName

# 移除最後一個 Migration (尚未套用至資料庫)
dotnet ef migrations remove
```

#### 產生 SQL 腳本 (正式環境使用)

```bash
dotnet ef migrations script --idempotent --output ./migrations/AddCustomerEmailField.sql
```

---

## 測試

### 執行所有測試

```bash
dotnet test
```

### 執行特定測試專案

```bash
dotnet test Tests/ClarityDesk.UnitTests
dotnet test Tests/ClarityDesk.IntegrationTests
```

### 執行特定測試

```bash
dotnet test --filter "FullyQualifiedName~IssueReportServiceTests"
```

### 測試涵蓋率報告

```bash
# 安裝 coverlet
dotnet tool install --global coverlet.console

# 執行測試並產生涵蓋率報告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 常見問題 (Troubleshooting)

### 問題 1: 無法連線至 Azure SQL Database

**症狀**: `System.Data.SqlClient.SqlException: A network-related or instance-specific error`

**解決方案**:
1. 檢查 Azure SQL 防火牆規則是否包含本地 IP
2. 驗證連線字串的使用者名稱與密碼
3. 確認 SQL Server 允許 TCP/IP 連線 (Port 1433)

### 問題 2: Migration 執行失敗

**症狀**: `dotnet ef database update` 報錯

**解決方案**:
1. 檢查連線字串是否正確
2. 確認資料庫使用者有足夠權限 (CREATE TABLE, ALTER TABLE)
3. 檢視 Migration 腳本是否有語法錯誤

### 問題 3: LINE Login 授權失敗

**症狀**: 授權後返回 `CSRF token mismatch` 錯誤

**解決方案**:
1. 確認 Callback URL 與 LINE Developers Console 設定一致
2. 檢查 `state` 參數是否正確驗證
3. 清除瀏覽器 Cookies 並重新登入

### 問題 4: 應用程式無法啟動

**症狀**: `The type initializer for 'Microsoft.AspNetCore.Hosting.Builder' threw an exception`

**解決方案**:
1. 確認 .NET SDK 版本為 8.0+
2. 執行 `dotnet restore` 重新還原套件
3. 刪除 `bin/` 和 `obj/` 目錄後重新建置

---

## 開發工具設定

### Visual Studio 2022

#### 推薦擴充功能

- **ReSharper** (付費): 程式碼分析與重構工具
- **CodeMaid**: 程式碼清理工具
- **Web Essentials**: 前端開發工具
- **EF Core Power Tools**: EF Core 視覺化工具

#### 設定程式碼格式化

1. 工具 → 選項 → 文字編輯器 → C#
2. 啟用 **格式化文件 (Ctrl+K, Ctrl+D)**
3. 使用 `.editorconfig` 統一團隊程式碼風格

### VS Code

#### 推薦擴充功能

- **C# Dev Kit**: C# 開發支援
- **C#**: 語法高亮與 IntelliSense
- **NuGet Package Manager**: 套件管理
- **EF Core Power Tools**: EF Core 工具

#### 設定檔 (`.vscode/launch.json`)

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net8.0/ClarityDesk.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

---

## 效能測試

### 使用 Apache JMeter

1. 下載 [Apache JMeter](https://jmeter.apache.org/)
2. 建立測試計畫:
   - Thread Group: 50 使用者,Ramp-Up: 10 秒
   - HTTP Request: `GET /Issues`
   - Assertions: Response Time < 200ms
3. 執行測試並檢視結果

### 使用 k6

```bash
# 安裝 k6
choco install k6

# 建立測試腳本 (load-test.js)
cat > load-test.js << 'EOF'
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  vus: 50,
  duration: '30s',
};

export default function () {
  let res = http.get('https://localhost:5001/Issues');
  check(res, { 'status was 200': (r) => r.status == 200 });
  sleep(1);
}
EOF

# 執行負載測試
k6 run load-test.js
```

---

## 部署至 IIS (正式環境)

### 步驟 1: 發佈應用程式

```bash
dotnet publish -c Release -o ./publish
```

### 步驟 2: 建立 IIS Application Pool

1. 開啟 **IIS 管理員**
2. 右鍵點擊 **Application Pools** → **Add Application Pool**
   - Name: `ClarityDeskPool`
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: **Integrated**

### 步驟 3: 建立網站

1. 右鍵點擊 **Sites** → **Add Website**
   - Site Name: `ClarityDesk`
   - Application Pool: `ClarityDeskPool`
   - Physical Path: `C:\inetpub\wwwroot\ClarityDesk`
   - Binding: HTTPS, Port 443

### 步驟 4: 設定 SSL 憑證

1. 選擇網站 → **Bindings** → **Add**
2. Type: HTTPS, SSL Certificate: (選擇已安裝的憑證)

### 步驟 5: 驗證部署

1. 訪問 `https://yourdomain.com`
2. 確認應用程式正常運作

---

## 資源連結

### 官方文件

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [LINE Login Documentation](https://developers.line.biz/en/docs/line-login/)
- [Azure SQL Database Documentation](https://learn.microsoft.com/en-us/azure/azure-sql/)

### 專案文件

- [Feature Specification](./spec.md)
- [Implementation Plan](./plan.md)
- [Data Model](./data-model.md)
- [Service Contracts](./contracts/README.md)
- [Research Report](./research.md)

---

## 聯絡資訊

如有問題或需要協助,請聯絡:

- **技術負責人**: [Your Name]
- **Email**: [your.email@company.com]
- **Teams Channel**: #claritydesk-dev

---

**版本**: 1.0  
**狀態**: ✅ Phase 1 Complete  
**最後更新**: 2025-10-20
