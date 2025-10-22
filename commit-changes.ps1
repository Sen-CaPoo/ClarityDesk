# Git Commit Script
$title = "feat(issues): 新增篩選重設、Excel匯出與最後修改人追蹤功能"

$body = @"
- 回報單列表：重設按鈕改用 JavaScript 清空所有篩選欄位
- 回報單列表：新增 Excel 下載功能，包含 14 欄完整資料
- 回報單編輯：新增最後修改人記錄 (LastModifiedByUserId)
- 回報單詳情：顯示最後修改人與修改時間
- 資料庫：新增 LastModifiedByUserId 欄位與外鍵 (Migration: AddLastModifiedByUser)
- 新增相依套件：EPPlus 7.0.5 用於 Excel 生成
- 修正 Details.cshtml Razor 語法錯誤與 null 安全檢查
"@

git add -A
git commit -m "$title" -m "$body"
git log -1 --oneline
