# 資料模型設計: LINE 整合功能

**Feature Branch**: `002-line-integration`  
**Created**: 2025-10-23  
**Status**: Design Complete

本文件定義 LINE 整合功能所需的資料庫實體、欄位規格、關聯關係與驗證規則。

---

## ERD (Entity Relationship Diagram)

```
┌─────────────────────┐
│       User          │
│  (現有實體)          │
├─────────────────────┤
│ Id (PK)             │
│ Username            │
│ Email               │
│ Role                │
│ IsGuest             │
│ ...                 │
└─────────────────────┘
         │ 1
         │
         │ 1:0..1
         ▼
┌─────────────────────────────┐
│      LineBinding            │
│   (LINE 帳號綁定)            │
├─────────────────────────────┤
│ Id (PK)                     │
│ UserId (FK) → User.Id       │
│ LineUserId (UNIQUE)         │
│ DisplayName                 │
│ PictureUrl                  │
│ BindingStatus               │
│ BoundAt                     │
│ LastInteractedAt            │
│ CreatedAt                   │
│ UpdatedAt                   │
└─────────────────────────────┘
         │ 1
         │
         │ 1:0..n
         ▼
┌──────────────────────────────────┐
│   LineConversationSession        │
│   (LINE 對話 Session)             │
├──────────────────────────────────┤
│ Id (PK)                          │
│ LineUserId (FK) → LineBinding    │
│ UserId (FK) → User.Id            │
│ CurrentStep                      │
│ SessionData (JSON)               │
│ CreatedAt                        │
│ UpdatedAt                        │
│ ExpiresAt                        │
└──────────────────────────────────┘

┌─────────────────────────────┐
│    LineMessageLog            │
│  (LINE 訊息發送日誌)          │
├─────────────────────────────┤
│ Id (PK)                     │
│ LineUserId                  │
│ MessageType                 │
│ Direction                   │
│ Content (JSON)              │
│ IsSuccess                   │
│ ErrorCode                   │
│ ErrorMessage                │
│ SentAt                      │
│ IssueReportId (FK, NULL)    │
└─────────────────────────────┘
         │
         │ 0..n : 0..1
         ▼
┌─────────────────────────┐
│    IssueReport          │
│  (問題回報單 - 現有)     │
├─────────────────────────┤
│ Id (PK)                 │
│ IssueNumber             │
│ Title                   │
│ Description             │
│ Urgency                 │
│ Status                  │
│ Source (新增欄位)        │
│ ...                     │
└─────────────────────────┘
```

---

## 實體詳細規格

### 1. LineBinding (LINE 帳號綁定)

**用途**: 儲存 ClarityDesk 使用者與 LINE 帳號的綁定關係,支援推送通知與 LINE 端操作。

#### 屬性

| 欄位名稱 | 資料型別 | 長度/精度 | 必填 | 唯一 | 預設值 | 說明 |
|---------|---------|----------|-----|-----|-------|------|
| `Id` | `int` | - | ✅ | ✅ (PK) | Identity | 主鍵,自動遞增 |
| `UserId` | `int` | - | ✅ | ✅ (Unique Index) | - | ClarityDesk 使用者 ID,外鍵關聯至 `User.Id` |
| `LineUserId` | `string` | 50 | ✅ | ✅ (Unique Index) | - | LINE Platform 回傳的唯一使用者識別碼 (格式: `U1234567890abcdef1234567890abcdef`) |
| `DisplayName` | `string` | 100 | ✅ | ❌ | - | LINE 使用者的顯示名稱 |
| `PictureUrl` | `string` | 500 | ❌ | ❌ | `NULL` | LINE 使用者的頭像 URL (可選) |
| `BindingStatus` | `enum` | - | ✅ | ❌ | `Active` | 綁定狀態: `Active` (正常), `Blocked` (使用者封鎖官方帳號), `Unbound` (已解除綁定) |
| `BoundAt` | `DateTime` | - | ✅ | ❌ | - | 首次綁定的時間 (UTC) |
| `LastInteractedAt` | `DateTime` | - | ✅ | ❌ | - | 最後一次與 LINE Bot 互動的時間 (用於活躍度分析) |
| `CreatedAt` | `DateTime` | - | ✅ | ❌ | `GETUTCDATE()` | 記錄建立時間 |
| `UpdatedAt` | `DateTime` | - | ✅ | ❌ | `GETUTCDATE()` | 記錄最後更新時間 |

