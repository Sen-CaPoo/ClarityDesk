-- ===================================================
-- 簡化問題狀態資料遷移腳本
-- 日期: 2025-10-22
-- 說明: 將 IssueReports 表中的 'InProgress' 狀態更新為 'Completed'
-- ===================================================

USE [ClarityDesk]  -- 請根據實際資料庫名稱修改
GO

-- 開始交易
BEGIN TRANSACTION;

BEGIN TRY
    -- 1. 備份現有資料（可選，但建議）
    PRINT '正在備份受影響的記錄...';
    SELECT *
    INTO IssueReports_Backup_20251022
    FROM IssueReports
    WHERE Status = 'InProgress';
    
    DECLARE @AffectedRows INT = @@ROWCOUNT;
    PRINT '已備份 ' + CAST(@AffectedRows AS VARCHAR) + ' 筆記錄';

    -- 2. 更新狀態
    PRINT '正在更新狀態...';
    UPDATE IssueReports
    SET Status = 'Completed',
        UpdatedAt = GETUTCDATE()
    WHERE Status = 'InProgress';
    
    SET @AffectedRows = @@ROWCOUNT;
    PRINT '已更新 ' + CAST(@AffectedRows AS VARCHAR) + ' 筆記錄';

    -- 3. 驗證更新結果
    IF EXISTS (SELECT 1 FROM IssueReports WHERE Status = 'InProgress')
    BEGIN
        RAISERROR('發現仍有 InProgress 狀態的記錄，交易將回滾', 16, 1);
    END

    -- 提交交易
    COMMIT TRANSACTION;
    PRINT '資料遷移成功完成！';
    
    -- 顯示更新後的狀態分布
    PRINT '';
    PRINT '目前狀態分布：';
    SELECT 
        Status,
        COUNT(*) AS Count
    FROM IssueReports
    GROUP BY Status
    ORDER BY Status;

END TRY
BEGIN CATCH
    -- 發生錯誤時回滾
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    PRINT '錯誤: ' + @ErrorMessage;
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

GO

-- ===================================================
-- 清理備份表（在確認一切正常後可執行）
-- ===================================================
-- DROP TABLE IF EXISTS IssueReports_Backup_20251022;
-- GO
