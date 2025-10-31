# LINE Integration 部署檢查清單

## 版本資訊
- **功能版本**: 1.0.0
- **實作日期**: 2025-11-01
- **最後更新**: 2025-11-01

---

## 📋 部署前準備

### 1. LINE Developers Console 設定

#### 1.1 LINE Login Channel
- [ ] 已在 [LINE Developers Console](https://developers.line.biz/console/) 建立 LINE Login Channel
- [ ] 記錄 **Channel ID**
- [ ] 記錄 **Channel Secret**
- [ ] 設定 Callback URL: `https://your-domain.com/signin-line`
- [ ] 確認已啟用 `profile` 和 `openid` scope

#### 1.2 LINE Messaging API Channel
- [ ] 已建立 Messaging API Channel
- [ ] 記錄 **Channel Access Token** (Long-lived)
- [ ] 記錄 **Channel Secret**
- [ ] 設定 Webhook URL: `https://your-domain.com/api/linewebhook`
- [ ] 啟用 **Use webhook** 選項
- [ ] 停用 **Auto-reply messages** (避免干擾)
- [ ] 停用 **Greeting messages** (避免干擾)

---

## 🗄️ 資料庫遷移

### 2. 執行 EF Core Migration

```bash
# 確認當前 migration 狀態
dotnet ef migrations list

# 套用 AddLineTables migration
dotnet ef database update

# 或產生 SQL 腳本手動執行
dotnet ef migrations script --output database/AddLineTables.sql
```

### 2.1 驗證資料表
- [ ] `LineBindings` 資料表已建立
- [ ] `LinePushLogs` 資料表已建立
- [ ] `LineConversationStates` 資料表已建立
- [ ] 所有索引和 FK 約束已正確建立

---

## ⚙️ 應用程式設定

### 3. appsettings.Production.json 配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<YOUR_SERVER>;Database=ClarityDesk;User Id=<USER>;Password=<PASSWORD>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true"
  },
  "LineLogin": {
    "ChannelId": "<LINE_LOGIN_CHANNEL_ID>",
    "ChannelSecret": "<LINE_LOGIN_CHANNEL_SECRET>",
    "CallbackPath": "/signin-line"
  },
  "LineMessaging": {
    "ChannelAccessToken": "<MESSAGING_API_CHANNEL_ACCESS_TOKEN>",
    "ChannelSecret": "<MESSAGING_API_CHANNEL_SECRET>",
    "BaseUrl": "https://your-domain.com",
    "ImageUploadPath": "wwwroot/uploads/line-images",
    "MaxImagesPerConversation": 3,
    "ConversationTimeoutMinutes": 1440,
    "RetryAttempts": 3,
    "RetryDelaySeconds": 2
  }
}
```

### 3.1 環境變數檢查
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] 確認 Connection String 正確
- [ ] 確認 LINE Channel credentials 正確
- [ ] 確認 BaseUrl 指向正確的 HTTPS 域名

---

## 📁 目錄結構準備

### 4. 建立上傳目錄

```bash
# Windows (PowerShell)
New-Item -ItemType Directory -Path "wwwroot\uploads\line-images" -Force
New-Item -ItemType Directory -Path "wwwroot\uploads\issues" -Force

# Linux
mkdir -p wwwroot/uploads/line-images
mkdir -p wwwroot/uploads/issues
```

### 4.1 目錄權限設定
- [ ] `wwwroot/uploads/line-images` 可寫入 (IIS_IUSRS / www-data)
- [ ] `wwwroot/uploads/issues` 可寫入
- [ ] 確認磁碟空間充足 (建議至少 10GB)

---

## 🌐 網站伺服器設定

### 5. IIS 部署 (Windows Server)

#### 5.1 IIS 模組安裝
- [ ] 已安裝 ASP.NET Core Hosting Bundle 8.0
- [ ] 已安裝 URL Rewrite Module (用於 HTTPS 重定向)

#### 5.2 應用程式集區設定
- [ ] .NET CLR 版本: **No Managed Code**
- [ ] Managed Pipeline Mode: **Integrated**
- [ ] Identity: **ApplicationPoolIdentity** 或自訂帳戶
- [ ] Start Mode: **AlwaysRunning** (避免冷啟動)
- [ ] Idle Time-out: **0** (永不逾時，確保背景服務持續運行)

#### 5.3 網站綁定
- [ ] HTTPS 綁定已設定 (Port 443)
- [ ] SSL 憑證已安裝且有效
- [ ] HTTP → HTTPS 重定向已設定

#### 5.4 檔案權限
```powershell
# 授予 IIS_IUSRS 寫入權限
icacls "C:\inetpub\wwwroot\ClarityDesk\wwwroot\uploads" /grant "IIS_IUSRS:(OI)(CI)M" /T
```

### 6. Linux (Nginx + Kestrel)

#### 6.1 Systemd Service 設定
```ini
# /etc/systemd/system/claritydesk.service
[Unit]
Description=ClarityDesk ASP.NET Core App
After=network.target

