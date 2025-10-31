# LINE Push Message API Contract

**Version**: 1.0
**Date**: 2025-10-31
**Endpoint**: `POST https://api.line.me/v2/bot/message/push`

## 概述

此文件定義 ClarityDesk 系統如何使用 LINE Messaging API 的 Push Message 功能，向已綁定使用者推送問題回報單通知。

## Authentication

所有 Push Message 請求需包含 Channel Access Token。

**Header**:

```http
Authorization: Bearer {channel access token}
Content-Type: application/json
```

## Request Format

### 基本結構

```json
{
  "to": "U4af4980629...",
  "messages": [
    {
      "type": "text",
      "text": "Hello, World!"
    }
  ]
}
```

### 欄位說明

| 欄位 | 型別 | 必填 | 說明 |
|------|------|------|------|
| to | string | ✓ | 目標使用者的 LINE User ID |
| messages | array | ✓ | 訊息陣列（最多 5 則） |

## Message Types

### 1. Text Message (純文字訊息)

**用途**: 簡單通知或狀態更新

**格式**:

```json
{
  "type": "text",
  "text": "✅ 您的問題回報單 IR-12345 已指派給技術部 - 李四處理"
}
```

**限制**:

- `text` 最大長度：5000 字元
- 支援換行符號 `\n`
- 支援 emoji（Unicode）

### 2. Flex Message (彈性訊息)

**用途**: 結構化資料展示（問題回報單通知）

**格式**: 見 [flex-message-templates.json](./flex-message-templates.json)

## Push Message Scenarios

### Scenario 1: 新增問題回報單通知

**觸發時機**: 當新增問題回報單並指派給已綁定 LINE 的處理人員時

**目標使用者**: 指派處理人員

**訊息類型**: Flex Message (Bubble)

**Payload 範例**:

```json
{
  "to": "U4af4980629...",
  "messages": [
    {
      "type": "flex",
      "altText": "新問題回報單通知 - IR-12345",
      "contents": {
        "type": "bubble",
        "header": {
          "type": "box",
          "layout": "vertical",
          "contents": [
            {
              "type": "text",
              "text": "🔔 新問題回報單",
              "weight": "bold",
              "size": "lg",
              "color": "#1DB446"
            }
          ]
        },
        "body": {
          "type": "box",
          "layout": "vertical",
          "spacing": "sm",
          "contents": [
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "編號", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "IR-12345", "size": "sm", "weight": "bold", "flex": 5}
              ]
            },
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "標題", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "系統登入問題", "size": "sm", "wrap": true, "flex": 5}
              ]
            },
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "緊急程度", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "🔴 高", "size": "sm", "color": "#FF0000", "flex": 5}
              ]
            },
            {"type": "separator", "margin": "md"},
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "單位", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "技術部", "size": "sm", "flex": 5}
              ]
            },
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "聯絡人", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "張三", "size": "sm", "flex": 5}
              ]
            },
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "電話", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "0912-345-678", "size": "sm", "flex": 5}
              ]
            },
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "日期", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "2025-10-31 14:30", "size": "sm", "flex": 5}
              ]
            },
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "回報人", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "王小明", "size": "sm", "flex": 5}
              ]
            }
          ]
        },
        "footer": {
          "type": "box",
          "layout": "vertical",
          "contents": [
            {
              "type": "button",
              "action": {
                "type": "uri",
                "label": "查看詳細內容",
                "uri": "https://claritydesk.example.com/Issues/Details/12345"
              },
              "style": "primary",
              "color": "#1DB446"
            }
          ]
        }
      }
    }
  ]
}
```

### Scenario 2: 問題狀態變更通知

**觸發時機**: 當問題回報單狀態從「待處理」變更為「已完成」（或反向）

**目標使用者**: 原回報人 + 指派處理人員（已綁定 LINE）

**訊息類型**: Text Message（簡單通知）

**Payload 範例**:

```json
{
  "to": "U4af4980629...",
  "messages": [
    {
      "type": "text",
      "text": "✅ 問題回報單狀態更新\n\n回報單編號：IR-12345\n標題：系統登入問題\n新狀態：已完成\n\n查看詳細內容：\nhttps://claritydesk.example.com/Issues/Details/12345"
    }
  ]
}
```

### Scenario 3: 指派人員變更通知

**觸發時機**: 當問題回報單的指派處理人員變更

**目標使用者**: 新指派處理人員（已綁定 LINE）

**訊息類型**: Flex Message（與新增問題類似，但標題改為「問題已指派給您」）

**Payload 範例**:

```json
{
  "to": "U4af4980629...",
  "messages": [
    {
      "type": "flex",
      "altText": "問題已指派給您 - IR-12345",
      "contents": {
        "type": "bubble",
        "header": {
          "type": "box",
          "layout": "vertical",
          "contents": [
            {
              "type": "text",
              "text": "👤 問題已指派給您",
              "weight": "bold",
              "size": "lg",
              "color": "#0084FF"
            }
          ]
        },
        "body": {
          "type": "box",
          "layout": "vertical",
          "spacing": "sm",
          "contents": [
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "編號", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "IR-12345", "size": "sm", "weight": "bold", "flex": 5}
              ]
            },
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "標題", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "系統登入問題", "size": "sm", "wrap": true, "flex": 5}
              ]
            },
            {
              "type": "box",
              "layout": "baseline",
              "contents": [
                {"type": "text", "text": "緊急程度", "size": "sm", "color": "#999999", "flex": 2},
                {"type": "text", "text": "🔴 高", "size": "sm", "color": "#FF0000", "flex": 5}
              ]
            }
          ]
        },
        "footer": {
          "type": "box",
          "layout": "vertical",
          "contents": [
            {
              "type": "button",
              "action": {
                "type": "uri",
                "label": "查看詳細內容",
                "uri": "https://claritydesk.example.com/Issues/Details/12345"
              },
              "style": "primary",
              "color": "#0084FF"
            }
          ]
        }
      }
    }
  ]
}
```

