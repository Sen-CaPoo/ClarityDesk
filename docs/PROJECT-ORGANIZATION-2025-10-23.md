# 專案目錄整理記錄 (2025-10-23)

## 📋 整理摘要

為了讓專案目錄更加清晰且易於維護,我們將根目錄的文件檔案重新組織到適當的子目錄中。

## 🗂️ 移動檔案清單

### 部署文件 → `docs/deployment/`
- ✅ `DEPLOYMENT.md` → `docs/deployment/DEPLOYMENT.md`
- ✅ `IIS-DEPLOYMENT-CHECKLIST.md` → `docs/deployment/IIS-DEPLOYMENT-CHECKLIST.md`

### 開發指南 → `docs/development/`
- ✅ `CONTRIBUTING.md` → `docs/development/CONTRIBUTING.md`
- ✅ `AGENTS.md` → `docs/development/AGENTS.md`

### 變更記錄 → `docs/changelogs/`
- ✅ `CHANGELOG-AcceptanceAdjustments-20251022.md` → `docs/changelogs/CHANGELOG-AcceptanceAdjustments-20251022.md`
- ✅ `CHANGELOG-GuestMode-20251023.md` → `docs/changelogs/CHANGELOG-GuestMode-20251023.md`
- ✅ `CHANGELOG-IssueFeatures-20251022.md` → `docs/changelogs/CHANGELOG-IssueFeatures-20251022.md`
- ✅ `CHANGELOG-IssueFilter-20251022.md` → `docs/changelogs/CHANGELOG-IssueFilter-20251022.md`

### 腳本工具 → `scripts/`
- ✅ `commit-changes.ps1` → `scripts/commit-changes.ps1`
- ✅ `git-commit-helper.ps1` → `scripts/git-commit-helper.ps1`

### 資料庫腳本 → `database/`
- ✅ `MSSQL-結構與範例資料建立檔.sql` → `database/MSSQL-結構與範例資料建立檔.sql`

## 📂 新增目錄結構

```
ClarityDesk-2/
├── docs/
│   ├── deployment/              # 部署相關文件 (新建)
│   │   ├── DEPLOYMENT.md
│   │   └── IIS-DEPLOYMENT-CHECKLIST.md
│   ├── development/             # 開發指南 (新建)
│   │   ├── CONTRIBUTING.md
│   │   └── AGENTS.md
│   ├── changelogs/              # 變更記錄 (新建)
│   │   ├── CHANGELOG-AcceptanceAdjustments-20251022.md
│   │   ├── CHANGELOG-GuestMode-20251023.md
│   │   ├── CHANGELOG-IssueFeatures-20251022.md
│   │   └── CHANGELOG-IssueFilter-20251022.md
│   ├── README.md                # 文件目錄索引 (新建)
│   ├── user-manual.md           # (保持原位)
│   ├── guest-mode-guide.md      # (保持原位)
│   ├── guest-mode-implementation.md  # (保持原位)
│   ├── GUEST-MODE-SUMMARY.md    # (保持原位)
│   └── avatar-fix.md            # (保持原位)
├── scripts/                      # 腳本工具 (新建)
│   ├── commit-changes.ps1
│   └── git-commit-helper.ps1
├── database/                     # 資料庫腳本 (新建)
│   └── MSSQL-結構與範例資料建立檔.sql
├── README.md                     # 專案主頁 (更新路徑引用)
└── .github/
    └── copilot-instructions.md  # AI Agent 指引 (更新路徑引用)
```

## 📝 更新的檔案引用

### `README.md`
- 更新「專案結構」章節,加入 `docs/`, `scripts/`, `database/` 目錄
- 更新「部署」章節的文件連結
- 新增「文件目錄」章節,統整所有文件入口

### `.github/copilot-instructions.md`
- 更新「重要檔案參考」章節的路徑
- 更新最後更新日期

### `docs/README.md` (新建)
- 建立完整的文件索引
- 提供快速導覽指引
- 說明文件更新原則

## 🎯 整理原則

### 為什麼這樣整理?

1. **部署文件集中管理** (`docs/deployment/`)
   - 所有部署相關的指南與檢查清單放在一起
   - 便於 DevOps 團隊快速找到部署資源

2. **開發指南獨立分類** (`docs/development/`)
   - 貢獻指南與 AI Agent 協作指引分開存放
   - 適合開發者參考的技術文件

3. **變更記錄統一存放** (`docs/changelogs/`)
   - 所有 CHANGELOG 檔案集中管理
   - 按日期命名,易於追蹤功能演進

4. **腳本工具專用目錄** (`scripts/`)
   - PowerShell 腳本獨立存放
   - 避免根目錄混雜執行檔

5. **資料庫腳本分離** (`database/`)
   - SQL 腳本與程式碼分開
   - 便於資料庫管理員使用

