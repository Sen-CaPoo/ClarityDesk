# LINE Webhook API Contract

**Version**: 1.0
**Date**: 2025-10-31
**Endpoint**: `POST /api/line/webhook`

## 概述

此 API 端點接收來自 LINE Messaging API 平台的 Webhook 事件，處理使用者在 LINE 官方帳號中發送的訊息、Postback 事件等。

## Authentication

LINE Webhook 使用簽章驗證確保請求來源的真實性。

**Header**:

```http
X-Line-Signature: {HMAC-SHA256 signature}
```

**驗證流程**:

1. 從請求 Header 取得 `X-Line-Signature`
2. 使用 Channel Secret 和請求 Body 計算 HMAC-SHA256
3. 比對計算結果與 Header 中的簽章
4. 不匹配則返回 `401 Unauthorized`

**實作範例**:

```csharp
private bool ValidateSignature(string channelSecret, string requestBody, string signature)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(channelSecret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
    var computedSignature = Convert.ToBase64String(hash);

    return computedSignature == signature;
}
```

## Request Format

LINE Webhook 使用 JSON 格式，可包含多個事件：

```json
{
  "destination": "U1234567890abcdef1234567890abcdef",
  "events": [
    {
      "type": "message",
      "message": {
        "type": "text",
        "id": "325708",
        "text": "回報問題"
      },
      "timestamp": 1462629479859,
      "source": {
        "type": "user",
        "userId": "U206d25c2ea6bd87c17655609a1c37cb8"
      },
      "replyToken": "nHuyWiB7yP5Zw52FIkcQobQuGDXCTA",
      "mode": "active"
    }
  ]
}
```

### 事件類型

#### 1. Message Event (訊息事件)

當使用者發送訊息時觸發。

**支援訊息類型**:

- `text`: 純文字訊息
- `image`: 圖片訊息
- `sticker`: 貼圖（不處理）
- `location`: 位置（不處理）
- `video`: 影片（不處理）
- `audio`: 音訊（不處理）

**Text Message 範例**:

```json
{
  "type": "message",
  "message": {
    "type": "text",
    "id": "325708",
    "text": "回報問題"
  },
  "timestamp": 1462629479859,
  "source": {
    "type": "user",
    "userId": "U206d25c2ea6bd87c17655609a1c37cb8"
  },
  "replyToken": "nHuyWiB7yP5Zw52FIkcQobQuGDXCTA",
  "mode": "active"
}
```

**Image Message 範例**:

```json
{
  "type": "message",
  "message": {
    "type": "image",
    "id": "325709",
    "contentProvider": {
      "type": "line"
    }
  },
  "timestamp": 1462629479859,
  "source": {
    "type": "user",
    "userId": "U206d25c2ea6bd87c17655609a1c37cb8"
  },
  "replyToken": "nHuyWiB7yP5Zw52FIkcQobQuGDXCTA",
  "mode": "active"
}
```

**圖片下載**:

```http
GET https://api-data.line.me/v2/bot/message/{messageId}/content
Authorization: Bearer {channel access token}
```

#### 2. Postback Event (Postback 事件)

當使用者點擊按鈕（action: postback）時觸發。

**範例**:

```json
{
  "type": "postback",
  "postback": {
    "data": "action=confirm&step=final"
  },
  "timestamp": 1462629479859,
  "source": {
    "type": "user",
    "userId": "U206d25c2ea6bd87c17655609a1c37cb8"
  },
  "replyToken": "nHuyWiB7yP5Zw52FIkcQobQuGDXCTA",
  "mode": "active"
}
```

**Postback Data 格式**:

使用查詢字串格式傳遞參數：

- `action=confirm` - 確認建立問題
- `action=cancel` - 取消流程
- `action=select_department&id=1` - 選擇單位
- `action=select_priority&value=High` - 選擇緊急程度

#### 3. Follow Event (關注事件)

當使用者加入官方帳號為好友時觸發（本功能不處理，僅記錄）。

```json
{
  "type": "follow",
  "timestamp": 1462629479859,
  "source": {
    "type": "user",
    "userId": "U206d25c2ea6bd87c17655609a1c37cb8"
  },
  "replyToken": "nHuyWiB7yP5Zw52FIkcQobQuGDXCTA",
  "mode": "active"
}
```

