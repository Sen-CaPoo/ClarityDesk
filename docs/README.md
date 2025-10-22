# ClarityDesk 文件目錄

本目錄包含 ClarityDesk 專案的所有文件資料。

## 📁 目錄結構

### `/docs/deployment` - 部署相關文件
- **DEPLOYMENT.md** - 完整部署指南,包含 IIS 設定、資料庫配置、SSL 憑證等
- **IIS-DEPLOYMENT-CHECKLIST.md** - IIS 部署檢查清單,包含故障排除步驟

### `/docs/development` - 開發指南
- **CONTRIBUTING.md** - 貢獻指南,包含程式碼風格、測試要求、PR 流程
- **AGENTS.md** - AI Agent 協作指引,說明專案架構與開發規範

### `/docs/changelogs` - 變更記錄
- **CHANGELOG-AcceptanceAdjustments-20251022.md** - 驗收調整記錄
- **CHANGELOG-GuestMode-20251023.md** - 訪客模式功能記錄
- **CHANGELOG-IssueFeatures-20251022.md** - 回報單功能記錄
- **CHANGELOG-IssueFilter-20251022.md** - 篩選功能記錄

### `/docs` (根目錄) - 使用者文件
- **user-manual.md** - 使用者手冊
- **guest-mode-guide.md** - 訪客模式使用指南
- **guest-mode-implementation.md** - 訪客模式技術實作說明
- **GUEST-MODE-SUMMARY.md** - 訪客模式功能摘要
- **avatar-fix.md** - 頭像功能修正記錄

## 🗂️ 其他相關目錄

### `/scripts` - 腳本工具
- **commit-changes.ps1** - Git 提交輔助腳本
- **git-commit-helper.ps1** - Git 行尾字元處理腳本

### `/database` - 資料庫腳本
- **MSSQL-結構與範例資料建立檔.sql** - 資料庫結構與範例資料

### `/specs` - 規格文件
- 專案規格、User Stories、API 合約定義等 (不在此目錄中)

## 📖 快速導覽

### 我想要...

#### 🚀 部署系統
→ 閱讀 [`deployment/DEPLOYMENT.md`](deployment/DEPLOYMENT.md)  
→ 使用 [`deployment/IIS-DEPLOYMENT-CHECKLIST.md`](deployment/IIS-DEPLOYMENT-CHECKLIST.md) 檢查清單

#### 💻 參與開發
→ 閱讀 [`development/CONTRIBUTING.md`](development/CONTRIBUTING.md)  
→ 了解 [`development/AGENTS.md`](development/AGENTS.md) 專案架構

#### 📚 學習使用
→ 閱讀 [`user-manual.md`](user-manual.md)  
→ 訪客模式: [`guest-mode-guide.md`](guest-mode-guide.md)

#### 🔍 查看變更歷史
→ 瀏覽 [`changelogs/`](changelogs/) 目錄

#### 🛠️ 使用腳本工具
→ 查看 [`/scripts`](../scripts/) 目錄

#### 🗄️ 設定資料庫
→ 使用 [`/database`](../database/) 中的 SQL 腳本

## 📝 文件更新原則

- **部署文件**: 當部署流程或環境需求變更時更新
- **開發指南**: 當程式碼規範或架構原則變更時更新
- **變更記錄**: 每次重大功能更新時新增 CHANGELOG 檔案,格式為 `CHANGELOG-功能名稱-日期.md`
- **使用者文件**: 當使用者介面或功能流程變更時更新

## 🔗 相關連結

- [專案 README](../README.md) - 專案主頁
- [規格文件](../specs/) - 詳細規格與 API 定義
- [GitHub Repository](https://github.com/Sen-CaPoo/ClarityDesk-2)

---

**最後更新**: 2025-10-23  
**維護者**: ClarityDesk Development Team