### 保持原位的檔案

以下檔案**保持在根目錄**:
- `README.md` - 專案主頁,必須在根目錄
- `appsettings.json` - 應用程式配置,ASP.NET Core 預設位置
- `web.config` - IIS 部署配置,必須在根目錄
- `ClarityDesk.sln`, `ClarityDesk.csproj` - 專案檔案,必須在根目錄
- `Program.cs` - 應用程式入口,必須在根目錄

以下檔案**保持在 `docs/` 根目錄**:
- `user-manual.md` - 使用者手冊,頻繁存取
- `guest-mode-*.md` - 訪客模式相關文件,主題相關性高

## 🔍 如何尋找檔案

### 我想找...

#### 部署相關文件
```bash
docs/deployment/DEPLOYMENT.md
docs/deployment/IIS-DEPLOYMENT-CHECKLIST.md
```

#### 開發相關文件
```bash
docs/development/CONTRIBUTING.md
docs/development/AGENTS.md
```

#### 變更歷史記錄
```bash
docs/changelogs/CHANGELOG-*.md
```

#### 使用者手冊
```bash
docs/user-manual.md
docs/guest-mode-guide.md
```

#### 腳本工具
```bash
scripts/commit-changes.ps1
scripts/git-commit-helper.ps1
```

#### 資料庫腳本
```bash
database/MSSQL-結構與範例資料建立檔.sql
```

## ✅ 驗證檢查清單

- [x] 所有檔案已成功移動
- [x] 新目錄結構已建立
- [x] `README.md` 路徑引用已更新
- [x] `.github/copilot-instructions.md` 路徑引用已更新
- [x] 建立 `docs/README.md` 作為文件索引
- [x] 專案根目錄清爽且結構清晰

## 📊 整理效益

### 整理前
```
ClarityDesk-2/
├── AGENTS.md                     ❌ 開發指南混在根目錄
├── CONTRIBUTING.md               ❌ 貢獻指南混在根目錄
├── DEPLOYMENT.md                 ❌ 部署文件混在根目錄
├── IIS-DEPLOYMENT-CHECKLIST.md  ❌ 部署文件混在根目錄
├── CHANGELOG-*.md (4 個檔案)     ❌ 變更記錄散落根目錄
├── commit-changes.ps1            ❌ 腳本混在根目錄
├── git-commit-helper.ps1         ❌ 腳本混在根目錄
├── MSSQL-結構與範例資料建立檔.sql  ❌ SQL 混在根目錄
└── ... (其他專案檔案)
```

**問題**:
- 根目錄檔案過多,難以快速找到核心專案檔案
- 不同類型的文件混雜,缺乏組織
- 新成員難以理解專案結構

### 整理後
```
ClarityDesk-2/
├── docs/
│   ├── deployment/      ✅ 部署文件集中
│   ├── development/     ✅ 開發指南集中
│   ├── changelogs/      ✅ 變更記錄集中
│   └── *.md            ✅ 使用者文件
├── scripts/             ✅ 腳本工具獨立
├── database/            ✅ 資料庫腳本獨立
├── README.md            ✅ 專案主頁
└── ... (核心專案檔案)
```

**效益**:
- ✅ 根目錄清爽,僅保留必要的專案檔案
- ✅ 文件分類清晰,快速定位所需資源
- ✅ 新成員容易理解專案組織
- ✅ 符合業界最佳實踐 (類似 GitHub 熱門專案結構)

## 🚀 後續維護建議

### 新增文件時的命名規則

1. **部署文件**: 放入 `docs/deployment/`,使用大寫命名 (如 `AWS-DEPLOYMENT.md`)
2. **開發指南**: 放入 `docs/development/`,使用大寫命名 (如 `TESTING-GUIDE.md`)
3. **變更記錄**: 放入 `docs/changelogs/`,格式為 `CHANGELOG-功能名稱-日期.md`
4. **使用者文件**: 放入 `docs/`,使用小寫加連字號 (如 `api-reference.md`)
5. **腳本工具**: 放入 `scripts/`,使用小寫加連字號 (如 `backup-database.ps1`)
6. **資料庫腳本**: 放入 `database/`,描述性命名 (如 `backup-20251023.sql`)

### 避免目錄混亂的規則

- ❌ **不要** 在根目錄新增 `.md` 或 `.ps1` 檔案 (除了 `README.md`)
- ❌ **不要** 在根目錄新增 `.sql` 檔案
- ✅ **務必** 根據檔案類型放入對應的子目錄
- ✅ **務必** 更新 `docs/README.md` 當新增重要文件時

---

**整理日期**: 2025-10-23  
**整理者**: Sen-CaPoo  
**驗證狀態**: ✅ 通過