#### 4. Unfollow Event (取消關注事件)

當使用者封鎖或刪除官方帳號時觸發（本功能不處理，僅記錄）。

```json
{
  "type": "unfollow",
  "timestamp": 1462629479859,
  "source": {
    "type": "user",
    "userId": "U206d25c2ea6bd87c17655609a1c37cb8"
  },
  "mode": "active"
}
```

## Response Format

LINE Webhook **必須在 3 秒內回應 200 OK**，否則 LINE 平台視為失敗並重試。

**成功回應**:

```http
HTTP/1.1 200 OK
Content-Type: application/json

{}
```

**Note**: Webhook 端點應立即回應 200 OK，實際訊息處理使用背景任務（避免逾時）。

## 業務邏輯處理流程

### 1. 文字訊息處理流程

```text
收到文字訊息
  ↓
檢查使用者是否已綁定
  ├─ 否 → 回覆「請先完成帳號綁定」+ 綁定連結
  └─ 是 → 繼續
        ↓
    檢查對話狀態
      ├─ 無狀態 → 處理指令（「回報問題」啟動流程）
      └─ 有狀態 → 處理當前步驟輸入
            ↓
        更新對話狀態 → 前進至下一步驟
            ↓
        回覆下一步驟問題或確認摘要
```

### 2. 圖片訊息處理流程

```text
收到圖片訊息
  ↓
檢查對話狀態 CurrentStep
  ├─ 非 AskImages → 忽略（回覆「目前無法接收圖片」）
  └─ 是 AskImages → 繼續
        ↓
    下載圖片（調用 LINE Content API）
        ↓
    驗證圖片格式和大小
        ↓
    儲存至本地檔案系統
        ↓
    記錄 URL 到對話狀態 ImageUrls
        ↓
    回覆「已收到圖片 X/3」
```

### 3. Postback 事件處理流程

```text
收到 Postback 事件
  ↓
解析 data 參數
  ├─ action=select_department → 更新對話狀態 DepartmentId
  ├─ action=select_priority → 更新對話狀態 Priority
  ├─ action=confirm → 建立問題回報單
  └─ action=cancel → 刪除對話狀態
```

## 對話流程範例

### 啟動回報流程

**User → LINE**:

```text
回報問題
```

**LINE → System Webhook**:

```json
{
  "events": [{
    "type": "message",
    "message": {"type": "text", "text": "回報問題"},
    "source": {"userId": "U206d25c2ea6bd87c17655609a1c37cb8"},
    "replyToken": "nHuyWiB7yP5Zw52FIkcQobQuGDXCTA"
  }]
}
```

**System → LINE Reply API**:

```json
{
  "replyToken": "nHuyWiB7yP5Zw52FIkcQobQuGDXCTA",
  "messages": [
    {
      "type": "text",
      "text": "📝 開始問題回報流程\n\n請輸入問題標題："
    }
  ]
}
```

### 填寫問題標題

**User → LINE**:

```text
系統登入問題
```

**System → LINE Reply API**:

```json
{
  "replyToken": "...",
  "messages": [
    {
      "type": "text",
      "text": "✅ 問題標題：系統登入問題\n\n請輸入問題詳細內容："
    }
  ]
}
```

### 選擇所屬單位

**System → LINE Reply API**:

```json
{
  "replyToken": "...",
  "messages": [
    {
      "type": "text",
      "text": "請選擇問題所屬單位：",
      "quickReply": {
        "items": [
          {
            "type": "action",
            "action": {
              "type": "postback",
              "label": "客服部",
              "data": "action=select_department&id=1"
            }
          },
          {
            "type": "action",
            "action": {
              "type": "postback",
              "label": "技術部",
              "data": "action=select_department&id=2"
            }
          },
          {
            "type": "action",
            "action": {
              "type": "postback",
              "label": "業務部",
              "data": "action=select_department&id=3"
            }
          }
        ]
      }
    }
  ]
}
```

### 確認並建立

**System → LINE Reply API** (顯示摘要):