#### 關聯

- **User (一對一)**: `LineBinding.UserId` → `User.Id` (刪除使用者時級聯刪除綁定)
- **LineConversationSession (一對多)**: 一個綁定可有多個進行中或歷史 Session

#### 驗證規則

- **唯一性**: 
  - 一個 `UserId` 只能有一筆 `Active` 狀態的綁定記錄
  - 一個 `LineUserId` 在整個系統中必須唯一 (防止一個 LINE 帳號綁定多個 ClarityDesk 帳號)
- **格式**: `LineUserId` 必須符合 LINE 格式 (U + 32 個十六進位字元)
- **長度**: `DisplayName` 長度介於 1-100 個字元

#### 索引設計

```sql
-- 主鍵索引
PRIMARY KEY (Id)

-- 唯一索引 (確保一個使用者只有一個綁定)
CREATE UNIQUE INDEX IX_LineBinding_UserId ON LineBinding(UserId) WHERE BindingStatus = 'Active';

-- 唯一索引 (確保一個 LINE 帳號只能綁定一個使用者)
CREATE UNIQUE INDEX IX_LineBinding_LineUserId ON LineBinding(LineUserId);

-- 查詢索引 (依狀態查詢)
CREATE INDEX IX_LineBinding_Status ON LineBinding(BindingStatus);
```

#### Entity Configuration (EF Core)

```csharp
// Data/Configurations/LineBindingConfiguration.cs
public class LineBindingConfiguration : IEntityTypeConfiguration<LineBinding>
{
    public void Configure(EntityTypeBuilder<LineBinding> builder)
    {
        builder.ToTable("LineBindings");
        
        builder.HasKey(lb => lb.Id);
        
        builder.Property(lb => lb.LineUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(lb => lb.DisplayName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(lb => lb.PictureUrl)
            .HasMaxLength(500);
        
        builder.Property(lb => lb.BindingStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(lb => lb.BoundAt)
            .IsRequired();
        
        builder.Property(lb => lb.LastInteractedAt)
            .IsRequired();
        
        builder.Property(lb => lb.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        builder.Property(lb => lb.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        // 關聯
        builder.HasOne(lb => lb.User)
            .WithOne(u => u.LineBinding)
            .HasForeignKey<LineBinding>(lb => lb.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // 索引
        builder.HasIndex(lb => lb.UserId)
            .IsUnique()
            .HasFilter($"[{nameof(LineBinding.BindingStatus)}] = 'Active'");
        
        builder.HasIndex(lb => lb.LineUserId)
            .IsUnique();
        
        builder.HasIndex(lb => lb.BindingStatus);
    }
}
```

---

### 2. LineConversationSession (LINE 對話 Session)

**用途**: 持久化儲存使用者在 LINE 端進行回報問題時的對話狀態,支援逾時清理與資料恢復。

#### 屬性

| 欄位名稱 | 資料型別 | 長度/精度 | 必填 | 唯一 | 預設值 | 說明 |
|---------|---------|----------|-----|-----|-------|------|
| `Id` | `Guid` | - | ✅ | ✅ (PK) | `NEWID()` | 主鍵,GUID 格式 |
| `LineUserId` | `string` | 50 | ✅ | ❌ | - | LINE 使用者 ID (關聯至 `LineBinding`) |
| `UserId` | `int` | - | ✅ | ❌ | - | ClarityDesk 使用者 ID (外鍵) |
| `CurrentStep` | `enum` | - | ✅ | ❌ | `AwaitingTitle` | 當前對話步驟 (詳見下方說明) |
| `SessionData` | `string` | MAX (JSON) | ✅ | ❌ | `'{}'` | JSON 格式的暫存資料 (問題標題、內容、單位等) |
| `CreatedAt` | `DateTime` | - | ✅ | ❌ | `GETUTCDATE()` | Session 建立時間 |
| `UpdatedAt` | `DateTime` | - | ✅ | ❌ | `GETUTCDATE()` | 最後更新時間 |
| `ExpiresAt` | `DateTime` | - | ✅ | ❌ | `CreatedAt + 30 分鐘` | 過期時間,超過此時間自動清理 |

