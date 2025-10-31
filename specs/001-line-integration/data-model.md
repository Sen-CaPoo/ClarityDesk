# Data Model: LINE 整合功能

**Date**: 2025-10-31
**Feature**: LINE 整合功能
**Status**: Completed

## 概述

本文件定義 LINE 整合功能所需的新資料實體、關聯關係和驗證規則。所有實體遵循現有專案的設計模式（繼承時間戳欄位、使用 Fluent API 配置）。

## 實體定義

### 1. LineBinding (LINE 綁定記錄)

**用途**: 儲存系統使用者帳號與 LINE 使用者識別碼的對應關係

**欄位**:

| 欄位名 | 型別 | 必填 | 說明 | 約束 |
|--------|------|------|------|------|
| Id | int | ✓ | 主鍵（自動遞增） | Primary Key |
| UserId | int | ✓ | 系統使用者 ID | Foreign Key → User.Id |
| LineUserId | string | ✓ | LINE 使用者唯一識別碼（由 LINE 提供） | Unique, MaxLength(100) |
| DisplayName | string | ✓ | LINE 顯示名稱（綁定時擷取） | MaxLength(100) |
| PictureUrl | string? | | LINE 頭像 URL（綁定時擷取） | MaxLength(500) |
| IsActive | bool | ✓ | 綁定是否有效 | Default: true |
| BoundAt | DateTime | ✓ | 綁定時間 | Default: GETUTCDATE() |
| UnboundAt | DateTime? | | 解除綁定時間 | Nullable |
| CreatedAt | DateTime | ✓ | 建立時間（自動管理） | |
| UpdatedAt | DateTime | ✓ | 更新時間（自動管理） | |

**關聯關係**:

- **User** (1:1): 每個系統使用者最多綁定一個 LINE 帳號
  - Navigation Property: `User` (LineBinding → User)
  - Foreign Key: `UserId`

**索引**:

- `IX_LineBinding_LineUserId` (Unique): 確保 LINE 帳號唯一性
- `IX_LineBinding_UserId` (Unique): 確保系統使用者唯一綁定

**驗證規則**:

- `LineUserId` 必須符合 LINE 平台格式（UID 格式，通常為 "U" 開頭的 33 字元字串）
- 同一個 `LineUserId` 不可綁定多個 `UserId`
- 同一個 `UserId` 不可同時擁有多個有效綁定（`IsActive = true`）

**狀態轉換**:

```text
[未綁定] → (綁定成功) → [已綁定 (IsActive=true)]
[已綁定] → (解除綁定) → [已解綁 (IsActive=false, UnboundAt=NOW)]
[已解綁] → (重新綁定) → [已綁定 (新記錄，IsActive=true)]
```

### 2. LinePushLog (LINE 推送記錄)

**用途**: 記錄每次 LINE Push Message 推送的詳細資訊，用於追蹤和除錯

**欄位**:

| 欄位名 | 型別 | 必填 | 說明 | 約束 |
|--------|------|------|------|------|
| Id | int | ✓ | 主鍵（自動遞增） | Primary Key |
| IssueReportId | int | ✓ | 關聯的問題回報單 ID | Foreign Key → IssueReport.Id |
| LineUserId | string | ✓ | 推送目標 LINE 使用者 ID | MaxLength(100) |
| MessageType | string | ✓ | 訊息類型（NewIssue, StatusChanged, AssignmentChanged） | MaxLength(50) |
| Status | string | ✓ | 推送狀態（Success, Failed, Retrying） | MaxLength(20) |
| RetryCount | int | ✓ | 重試次數 | Default: 0 |
| ErrorMessage | string? | | 錯誤訊息（推送失敗時記錄） | MaxLength(1000) |
| PushedAt | DateTime | ✓ | 推送時間 | Default: GETUTCDATE() |
| CreatedAt | DateTime | ✓ | 建立時間（自動管理） | |

**關聯關係**:

- **IssueReport** (N:1): 一個問題回報單可有多筆推送記錄
  - Navigation Property: `IssueReport` (LinePushLog → IssueReport)
  - Foreign Key: `IssueReportId`

**索引**:

- `IX_LinePushLog_IssueReportId`: 快速查詢特定問題的推送歷史
- `IX_LinePushLog_LineUserId_PushedAt`: 快速查詢特定使用者的推送記錄（時間排序）
- `IX_LinePushLog_Status`: 快速查詢失敗推送

**驗證規則**:

- `MessageType` 必須為預定義值：`NewIssue`, `StatusChanged`, `AssignmentChanged`
- `Status` 必須為預定義值：`Success`, `Failed`, `Retrying`
- `RetryCount` 不可為負數

**業務邏輯**:

- 推送成功：`Status = Success`, `ErrorMessage = null`
- 推送失敗：`Status = Failed`, 記錄 `ErrorMessage`
- 重試中：`Status = Retrying`, 每次重試 `RetryCount + 1`
- 最多重試 3 次，超過後標記為 `Failed`

### 3. LineConversationState (LINE 對話狀態)

**用途**: 儲存使用者在 LINE 端回報問題時的對話進度和暫存資料

**欄位**:

| 欄位名 | 型別 | 必填 | 說明 | 約束 |
|--------|------|------|------|------|
| Id | int | ✓ | 主鍵（自動遞增） | Primary Key |
| LineUserId | string | ✓ | LINE 使用者 ID | MaxLength(100) |
| UserId | int | ✓ | 系統使用者 ID（從綁定帶入） | Foreign Key → User.Id |
| CurrentStep | string | ✓ | 當前對話步驟（Enum 序列化） | MaxLength(50) |
| Title | string? | | 暫存：問題標題 | MaxLength(200) |
| Content | string? | | 暫存：問題內容 | MaxLength(2000) |
| DepartmentId | int? | | 暫存：所屬單位 ID | Nullable |
| Priority | string? | | 暫存：緊急程度（Low/Medium/High） | MaxLength(20) |
| CustomerName | string? | | 暫存：聯絡人姓名 | MaxLength(100) |
| CustomerPhone | string? | | 暫存：聯絡電話 | MaxLength(20) |
| ImageUrls | string? | | 暫存：圖片附件 URL（JSON 陣列） | MaxLength(1000) |
| ExpiresAt | DateTime | ✓ | 過期時間（建立後 24 小時） | |
| CreatedAt | DateTime | ✓ | 建立時間（自動管理） | |
| UpdatedAt | DateTime | ✓ | 更新時間（自動管理） | |

**關聯關係**:

- **User** (N:1): 對話狀態屬於特定使用者
  - Navigation Property: `User` (LineConversationState → User)
  - Foreign Key: `UserId`

**索引**:

- `IX_LineConversationState_LineUserId`: 快速查詢特定 LINE 使用者的對話狀態
- `IX_LineConversationState_ExpiresAt`: 支援過期資料清理

**驗證規則**:

- 同一個 `LineUserId` 同時間只能有一個進行中的對話（防止多重對話衝突）
- `CurrentStep` 必須為預定義步驟值（見下方 Enum）
- `ExpiresAt` 自動設定為 `CreatedAt + 24 小時`
- `ImageUrls` 儲存為 JSON 字串，例如：`["url1", "url2", "url3"]`

**對話步驟 Enum (ConversationStep)**:

```csharp
public enum ConversationStep
{
    AskTitle,           // 詢問問題標題
    AskContent,         // 詢問問題內容
    AskDepartment,      // 詢問所屬單位
    AskPriority,        // 詢問緊急程度
    AskCustomerName,    // 詢問聯絡人姓名
    AskCustomerPhone,   // 詢問聯絡電話
    AskImages,          // 詢問是否上傳圖片
    Confirm,            // 確認摘要
    Completed           // 完成（等待清理）
}
```