## Response Format

### 成功回應

```http
HTTP/1.1 200 OK
Content-Type: application/json

{}
```

**Note**: 成功回應的 Body 為空 JSON 物件。

### 錯誤回應

#### 401 Unauthorized (無效的 Channel Access Token)

```json
{
  "message": "Authentication failed due to the following reason: invalid token. Confirm that the access token in the authorization header is valid."
}
```

#### 400 Bad Request (無效的請求格式)

```json
{
  "message": "The request body has 1 error(s)",
  "details": [
    {
      "message": "May not be empty",
      "property": "messages[0].text"
    }
  ]
}
```

#### 429 Too Many Requests (超過 Rate Limit)

```json
{
  "message": "Too many requests"
}
```

**處理策略**: 使用指數退避重試（1s, 2s, 4s）

## Rate Limits

- **每秒請求數 (QPS)**: 建議 < 10 QPS
- **每月訊息數**: 取決於 LINE 官方帳號方案
  - 免費方案：500 則/月
  - 輕量方案：~5000 則/月
  - 標準方案：無限制

**Note**: 超過限制會返回 429 錯誤，需等待後重試。

## Retry Strategy

推送失敗時採用指數退避重試策略：

```csharp
public async Task<bool> PushMessageWithRetryAsync(string userId, object message, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var success = await PushMessageAsync(userId, message);
            if (success) return true;

            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 1s, 2s, 4s
            await Task.Delay(delay);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "LINE Push Message 失敗 (嘗試 {Attempt}/{MaxRetries})", attempt + 1, maxRetries);

            if (attempt == maxRetries - 1)
            {
                // 最後一次重試失敗，記錄到 LinePushLog
                await LogPushFailureAsync(userId, message, ex.Message);
                return false;
            }
        }
    }

    return false;
}
```

## Best Practices

### 1. 使用 altText

Flex Message 必須提供 `altText`，在通知預覽和不支援 Flex Message 的裝置上顯示。

```json
{
  "type": "flex",
  "altText": "新問題回報單通知 - IR-12345",
  "contents": { ... }
}
```

### 2. URL 必須使用 HTTPS

按鈕的 `uri` 必須是 HTTPS URL，否則 LINE 會拒絕顯示。

### 3. 避免推送過於頻繁

- 僅推送關鍵事件（新增、狀態變更、指派變更）
- 避免短時間內對同一使用者推送多則訊息
- 考慮合併多個更新為單一訊息

### 4. 處理未綁定使用者

推送前檢查使用者是否已綁定 LINE：

```csharp
var binding = await _dbContext.LineBindings
    .FirstOrDefaultAsync(lb => lb.UserId == assignedUserId && lb.IsActive);

if (binding != null)
{
    await PushMessageAsync(binding.LineUserId, flexMessage);
}
```

### 5. 記錄推送日誌

所有推送操作應記錄到 `LinePushLog` 表，包含成功和失敗記錄。

## Testing

### 使用 LINE Messaging API Simulator

LINE 提供線上模擬器測試 Flex Message 格式：

- [Flex Message Simulator](https://developers.line.biz/flex-simulator/)

### 本地測試

使用 Postman 或 curl 模擬推送：

```bash
curl -X POST https://api.line.me/v2/bot/message/push \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_CHANNEL_ACCESS_TOKEN' \
  -d '{
    "to": "U4af4980629...",
    "messages": [
      {
        "type": "text",
        "text": "測試訊息"
      }
    ]
  }'
```

### 單元測試範例

```csharp
[Fact]
public async Task PushNewIssueNotification_ShouldSendFlexMessage_WhenUserHasBinding()
{
    // Arrange
    var issue = new IssueReportDto { Id = 12345, Title = "系統登入問題", ... };
    var binding = new LineBinding { LineUserId = "U123...", UserId = 1 };

    _mockDbContext.Setup(db => db.LineBindings.FirstOrDefaultAsync(...))
        .ReturnsAsync(binding);

    // Act
    var result = await _lineMessagingService.PushNewIssueNotificationAsync(issue);

    // Assert
    Assert.True(result);
    _mockHttpClient.Verify(http => http.PostAsJsonAsync(
        "https://api.line.me/v2/bot/message/push",
        It.IsAny<object>()), Times.Once);
}
```

## References

- [LINE Messaging API - Push Message](https://developers.line.biz/en/docs/messaging-api/sending-messages/#methods-of-sending-message)
- [LINE Messaging API - Flex Message](https://developers.line.biz/en/docs/messaging-api/using-flex-messages/)
- [LINE Messaging API - Rate Limits](https://developers.line.biz/en/docs/messaging-api/overview/#rate-limits)