#### CurrentStep 列舉值

```csharp
public enum ConversationStep
{
    AwaitingTitle,          // 等待使用者輸入問題標題
    AwaitingDescription,    // 等待輸入問題內容
    AwaitingDepartment,     // 等待選擇所屬單位
    AwaitingUrgency,        // 等待選擇緊急程度
    AwaitingContactName,    // 等待輸入聯絡人姓名
    AwaitingContactPhone,   // 等待輸入連絡電話
    AwaitingConfirmation,   // 顯示摘要,等待確認或取消
    Completed               // 回報單已建立,Session 即將刪除
}
```

#### SessionData JSON 結構

```json
{
  "title": "問題標題範例",
  "description": "問題詳細描述",
  "departmentId": 3,
  "urgency": "High",
  "contactName": "王小明",
  "contactPhone": "0912-345678"
}
```

#### 關聯

- **User (多對一)**: `LineConversationSession.UserId` → `User.Id`
- **LineBinding (多對一)**: `LineConversationSession.LineUserId` → `LineBinding.LineUserId`

#### 驗證規則

- **唯一性**: 一個 `LineUserId` 同時只能有一個未過期的 Session (透過應用程式邏輯強制)
- **過期時間**: `ExpiresAt` 必須大於 `CreatedAt`
- **JSON 資料**: `SessionData` 必須為有效 JSON 格式 (透過應用程式驗證)

#### 索引設計

```sql
-- 主鍵
PRIMARY KEY (Id)

-- 查詢索引 (依 LineUserId 查詢活躍 Session)
CREATE INDEX IX_LineConversationSession_LineUserId ON LineConversationSession(LineUserId, ExpiresAt);

-- 清理索引 (定期清理過期 Session)
CREATE INDEX IX_LineConversationSession_ExpiresAt ON LineConversationSession(ExpiresAt);
```

#### Entity Configuration

```csharp
public class LineConversationSessionConfiguration : IEntityTypeConfiguration<LineConversationSession>
{
    public void Configure(EntityTypeBuilder<LineConversationSession> builder)
    {
        builder.ToTable("LineConversationSessions");
        
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("NEWID()");
        
        builder.Property(s => s.LineUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(s => s.CurrentStep)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        
        builder.Property(s => s.SessionData)
            .HasColumnType("NVARCHAR(MAX)")
            .IsRequired();
        
        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        builder.Property(s => s.ExpiresAt)
            .IsRequired();
        
        // 關聯
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // 索引
        builder.HasIndex(s => new { s.LineUserId, s.ExpiresAt });
        builder.HasIndex(s => s.ExpiresAt);
    }
}
```

---

### 3. LineMessageLog (LINE 訊息發送日誌)

**用途**: 記錄所有與 LINE Platform 的訊息互動,用於除錯、配額監控與統計分析。

#### 屬性

