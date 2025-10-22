# Git Commit Helper Script
# 這個腳本會幫助您提交變更並處理行尾字元問題

Write-Host "開始處理 Git 提交..." -ForegroundColor Green

# 1. 重新正規化所有檔案的行尾字元
Write-Host "`n步驟 1: 重新正規化行尾字元..." -ForegroundColor Yellow
git add --renormalize .
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ 行尾字元正規化完成" -ForegroundColor Green
} else {
    Write-Host "✗ 行尾字元正規化失敗" -ForegroundColor Red
    exit 1
}

# 2. 檢查狀態
Write-Host "`n步驟 2: 檢查 Git 狀態..." -ForegroundColor Yellow
git status

# 3. 僅提交新增的設定檔案和文件
Write-Host "`n步驟 3: 加入變更檔案..." -ForegroundColor Yellow
git add .gitattributes .config/dotnet-tools.json IIS-DEPLOYMENT-CHECKLIST.md
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ 檔案已加入暫存區" -ForegroundColor Green
} else {
    Write-Host "✗ 加入檔案失敗" -ForegroundColor Red
    exit 1
}

# 4. 提交變更
Write-Host "`n步驟 4: 提交變更..." -ForegroundColor Yellow
$commitMessage = @"
chore: add .gitattributes and normalize line endings

- Add .gitattributes to enforce LF line endings for text files
- Add .config/dotnet-tools.json for dotnet-ef tool
- Add IIS-DEPLOYMENT-CHECKLIST.md for deployment guidance
"@

git commit -m $commitMessage
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ 提交成功!" -ForegroundColor Green
} else {
    Write-Host "✗ 提交失敗" -ForegroundColor Red
    exit 1
}

# 5. 顯示提交資訊
Write-Host "`n步驟 5: 查看提交資訊..." -ForegroundColor Yellow
git log -1 --stat

Write-Host "`n全部完成! ✓" -ForegroundColor Green
Write-Host "`n接下來您可以執行: git push origin 001-customer-issue-tracker" -ForegroundColor Cyan