[Service]
WorkingDirectory=/var/www/claritydesk
ExecStart=/usr/bin/dotnet /var/www/claritydesk/ClarityDesk.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SLAStartTimeout=infinity
SLAStartDeadline=infinity
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

#### 6.2 Nginx 反向代理
```nginx
server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # LINE Webhook timeout settings
        proxy_connect_timeout 10s;
        proxy_send_timeout 10s;
        proxy_read_timeout 30s;
    }

    client_max_body_size 30M; # 支援圖片上傳
}
```

#### 6.3 檔案權限
```bash
chown -R www-data:www-data /var/www/claritydesk/wwwroot/uploads
chmod -R 755 /var/www/claritydesk/wwwroot/uploads
```

---

## 🔐 安全性檢查

### 7. 應用程式安全

- [ ] **敏感資料保護**:
  - [ ] appsettings.Production.json 不包含在版本控制中 (.gitignore)
  - [ ] Channel Secret 使用環境變數或 Azure Key Vault 儲存
  - [ ] Connection String 使用加密或 Managed Identity

- [ ] **HTTPS 強制執行**:
  - [ ] 所有 HTTP 請求重定向到 HTTPS
  - [ ] HSTS 標頭已啟用 (`UseHsts()`)

- [ ] **Webhook 安全性**:
  - [ ] `LineWebhookController` 已正確驗證 X-Line-Signature
  - [ ] Channel Secret 配置正確

- [ ] **CORS 政策**:
  - [ ] 如使用 SPA，確認 CORS 政策僅允許信任的來源

- [ ] **輸入驗證**:
  - [ ] 所有用戶輸入已通過 Data Annotations 驗證
  - [ ] 圖片上傳限制為 10MB × 3 張
  - [ ] 防止路徑遍歷攻擊 (檔案上傳使用 `Path.GetFileName`)

---

## 🧪 部署後驗證

### 8. 功能測試清單

#### 8.1 LINE Login (User Story 1)
- [ ] 訪問 `/Account/LineBinding` 頁面
- [ ] 點擊「綁定 LINE 帳號」按鈕
- [ ] 完成 LINE OAuth 授權流程
- [ ] 確認綁定狀態顯示正確
- [ ] 測試解除綁定功能
- [ ] 確認 `LineBindings` 資料表有記錄

#### 8.2 Push Notifications (User Story 2)
- [ ] 建立新的 Issue 並指派給已綁定 LINE 的使用者
- [ ] 確認該使用者收到 LINE 推送通知
- [ ] 修改 Issue 狀態 (Pending → Completed)
- [ ] 確認收到狀態變更通知
- [ ] 重新指派 Issue 給其他使用者
- [ ] 確認新指派人收到通知
- [ ] 檢查 `LinePushLogs` 資料表記錄

#### 8.3 Conversational Reporting (User Story 3)
- [ ] 使用已綁定的 LINE 帳號發送「回報問題」
- [ ] 依序輸入：標題、內容、單位、優先級、聯絡人、電話
- [ ] 上傳 1-3 張圖片 (測試限制)
- [ ] 確認收到確認訊息
- [ ] 送出確認後，檢查 ClarityDesk 系統是否建立 Issue
- [ ] 確認圖片已移至 `/uploads/issues/{issueId}/` 目錄
- [ ] 測試「取消」指令中斷對話
- [ ] 測試超時自動清理 (等待 24 小時或手動觸發)

#### 8.4 Background Services
- [ ] 確認 `ConversationCleanupService` 正在運行
  ```bash
  # 檢查 IIS Worker Process 日誌
  # 或查看 systemd journal
  journalctl -u claritydesk -f
  ```
- [ ] 建立測試對話但不完成
- [ ] 等待 1 小時後確認過期記錄已清理
- [ ] 確認暫存圖片已刪除

---

## 📊 監控與日誌

### 9. 應用程式監控設定