| 欄位名稱 | 資料型別 | 長度/精度 | 必填 | 唯一 | 預設值 | 說明 |
|---------|---------|----------|-----|-----|-------|------|
| `Id` | `Guid` | - | ✅ | ✅ (PK) | `NEWID()` | 主鍵 |
| `LineUserId` | `string` | 50 | ✅ | ❌ | - | 目標 LINE 使用者 ID |
| `MessageType` | `enum` | - | ✅ | ❌ | - | 訊息類型: `Push` (主動推送), `Reply` (回覆使用者), `Multicast` (多播) |
| `Direction` | `enum` | - | ✅ | ❌ | - | 方向: `Outbound` (系統發送), `Inbound` (使用者傳入) |
| `Content` | `string` | MAX (JSON) | ✅ | ❌ | - | 訊息內容 (JSON 格式,包含完整訊息物件) |
| `IsSuccess` | `bool` | - | ✅ | ❌ | - | 發送是否成功 |
| `ErrorCode` | `string` | 50 | ❌ | ❌ | `NULL` | LINE API 錯誤代碼 (例如 `401`, `403`) |
| `ErrorMessage` | `string` | 500 | ❌ | ❌ | `NULL` | 錯誤訊息描述 |
| `SentAt` | `DateTime` | - | ✅ | ❌ | `GETUTCDATE()` | 發送時間 (UTC) |
| `IssueReportId` | `int?` | - | ❌ | ❌ | `NULL` | 關聯的回報單 ID (若訊息與特定回報單相關) |

#### MessageType 列舉值

```csharp
public enum LineMessageType
{
    Push,       // 主動推送 (計入配額)
    Reply,      // 回覆使用者訊息 (不計入配額)
    Multicast   // 多播 (向多個使用者推送相同訊息,計入配額)
}
```

#### Direction 列舉值

```csharp
public enum MessageDirection
{
    Outbound,   // 系統發送至 LINE
    Inbound     // 使用者發送至系統
}
```

#### 關聯

- **IssueReport (多對一)**: `LineMessageLog.IssueReportId` → `IssueReport.Id` (可選)

#### 驗證規則

- **錯誤欄位**: 當 `IsSuccess = false` 時,`ErrorCode` 或 `ErrorMessage` 至少需填寫一個
- **JSON 格式**: `Content` 必須為有效 JSON

#### 索引設計

```sql
-- 主鍵
PRIMARY KEY (Id)

-- 查詢索引 (依時間範圍與類型統計配額)
CREATE INDEX IX_LineMessageLog_SentAt_Type ON LineMessageLog(SentAt, MessageType, IsSuccess);

-- 查詢索引 (依使用者查詢歷史訊息)
CREATE INDEX IX_LineMessageLog_LineUserId ON LineMessageLog(LineUserId, SentAt DESC);

-- 外鍵索引
CREATE INDEX IX_LineMessageLog_IssueReportId ON LineMessageLog(IssueReportId) WHERE IssueReportId IS NOT NULL;
```

#### Entity Configuration

```csharp
public class LineMessageLogConfiguration : IEntityTypeConfiguration<LineMessageLog>
{
    public void Configure(EntityTypeBuilder<LineMessageLog> builder)
    {
        builder.ToTable("LineMessageLogs");
        
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).HasDefaultValueSql("NEWID()");
        
        builder.Property(log => log.LineUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(log => log.MessageType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(log => log.Direction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(log => log.Content)
            .HasColumnType("NVARCHAR(MAX)")
            .IsRequired();
        
        builder.Property(log => log.IsSuccess)
            .IsRequired();
        
        builder.Property(log => log.ErrorCode)
            .HasMaxLength(50);
        
        builder.Property(log => log.ErrorMessage)
            .HasMaxLength(500);
        
        builder.Property(log => log.SentAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        // 關聯
        builder.HasOne(log => log.IssueReport)
            .WithMany()
            .HasForeignKey(log => log.IssueReportId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // 索引
        builder.HasIndex(log => new { log.SentAt, log.MessageType, log.IsSuccess });
        builder.HasIndex(log => new { log.LineUserId, log.SentAt });
        builder.HasIndex(log => log.IssueReportId)
            .HasFilter($"[{nameof(LineMessageLog.IssueReportId)}] IS NOT NULL");
    }
}
```

---

### 4. IssueReport (問題回報單 - 新增欄位)

**修改內容**: 在現有 `IssueReport` 實體新增 `Source` 欄位,追蹤回報單的建立來源。

#### 新增欄位

| 欄位名稱 | 資料型別 | 長度/精度 | 必填 | 唯一 | 預設值 | 說明 |
|---------|---------|----------|-----|-----|-------|------|
| `Source` | `enum` | - | ✅ | ❌ | `Web` | 回報單建立來源: `Web` (網頁端), `Line` (LINE 端) |

