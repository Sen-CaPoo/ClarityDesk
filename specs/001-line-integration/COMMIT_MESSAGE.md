# Git Commit Message (建議)

## 主要 Commit

```
feat(line-integration): 實作 LINE Messaging API 完整整合

新增功能：
- LINE Login OAuth 2.1 帳號綁定流程
- 問題回報單推送通知（新問題/狀態變更/重新指派）
- LINE 對話式問題回報（多步驟互動流程）
- 圖片上傳支援（最多 3 張，10MB/張）
- Webhook 接收與簽章驗證
- 背景服務自動清理過期對話狀態

技術實作：
- 新增 3 個資料表（LineBindings, LinePushLogs, LineConversationStates）
- 新增 21 個檔案（實體、DTO、服務、控制器、Razor Pages）
- 修改 4 個檔案（appsettings.json, Program.cs, ApplicationDbContext.cs, IssueReportService.cs）
- Flex Message 動態卡片設計
- HMAC-SHA256 Webhook 簽章驗證
- ConversationCleanupService (IHostedService) 每小時清理

安全性：
- 輸入驗證與長度限制
- 防止路徑遍歷攻擊
- Fail-safe 推送機制（失敗不影響主流程）
- 重試機制（3 次指數退避）

文件：
- 完整部署檢查清單（DEPLOYMENT.md）
- 實作摘要文件（IMPLEMENTATION_SUMMARY.md）

參考：
- specs/001-line-integration/README.md
- specs/001-line-integration/tasks.md

BREAKING CHANGE: 需執行 EF Core Migration `dotnet ef database update`
```

---

## 詳細 Commit (可選，如需拆分提交)

### 1. Setup & Configuration
```
feat(line-integration): 設定 LINE Messaging 基礎配置

- 新增 appsettings.json LineMessaging 配置區塊
- 建立 wwwroot/uploads/line-images 目錄
- Program.cs 新增 FormOptions 設定 (30MB limit)
```

### 2. Database Schema
```
feat(line-integration): 新增 LINE 相關資料表結構

- 新增 LineBinding, LinePushLog, LineConversationState 實體
- 新增 LinePushStatus, ConversationStep 列舉
- 建立 EF Core Fluent API 配置
- 新增 AddLineTables Migration
```

### 3. LINE Messaging Service
```
feat(line-integration): 實作 LINE Messaging API 核心服務

- 建立 ILineMessagingService 介面
- 實作 LineMessagingService (Push/Reply/Webhook 處理)
- Flex Message 動態建構器
- HMAC-SHA256 簽章驗證
- 重試機制與錯誤處理
```

### 4. LINE Login Binding
```
feat(line-integration): 實作 LINE Login OAuth 綁定功能

- 新增 LineBinding Razor Page (綁定/解綁 UI)
- OAuth 2.1 Authorization Code Flow
- State 驗證防止 CSRF
- 使用者資料同步（DisplayName, PictureUrl）
```

### 5. Push Notifications
```
feat(line-integration): 整合問題回報推送通知

- IssueReportService 新增 LINE 通知邏輯
- 新問題指派通知
- 狀態變更通知
- 重新指派通知
- Fail-safe 設計（Task.Run 非同步執行）
```

### 6. Webhook & Conversation
```
feat(line-integration): 實作 Webhook 與對話式問題回報

- 新增 LineWebhookController API 端點
- 對話狀態機（8 個步驟流程）
- 圖片上傳與儲存（3 張限制）
- Quick Reply 與 Postback 互動
- 確認訊息與問題建立
- 圖片從暫存移至 issues 目錄
```

### 7. Background Cleanup
```
feat(line-integration): 新增對話狀態背景清理服務

- 建立 ConversationCleanupService (IHostedService)
- 每小時清理過期對話（24 小時逾時）
- 自動刪除關聯暫存圖片
- 註冊至 Program.cs Hosted Services
```

### 8. Documentation
```
docs(line-integration): 新增部署與實作文件

- 新增 DEPLOYMENT.md (完整部署檢查清單)
- 新增 IMPLEMENTATION_SUMMARY.md (實作摘要)
- 包含 IIS/Linux 部署步驟
- 功能測試清單
- 故障排除指南
```

---

## 推薦方式

**建議使用單一 Commit**（上方第一個），因為：
1. 這是一個完整的功能模組（LINE Integration）
2. 所有檔案互相依賴，拆分沒有意義
3. 符合 Conventional Commits 規範
4. 便於未來 cherry-pick 或 revert

---

## 執行指令

```bash
# 檢查當前狀態
git status

# 新增所有變更
git add .

# 提交（使用上方建議的 commit message）
git commit -F COMMIT_MESSAGE.txt

# 推送至遠端
git push origin 001-line-integration

# 建立 Pull Request (GitHub CLI)
gh pr create --title "feat: LINE Messaging API Integration" --body "完整實作三個 User Story，詳見 specs/001-line-integration/IMPLEMENTATION_SUMMARY.md"
```

---

## Pull Request 描述範本

```markdown
## 📦 功能摘要

完整實作 ClarityDesk 的 LINE Messaging API 整合，包含：

- ✅ **User Story 1**: LINE 帳號綁定（OAuth 2.1）
- ✅ **User Story 2**: 推送通知（新問題/狀態變更/重新指派）
- ✅ **User Story 3**: 對話式問題回報（多步驟互動 + 圖片上傳）

## 🔧 技術變更

- **新增**: 21 個檔案（實體、服務、控制器、Razor Pages、文件）
- **修改**: 4 個檔案（appsettings, Program.cs, DbContext, IssueReportService）
- **資料庫**: 3 個新資料表（需執行 Migration）

## 🧪 測試狀態

- ✅ 編譯成功（2 個 nullable 警告可忽略）
- ⏳ 單元測試待補充
- ⏳ 整合測試待補充
- 📋 手動測試清單見 `specs/001-line-integration/DEPLOYMENT.md` 第 8 節

## 📖 文件

- [實作摘要](./specs/001-line-integration/IMPLEMENTATION_SUMMARY.md)
- [部署指南](./specs/001-line-integration/DEPLOYMENT.md)
- [API 文件](./specs/001-line-integration/api-integration-details.md)

## ⚠️ Breaking Changes

需執行資料庫 Migration：
```bash
dotnet ef database update
```

## 🚀 部署前準備

1. LINE Developers Console 設定 (取得 Channel credentials)
2. 配置 `appsettings.Production.json`
3. 建立上傳目錄並設定權限
4. 驗證 Webhook 連線

詳見 [DEPLOYMENT.md](./specs/001-line-integration/DEPLOYMENT.md)

## 📝 Checklist

- [x] 程式碼編譯成功
- [x] 遵循專案憲章（最小化變更原則）
- [x] 新增完整文件
- [x] 安全性檢查通過
- [ ] 單元測試（建議後續補充）
- [ ] 人工功能測試（部署後執行）
```

---

**建立日期**: 2025-11-01
**分支**: `001-line-integration`