#### 9.1 日誌級別
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ClarityDesk.Services.LineMessagingService": "Information",
      "ClarityDesk.Services.ConversationCleanupService": "Information",
      "ClarityDesk.Controllers.LineWebhookController": "Information"
    }
  }
}
```

#### 9.2 監控項目
- [ ] 設定 Application Insights 或 Serilog
- [ ] 監控 LINE API 呼叫失敗率
- [ ] 追蹤 Webhook 接收延遲 (應 < 3 秒回應)
- [ ] 監控圖片上傳目錄磁碟使用率
- [ ] 設定錯誤警報 (Email/Slack)

#### 9.3 效能指標
- [ ] 平均 Webhook 回應時間 < 500ms
- [ ] 推送通知成功率 > 95%
- [ ] 對話完成率追蹤
- [ ] 資料庫連線池使用率

---

## 🔄 LINE Webhook 驗證

### 10. Webhook 連線測試

#### 10.1 使用 LINE Developers Console
1. 前往 Messaging API 設定頁面
2. 找到 **Webhook settings** 區塊
3. 點擊 **Verify** 按鈕
4. [ ] 確認顯示 **Success** (200 OK)

#### 10.2 手動測試
```bash
# 模擬 LINE Webhook POST 請求
curl -X POST https://your-domain.com/api/linewebhook \
  -H "Content-Type: application/json" \
  -H "X-Line-Signature: <VALID_SIGNATURE>" \
  -d '{
    "destination": "test",
    "events": []
  }'
```

---

## 🚨 故障排除

### 11. 常見問題

#### 11.1 LINE Login 失敗
**症狀**: OAuth callback 返回錯誤
- **檢查**:
  - Callback URL 是否完全匹配 (含 HTTPS)
  - Channel ID/Secret 是否正確
  - Session 是否啟用 (`AddSession()`)

#### 11.2 Webhook 接收失敗
**症狀**: LINE 訊息無反應
- **檢查**:
  - `dotnet ef database update` 是否已執行
  - Webhook URL 是否可從外部訪問 (使用 ngrok 測試)
  - 簽章驗證是否通過 (檢查 Channel Secret)
  - IIS/Nginx 是否允許 POST 請求
  - 防火牆/WAF 規則

#### 11.3 推送通知失敗
**症狀**: `LinePushLogs` 顯示 Failed 狀態
- **檢查**:
  - Channel Access Token 是否有效 (Long-lived)
  - HttpClient "LineMessagingAPI" 基底 URL 是否正確
  - 使用者是否已綁定且 `IsActive=true`
  - 網路連線至 `https://api.line.me`

#### 11.4 圖片上傳失敗
**症狀**: 圖片無法儲存或顯示
- **檢查**:
  - `wwwroot/uploads` 目錄權限
  - `FormOptions.MultipartBodyLengthLimit` 是否 ≥ 30MB
  - 磁碟空間是否充足
  - IIS Request Filtering 限制

#### 11.5 背景服務未運行
**症狀**: 過期對話未清理
- **檢查**:
  - IIS Application Pool Idle Timeout 設為 0
  - Linux systemd service 狀態 `systemctl status claritydesk`
  - 日誌中是否有 "ConversationCleanupService 已啟動"

---

## 📝 部署後檢查表

### 12. 最終驗證 (Go-Live Checklist)

- [ ] ✅ 所有資料庫 migration 已套用
- [ ] ✅ LINE Developers Console 設定完成
- [ ] ✅ appsettings.Production.json 配置正確
- [ ] ✅ HTTPS 憑證有效且已綁定
- [ ] ✅ 目錄權限設定正確
- [ ] ✅ 背景服務正常運行
- [ ] ✅ Webhook 驗證通過
- [ ] ✅ 三個 User Story 功能測試通過
- [ ] ✅ 日誌記錄正常運作
- [ ] ✅ 監控儀表板設定完成
- [ ] ✅ 錯誤警報機制已建立
- [ ] ✅ 備份策略已實施 (資料庫 + 上傳檔案)

---

## 📞 支援聯絡資訊

- **技術文件**: `specs/001-line-integration/README.md`
- **API 文件**: `specs/001-line-integration/api-integration-details.md`
- **架構文件**: `specs/001-line-integration/architecture.md`
- **LINE 官方文件**: https://developers.line.biz/en/docs/

---

## 🔖 版本歷史

| 版本 | 日期 | 變更摘要 |
|------|------|----------|
| 1.0.0 | 2025-11-01 | 初始版本 - LINE Integration 完整部署指南 |