#### Source 列舉值

```csharp
public enum IssueReportSource
{
    Web,   // 透過網頁端建立
    Line   // 透過 LINE 對話建立
}
```

#### 修改 Entity Configuration

```csharp
// 在現有的 IssueReportConfiguration.cs 中新增
builder.Property(ir => ir.Source)
    .HasConversion<string>()
    .HasMaxLength(20)
    .HasDefaultValue(IssueReportSource.Web)
    .IsRequired();

// 新增索引 (用於統計不同來源的回報單數量)
builder.HasIndex(ir => ir.Source);
```

#### Migration Script

```csharp
// 新增 Migration
public partial class AddLineIntegrationEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. 新增 Source 欄位至 IssueReports (預設為 Web)
        migrationBuilder.AddColumn<string>(
            name: "Source",
            table: "IssueReports",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Web");
        
        // 2. 建立 LineBindings 資料表
        migrationBuilder.CreateTable(
            name: "LineBindings",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(nullable: false),
                LineUserId = table.Column<string>(maxLength: 50, nullable: false),
                DisplayName = table.Column<string>(maxLength: 100, nullable: false),
                PictureUrl = table.Column<string>(maxLength: 500, nullable: true),
                BindingStatus = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Active"),
                BoundAt = table.Column<DateTime>(nullable: false),
                LastInteractedAt = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LineBindings", x => x.Id);
                table.ForeignKey("FK_LineBindings_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
        
        // 3. 建立 LineConversationSessions 資料表
        migrationBuilder.CreateTable(
            name: "LineConversationSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false, defaultValueSql: "NEWID()"),
                LineUserId = table.Column<string>(maxLength: 50, nullable: false),
                UserId = table.Column<int>(nullable: false),
                CurrentStep = table.Column<string>(maxLength: 30, nullable: false),
                SessionData = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "{}"),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                ExpiresAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LineConversationSessions", x => x.Id);
                table.ForeignKey("FK_LineConversationSessions_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
        
        // 4. 建立 LineMessageLogs 資料表
        migrationBuilder.CreateTable(
            name: "LineMessageLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false, defaultValueSql: "NEWID()"),
                LineUserId = table.Column<string>(maxLength: 50, nullable: false),
                MessageType = table.Column<string>(maxLength: 20, nullable: false),
                Direction = table.Column<string>(maxLength: 20, nullable: false),
                Content = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                IsSuccess = table.Column<bool>(nullable: false),
                ErrorCode = table.Column<string>(maxLength: 50, nullable: true),
                ErrorMessage = table.Column<string>(maxLength: 500, nullable: true),
                SentAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                IssueReportId = table.Column<int>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LineMessageLogs", x => x.Id);
                table.ForeignKey("FK_LineMessageLogs_IssueReports_IssueReportId", x => x.IssueReportId, "IssueReports", "Id", onDelete: ReferentialAction.SetNull);
            });
        
        // 5. 建立索引
        migrationBuilder.CreateIndex("IX_IssueReports_Source", "IssueReports", "Source");
        migrationBuilder.CreateIndex("IX_LineBindings_UserId", "LineBindings", "UserId", unique: true, filter: "[BindingStatus] = 'Active'");
        migrationBuilder.CreateIndex("IX_LineBindings_LineUserId", "LineBindings", "LineUserId", unique: true);
        migrationBuilder.CreateIndex("IX_LineBindings_Status", "LineBindings", "BindingStatus");
        migrationBuilder.CreateIndex("IX_LineConversationSessions_LineUserId_ExpiresAt", "LineConversationSessions", new[] { "LineUserId", "ExpiresAt" });
        migrationBuilder.CreateIndex("IX_LineConversationSessions_ExpiresAt", "LineConversationSessions", "ExpiresAt");
        migrationBuilder.CreateIndex("IX_LineMessageLogs_SentAt_Type_Success", "LineMessageLogs", new[] { "SentAt", "MessageType", "IsSuccess" });
        migrationBuilder.CreateIndex("IX_LineMessageLogs_LineUserId_SentAt", "LineMessageLogs", new[] { "LineUserId", "SentAt" });
        migrationBuilder.CreateIndex("IX_LineMessageLogs_IssueReportId", "LineMessageLogs", "IssueReportId", filter: "[IssueReportId] IS NOT NULL");
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("LineMessageLogs");
        migrationBuilder.DropTable("LineConversationSessions");
        migrationBuilder.DropTable("LineBindings");
        migrationBuilder.DropColumn("Source", "IssueReports");
    }
}
```