```json
{
  "replyToken": "...",
  "messages": [
    {
      "type": "text",
      "text": "📋 請確認以下資訊：\n\n標題：系統登入問題\n內容：無法使用帳號密碼登入\n單位：技術部\n緊急程度：🔴 高\n聯絡人：張三\n電話：0912345678\n\n請點擊下方按鈕確認送出：",
      "quickReply": {
        "items": [
          {
            "type": "action",
            "action": {
              "type": "postback",
              "label": "✅ 確認送出",
              "data": "action=confirm"
            }
          },
          {
            "type": "action",
            "action": {
              "type": "postback",
              "label": "❌ 取消",
              "data": "action=cancel"
            }
          }
        ]
      }
    }
  ]
}
```

**System → LINE Reply API** (建立成功):

```json
{
  "replyToken": "...",
  "messages": [
    {
      "type": "text",
      "text": "✅ 問題回報單已建立！\n\n回報單編號：IR-12345\n指派處理人員：李四\n\n點擊下方連結查看詳細內容："
    },
    {
      "type": "template",
      "altText": "查看回報單詳細內容",
      "template": {
        "type": "buttons",
        "text": "回報單 IR-12345",
        "actions": [
          {
            "type": "uri",
            "label": "查看詳細內容",
            "uri": "https://claritydesk.example.com/Issues/Details/12345"
          }
        ]
      }
    }
  ]
}
```

## Error Handling

### 簽章驗證失敗

```http
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "error": "Invalid signature"
}
```

### 未綁定帳號

系統回覆引導訊息：

```json
{
  "replyToken": "...",
  "messages": [
    {
      "type": "text",
      "text": "⚠️ 您尚未綁定系統帳號\n\n請先登入 ClarityDesk 網站完成帳號綁定：\nhttps://claritydesk.example.com/Account/LineBinding"
    }
  ]
}
```

### 圖片下載失敗

記錄錯誤並回覆使用者：

```json
{
  "replyToken": "...",
  "messages": [
    {
      "type": "text",
      "text": "❌ 圖片接收失敗，請重新上傳或跳過此步驟"
    }
  ]
}
```

### Webhook 處理逾時

- 立即回應 200 OK（避免 LINE 重試）
- 將訊息放入佇列或背景任務處理
- 使用 Push API 主動回覆使用者（不使用 Reply Token）

## Performance Considerations

- **非同步處理**: 接收 Webhook 後立即返回 200 OK，實際處理使用背景任務
- **冪等性**: 使用 `message.id` 防止重複處理（LINE 可能重送）
- **Rate Limiting**: 限制同一使用者的訊息處理頻率（防止濫用）

## Security

- **簽章驗證**: 所有請求必須驗證 `X-Line-Signature`
- **HTTPS Only**: Webhook URL 必須使用 HTTPS
- **IP Whitelist**: 可選，限制只接受 LINE 平台 IP（需查詢官方文件）

## Testing

### 使用 LINE Webhook Tester

LINE 開發者控制台提供 Webhook Tester 工具，可模擬發送事件測試端點。

### 本地測試

使用 ngrok 或類似工具將本地端點暴露至公網：

```bash
ngrok http 5191
```

將生成的 HTTPS URL 配置到 LINE Developers Console。

### 單元測試範例

```csharp
[Fact]
public async Task HandleTextMessage_ShouldStartConversation_WhenUserSendsKeyword()
{
    // Arrange
    var webhookEvent = new LineWebhookEvent
    {
        Type = "message",
        Message = new { Type = "text", Text = "回報問題" },
        Source = new { UserId = "U123..." },
        ReplyToken = "reply123"
    };

    // Act
    var result = await _webhookController.HandleWebhook(webhookEvent);

    // Assert
    Assert.Equal(200, result.StatusCode);
    // Verify conversation state created
}
```

## References

- [LINE Messaging API - Webhook](https://developers.line.biz/en/docs/messaging-api/receiving-messages/)
- [LINE Messaging API - Signature Validation](https://developers.line.biz/en/docs/messaging-api/receiving-messages/#verifying-signatures)
- [LINE Messaging API - Reply Message](https://developers.line.biz/en/docs/messaging-api/sending-messages/#methods-of-sending-message)