**狀態流轉**:

```text
AskTitle → AskContent → AskDepartment → AskPriority →
AskCustomerName → AskCustomerPhone → AskImages → Confirm → Completed
```

使用者可在任何步驟輸入「取消」回到初始狀態（刪除對話狀態記錄）。

## 資料關聯圖

```text
User (現有實體)
  ↑ 1
  |
  | 1:1
  ↓
LineBinding
  - LineUserId (Unique)
  - IsActive
  - BoundAt


IssueReport (現有實體)
  ↑ 1
  |
  | 1:N
  ↓
LinePushLog
  - MessageType
  - Status
  - RetryCount


User (現有實體)
  ↑ 1
  |
  | 1:N
  ↓
LineConversationState
  - CurrentStep
  - Title, Content, DepartmentId, Priority...
  - ExpiresAt
```

## Entity Framework Core 配置範例

### LineBindingConfiguration.cs

```csharp
public class LineBindingConfiguration : IEntityTypeConfiguration<LineBinding>
{
    public void Configure(EntityTypeBuilder<LineBinding> builder)
    {
        builder.ToTable("LineBindings");

        builder.HasKey(lb => lb.Id);

        builder.Property(lb => lb.LineUserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(lb => lb.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(lb => lb.PictureUrl)
            .HasMaxLength(500);

        builder.Property(lb => lb.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(lb => lb.BoundAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // 索引
        builder.HasIndex(lb => lb.LineUserId)
            .IsUnique()
            .HasDatabaseName("IX_LineBinding_LineUserId");

        builder.HasIndex(lb => lb.UserId)
            .IsUnique()
            .HasDatabaseName("IX_LineBinding_UserId");

        // 關聯
        builder.HasOne(lb => lb.User)
            .WithOne()
            .HasForeignKey<LineBinding>(lb => lb.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### LinePushLogConfiguration.cs

```csharp
public class LinePushLogConfiguration : IEntityTypeConfiguration<LinePushLog>
{
    public void Configure(EntityTypeBuilder<LinePushLog> builder)
    {
        builder.ToTable("LinePushLogs");

        builder.HasKey(lpl => lpl.Id);

        builder.Property(lpl => lpl.LineUserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(lpl => lpl.MessageType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(lpl => lpl.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(lpl => lpl.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(lpl => lpl.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(lpl => lpl.PushedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // 索引
        builder.HasIndex(lpl => lpl.IssueReportId)
            .HasDatabaseName("IX_LinePushLog_IssueReportId");

        builder.HasIndex(lpl => new { lpl.LineUserId, lpl.PushedAt })
            .HasDatabaseName("IX_LinePushLog_LineUserId_PushedAt");

        builder.HasIndex(lpl => lpl.Status)
            .HasDatabaseName("IX_LinePushLog_Status");

        // 關聯
        builder.HasOne(lpl => lpl.IssueReport)
            .WithMany()
            .HasForeignKey(lpl => lpl.IssueReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### LineConversationStateConfiguration.cs

```csharp
public class LineConversationStateConfiguration : IEntityTypeConfiguration<LineConversationState>
{
    public void Configure(EntityTypeBuilder<LineConversationState> builder)
    {
        builder.ToTable("LineConversationStates");

        builder.HasKey(lcs => lcs.Id);

        builder.Property(lcs => lcs.LineUserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(lcs => lcs.CurrentStep)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(lcs => lcs.Title)
            .HasMaxLength(200);

        builder.Property(lcs => lcs.Content)
            .HasMaxLength(2000);

        builder.Property(lcs => lcs.Priority)
            .HasMaxLength(20);

        builder.Property(lcs => lcs.CustomerName)
            .HasMaxLength(100);

        builder.Property(lcs => lcs.CustomerPhone)
            .HasMaxLength(20);

        builder.Property(lcs => lcs.ImageUrls)
            .HasMaxLength(1000);

        builder.Property(lcs => lcs.ExpiresAt)
            .IsRequired();

        // 索引
        builder.HasIndex(lcs => lcs.LineUserId)
            .HasDatabaseName("IX_LineConversationState_LineUserId");

        builder.HasIndex(lcs => lcs.ExpiresAt)
            .HasDatabaseName("IX_LineConversationState_ExpiresAt");

        // 關聯
        builder.HasOne(lcs => lcs.User)
            .WithMany()
            .HasForeignKey(lcs => lcs.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## 資料遷移注意事項

### 遷移命令

```bash
# 建立遷移
dotnet ef migrations add AddLineTables

# 套用遷移
dotnet ef database update
```

### 遷移影響評估

- **新增 3 個資料表**：不影響現有資料表結構
- **新增外鍵關聯**：參照現有 `Users` 和 `IssueReports` 表，設定 Cascade Delete
- **索引建立**：提升查詢效能，不影響資料完整性
- **預設值**：使用 SQL Server 內建函式 `GETUTCDATE()`

### 回滾計畫

如需回滾此功能：

```bash
# 移除遷移（在套用前）
dotnet ef migrations remove

# 回滾到指定遷移（在套用後）
dotnet ef database update <PreviousMigrationName>
```

## DTO 對應

### LineBindingDto

```csharp
public class LineBindingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LineUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime BoundAt { get; set; }
    public DateTime? UnboundAt { get; set; }
}
```

### LinePushLogDto

```csharp
public class LinePushLogDto
{
    public int Id { get; set; }
    public int IssueReportId { get; set; }
    public string LineUserId { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime PushedAt { get; set; }
}
```

### LineConversationStateDto

```csharp
public class LineConversationStateDto
{
    public int Id { get; set; }
    public string LineUserId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public ConversationStep CurrentStep { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? DepartmentId { get; set; }
    public string? Priority { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public List<string>? ImageUrls { get; set; }  // 從 JSON 字串反序列化
    public DateTime ExpiresAt { get; set; }
}
```

## 資料庫容量估算

**假設**:

- 100 名活躍使用者
- 每月 500 個問題回報
- 對話狀態保留 24 小時

**估算**:

| 資料表 | 每筆記錄大小 | 記錄數（年） | 總容量（年） |
|--------|--------------|--------------|--------------|
| LineBindings | ~200 bytes | 100 | 20 KB |
| LinePushLogs | ~300 bytes | 6,000 (500 * 12) | 1.8 MB |
| LineConversationStates | ~500 bytes | ~10 (任何時刻) | 5 KB |

**總計**: 約 2 MB/年（極小影響）

## 安全性考量

1. **LINE User ID 隱私**: 不對外公開，僅內部使用
2. **圖片 URL 保護**: 使用相對路徑，避免外部直接存取
3. **對話狀態加密**: 如包含敏感資訊，考慮欄位層級加密（目前不需要）
4. **Cascade Delete**: 使用者刪除時自動清理相關 LINE 資料

## 測試資料建議

建立測試資料時可參考以下範例：

```csharp
// LineBinding 測試資料
new LineBinding
{
    UserId = 1,
    LineUserId = "U1234567890abcdef1234567890abcdef",
    DisplayName = "測試使用者",
    PictureUrl = "https://profile.line-scdn.net/...",
    IsActive = true,
    BoundAt = DateTime.UtcNow
}

// LinePushLog 測試資料
new LinePushLog
{
    IssueReportId = 1,
    LineUserId = "U1234567890abcdef1234567890abcdef",
    MessageType = "NewIssue",
    Status = "Success",
    RetryCount = 0,
    PushedAt = DateTime.UtcNow
}

// LineConversationState 測試資料
new LineConversationState
{
    LineUserId = "U1234567890abcdef1234567890abcdef",
    UserId = 1,
    CurrentStep = ConversationStep.AskTitle.ToString(),
    ExpiresAt = DateTime.UtcNow.AddHours(24)
}
```