---

## 資料完整性與約束

### 資料庫層級約束

1. **外鍵約束**:
   - `LineBinding.UserId` → `User.Id` (CASCADE DELETE)
   - `LineConversationSession.UserId` → `User.Id` (CASCADE DELETE)
   - `LineMessageLog.IssueReportId` → `IssueReport.Id` (SET NULL)

2. **唯一性約束**:
   - `LineBinding.LineUserId` 必須唯一
   - `LineBinding.UserId` 在 `BindingStatus = Active` 時必須唯一

3. **檢查約束**:
   - `LineConversationSession.ExpiresAt > CreatedAt`
   - `LineBinding.DisplayName` 長度 ≥ 1

### 應用程式層級驗證

1. **LineBinding**:
   - `LineUserId` 格式驗證 (正規表示式: `^U[0-9a-f]{32}$`)
   - 禁止訪客帳號建立綁定 (`User.IsGuest = true`)

2. **LineConversationSession**:
   - `SessionData` JSON 格式驗證
   - 單一使用者同時只能有一個活躍 Session

3. **LineMessageLog**:
   - 當 `IsSuccess = false` 時,`ErrorCode` 或 `ErrorMessage` 必填

---

## 效能考量

### 查詢最佳化

1. **快取策略**:
   - 單位清單 (用於 LINE 快速回覆按鈕): 快取 1 小時
   - 使用者綁定狀態: 快取 5 分鐘

2. **批次操作**:
   - Session 清理使用批次刪除 (`DELETE WHERE ExpiresAt < @now`)
   - 訊息日誌查詢使用分頁 (每頁 50 筆)

3. **非同步處理**:
   - 推送訊息後記錄日誌使用 Fire-and-Forget 模式
   - Session 清理透過背景服務執行,不阻塞主執行緒

### 預估資料量

| 實體 | 首年預估 | 成長率 | 備註 |
|-----|---------|-------|------|
| `LineBinding` | 300 | 20%/年 | 綁定後不常變動 |
| `LineConversationSession` | 50-100 (並行) | - | 定期清理,不累積 |
| `LineMessageLog` | 60,000 | 50%/年 | 需定期封存或清理歷史資料 |

**建議**: `LineMessageLog` 應實作資料封存機制,保留最近 3 個月資料於主表,舊資料移至封存表或刪除。

---

## 安全性考量

1. **敏感資料保護**:
   - `LineBinding.LineUserId` 加密儲存 (使用 EF Core Value Converter + Data Protection API) - **可選**
   - `SessionData` 不應包含信用卡號、密碼等高敏感資訊

2. **存取控制**:
   - 使用者僅能查詢/修改自己的 `LineBinding` 與 `LineConversationSession`
   - 管理員可查看所有綁定與日誌,但不可修改

3. **稽核記錄**:
   - `LineMessageLog` 作為稽核記錄,不允許刪除 (僅允許封存)

---

## 總結

本資料模型設計完整涵蓋 LINE 整合功能的三大核心需求:

1. **帳號綁定**: `LineBinding` 實體管理一對一關聯與狀態追蹤
2. **推送通知**: `LineMessageLog` 記錄所有訊息互動,支援配額監控
3. **LINE 端回報**: `LineConversationSession` 持久化對話狀態,支援逾時清理

所有實體均遵循專案現有的命名慣例、索引策略與 EF Core Code First 工作流程。
