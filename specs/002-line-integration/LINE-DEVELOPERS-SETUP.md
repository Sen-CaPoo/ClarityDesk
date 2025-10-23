# LINE Developers Console 設定指引

**功能**: LINE 官方帳號整合  
**建立日期**: 2025-10-24  
**參考文件**: quickstart.md, research.md

本文件提供建立 LINE Developers Console Channel 並取得憑證的詳細步驟。

---

## 前置需求

1. **LINE 帳號**: 您需要一個 LINE 帳號來登入 LINE Developers Console
2. **HTTPS 端點**: LINE Webhook 僅接受 HTTPS URL (本地開發可使用 ngrok 等工具)
3. **Azure 網域** (生產環境): 用於設定 Webhook URL

---

## 步驟 1: 建立 LINE Developers Provider

1. 前往 [LINE Developers Console](https://developers.line.biz/console/)
2. 使用您的 LINE 帳號登入
3. 點擊 **「Create a new provider」**
4. 輸入 Provider 名稱 (例如: `ClarityDesk`)
5. 點擊 **「Create」** 完成建立

---

## 步驟 2: 建立 Messaging API Channel

1. 在 Provider 頁面中,點擊 **「Create a new channel」**
2. 選擇 **「Messaging API」**
3. 填寫以下資訊:
   - **Channel type**: Messaging API
   - **Provider**: 選擇剛才建立的 Provider
   - **Channel icon** (可選): 上傳官方帳號頭像
   - **Channel name**: 輸入頻道名稱 (例如: `ClarityDesk Bot`)
   - **Channel description**: 輸入描述 (例如: `問題回報系統官方帳號`)
   - **Category**: 選擇 `Productivity` 或相關類別
   - **Subcategory**: 選擇適當的子類別
   - **Email address**: 輸入您的聯絡信箱
4. 勾選 **「I have read and agree to the LINE Official Account Terms of Use」**
5. 勾選 **「I have read and agree to the LINE Official Account API Terms of Use」**
6. 點擊 **「Create」** 完成建立

---

## 步驟 3: 取得 Channel ID 與 Channel Secret

1. 建立完成後,進入 Channel 設定頁面
2. 在 **「Basic settings」** 標籤中找到:
   - **Channel ID**: 複製此數值 (格式: 10 位數字,例如 `1234567890`)
   - **Channel secret**: 點擊 **「Show」** 按鈕顯示並複製 (格式: 32 個字元)

---

## 步驟 4: 發行 Channel Access Token

1. 切換至 **「Messaging API」** 標籤
2. 找到 **「Channel access token (long-lived)」** 區段
3. 點擊 **「Issue」** 按鈕
4. 複製生成的 Access Token (格式: 長字串,例如 `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`)

⚠️ **重要**: Access Token 僅顯示一次,請妥善保存!遺失需重新發行。

---

## 步驟 5: 設定 Webhook URL

1. 在 **「Messaging API」** 標籤中找到 **「Webhook settings」** 區段
2. 點擊 **「Edit」** 按鈕
3. 輸入 Webhook URL:
   - **本地開發**: `https://your-ngrok-url.ngrok.io/api/line/webhook`
   - **生產環境**: `https://your-domain.com/api/line/webhook`
4. 點擊 **「Update」** 儲存
5. 開啟 **「Use webhook」** 開關
6. 點擊 **「Verify」** 按鈕測試 Webhook 連線

⚠️ **注意**: Webhook 驗證會發送 POST 請求至您的伺服器,需確保端點已實作並回應 200 OK。

---

## 步驟 6: 關閉自動回覆訊息 (重要!)

LINE 官方帳號預設會自動回覆訊息,這會與 ClarityDesk 的對話流程衝突。

1. 在 **「Messaging API」** 標籤中找到 **「Auto-reply messages」** 區段
2. 點擊 **「Edit」** 連結 (會跳轉至 LINE Official Account Manager)
3. 在 LINE Official Account Manager 中:
   - 點擊 **「Response settings」**
   - 關閉 **「Greeting message」** (歡迎訊息)
   - 關閉 **「Auto-response messages」** (自動回覆)
   - 啟用 **「Webhook」** (如果尚未啟用)
4. 儲存設定

---

## 步驟 7: 將憑證設定至 ClarityDesk

### 方法 A: 使用 User Secrets (開發環境 - 推薦)

```powershell
# 在專案根目錄執行
cd d:\Project_01\ClarityDesk-2

# 設定 Channel ID
dotnet user-secrets set "LineSettings:ChannelId" "您的_CHANNEL_ID"

# 設定 Channel Secret
dotnet user-secrets set "LineSettings:ChannelSecret" "您的_CHANNEL_SECRET"

# 設定 Channel Access Token
dotnet user-secrets set "LineSettings:ChannelAccessToken" "您的_ACCESS_TOKEN"
```

### 方法 B: 使用環境變數 (生產環境 - Azure App Service)

1. 前往 Azure Portal
2. 開啟您的 App Service
3. 選擇 **「Configuration」** → **「Application settings」**
4. 新增以下設定:
   - `LineSettings:ChannelId` = `您的_CHANNEL_ID`
   - `LineSettings:ChannelSecret` = `您的_CHANNEL_SECRET`
   - `LineSettings:ChannelAccessToken` = `您的_ACCESS_TOKEN`
5. 點擊 **「Save」** 儲存並重新啟動應用程式

---

## 步驟 8: 測試綁定功能

1. 啟動 ClarityDesk 應用程式
2. 登入系統 (非訪客帳號)
3. 前往 **「帳號設定」** → **「LINE 綁定」**
4. 點擊 **「綁定 LINE 帳號」** 按鈕
5. 使用手機掃描 QR Code 或點擊連結
6. 授權後應自動導向回 ClarityDesk,顯示「綁定成功」

---

## 步驟 9: 測試推送通知功能

1. 在網頁端建立一筆新回報單
2. 指派給已綁定 LINE 的使用者
3. 確認該使用者在 LINE 收到推送訊息
4. 點擊訊息中的「查看詳情」按鈕,確認可正確開啟回報單頁面

---

## 步驟 10: 測試 LINE 端回報功能

1. 使用已綁定的 LINE 帳號
2. 在 LINE 中輸入「回報問題」
3. 依照系統引導逐步輸入資訊
4. 確認送出後在網頁端看到新建立的回報單

---

## 常見問題排除

### Q1: Webhook 驗證失敗,顯示「Connection refused」

**原因**: 本地開發環境無 HTTPS 端點或 ngrok 未啟動

**解決方式**:
1. 安裝 ngrok: `choco install ngrok` 或前往 [ngrok.com](https://ngrok.com/) 下載
2. 啟動 ngrok: `ngrok http https://localhost:5001`
3. 複製 ngrok 提供的 HTTPS URL (例如 `https://abcd1234.ngrok.io`)
4. 更新 LINE Developers Console 的 Webhook URL 為 `https://abcd1234.ngrok.io/api/line/webhook`

### Q2: 推送訊息失敗,錯誤代碼 401 Unauthorized

**原因**: Channel Access Token 無效或已過期

**解決方式**:
1. 前往 LINE Developers Console
2. 重新發行 Channel Access Token
3. 更新 User Secrets 或環境變數
4. 重新啟動應用程式

### Q3: 使用者點擊「綁定 LINE」後顯示「invalid_client」錯誤

**原因**: Channel ID 或 Channel Secret 設定錯誤

**解決方式**:
1. 確認 `appsettings.json` 中的 `LineLogin:ChannelId` 與 LINE Developers Console 的 Channel ID 一致
2. 確認 User Secrets 中的 `LineLogin:ChannelSecret` 正確
3. 重新啟動應用程式

### Q4: LINE 端回報時系統無回應

**原因**: Webhook 簽章驗證失敗或 Webhook 未啟用

**解決方式**:
1. 確認 LINE Official Account Manager 中已啟用 Webhook
2. 確認關閉了自動回覆訊息功能
3. 檢查 ClarityDesk 日誌,查看是否有簽章驗證錯誤

---

## 安全性提醒

⚠️ **絕對不要**:
- 將 Channel Secret 或 Access Token 提交到 Git 版本控制
- 在公開文件或截圖中洩漏憑證資訊
- 使用相同的 Access Token 於多個環境 (開發/測試/生產)

✅ **務必**:
- 定期輪換 Channel Access Token (建議每 6 個月)
- 使用 User Secrets (開發) 與環境變數 (生產) 管理憑證
- 監控 LINE Developers Console 的使用量統計

---

## 參考資源

- [LINE Developers 官方文件](https://developers.line.biz/en/docs/messaging-api/)
- [LINE Messaging API Reference](https://developers.line.biz/en/reference/messaging-api/)
- [LINE Login 整合指南](https://developers.line.biz/en/docs/line-login/)
- [Flex Message Simulator](https://developers.line.biz/flex-simulator/)

---

**完成日期**: 2025-10-24  
**最後更新**: 2025-10-24  
**狀態**: ✅ 設定完成,可開始實作
