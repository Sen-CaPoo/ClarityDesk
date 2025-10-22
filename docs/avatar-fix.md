# 頭像顯示修復說明

## 問題描述

遊客帳號登入時，由於沒有 `PictureUrl`，導致網站右上角的頭像圖片破圖。

## 解決方案

### 實作邏輯

在 `_Layout.cshtml` 中實作智慧型頭像顯示：

1. **有頭像圖片時**（LINE 使用者）
   - 顯示實際頭像圖片
   - 如果圖片載入失敗，自動切換為預設圖示

2. **無頭像圖片時**（遊客使用者）
   - 直接顯示圓形背景的人物圖示
   - 使用 Bootstrap Icons 的 `bi-person-fill` 圖示

### 視覺效果

#### LINE 使用者
```
┌────────┐
│  照片  │ 使用者名稱 [登出]
└────────┘
```

#### 遊客使用者
```
┌────────┐
│   👤   │ 訪客 [登出]
└────────┘
灰色圓形背景 + 白色人物圖示
```

### 技術細節

#### 預設圖示樣式
```html
<span class="rounded-circle bg-secondary text-white d-inline-flex align-items-center justify-content-center me-2" 
      style="width: 32px; height: 32px;">
    <i class="bi bi-person-fill"></i>
</span>
```

#### 圖片載入失敗處理
```html
<img src="@pictureUrl" alt="頭像" class="rounded-circle me-2" 
     style="width: 32px; height: 32px; object-fit: cover;" 
     onerror="this.style.display='none'; this.nextElementSibling.style.display='inline-flex';" />
```

### 樣式特點

- **圓形顯示**：使用 `rounded-circle` class
- **固定尺寸**：32px × 32px
- **垂直對齊**：使用 flexbox 確保圖示居中
- **顏色配置**：灰色背景 (`bg-secondary`)、白色圖示 (`text-white`)
- **圖片適配**：使用 `object-fit: cover` 確保圖片不變形

## 修改的檔案

- `Pages/Shared/_Layout.cshtml`

## 相容性

- ✅ LINE 登入使用者：顯示實際頭像
- ✅ 遊客使用者：顯示預設圖示
- ✅ 圖片載入失敗：自動降級為圖示
- ✅ 響應式設計：在各種螢幕尺寸下正常顯示

## 變更日期

- **2025-10-23**: 修復遊客帳號頭像破圖問題
