/**
 * ClarityDesk 共用 JavaScript 功能
 * 包含表單驗證、AJAX 輔助函式、UI 互動功能
 */

// 等待 DOM 載入完成
document.addEventListener('DOMContentLoaded', function () {
    // 初始化工具提示 (Tooltips)
    initializeTooltips();

    // 初始化確認對話框
    initializeConfirmDialogs();

    // 初始化表單驗證
    initializeFormValidation();

    // 初始化日期選擇器
    initializeDatePickers();
});

/**
 * 初始化 Bootstrap Tooltips
 */
function initializeTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

/**
 * 初始化確認對話框
 * 為所有帶有 data-confirm 屬性的按鈕/連結加入確認對話框
 */
function initializeConfirmDialogs() {
    $('[data-confirm]').on('click', function (e) {
        var message = $(this).data('confirm');
        if (!confirm(message)) {
            e.preventDefault();
            return false;
        }
    });
}

/**
 * 初始化表單驗證
 * 使用 Bootstrap 5 表單驗證樣式
 */
function initializeFormValidation() {
    var forms = document.querySelectorAll('.needs-validation');
    Array.prototype.slice.call(forms).forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
}

/**
 * 初始化日期選擇器
 * 設定日期輸入框的預設值與格式
 */
function initializeDatePickers() {
    // 為所有日期輸入框設定今天為預設值 (如果為空)
    $('input[type="date"]').each(function () {
        if (!$(this).val()) {
            var today = new Date().toISOString().split('T')[0];
            $(this).val(today);
        }
    });
}

/**
 * AJAX 輔助函式 - 發送 POST 請求
 * @param {string} url - 請求 URL
 * @param {object} data - 請求資料
 * @param {function} successCallback - 成功回呼函式
 * @param {function} errorCallback - 錯誤回呼函式
 */
function ajaxPost(url, data, successCallback, errorCallback) {
    showLoadingSpinner();

    $.ajax({
        url: url,
        type: 'POST',
        data: data,
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            hideLoadingSpinner();
            if (successCallback) {
                successCallback(response);
            }
        },
        error: function (xhr, status, error) {
            hideLoadingSpinner();
            if (errorCallback) {
                errorCallback(xhr, status, error);
            } else {
                showErrorAlert('發生錯誤: ' + error);
            }
        }
    });
}

/**
 * AJAX 輔助函式 - 發送 GET 請求
 * @param {string} url - 請求 URL
 * @param {function} successCallback - 成功回呼函式
 * @param {function} errorCallback - 錯誤回呼函式
 */
function ajaxGet(url, successCallback, errorCallback) {
    showLoadingSpinner();

    $.ajax({
        url: url,
        type: 'GET',
        success: function (response) {
            hideLoadingSpinner();
            if (successCallback) {
                successCallback(response);
            }
        },
        error: function (xhr, status, error) {
            hideLoadingSpinner();
            if (errorCallback) {
                errorCallback(xhr, status, error);
            } else {
                showErrorAlert('發生錯誤: ' + error);
            }
        }
    });
}

/**
 * 顯示載入動畫
 */
function showLoadingSpinner() {
    if ($('#loadingSpinner').length === 0) {
        $('body').append(`
            <div id="loadingSpinner" class="position-fixed top-50 start-50 translate-middle" style="z-index: 9999;">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">載入中...</span>
                </div>
            </div>
        `);
    }
}

/**
 * 隱藏載入動畫
 */
function hideLoadingSpinner() {
    $('#loadingSpinner').remove();
}

/**
 * 顯示成功提示訊息
 * @param {string} message - 提示訊息
 */
function showSuccessAlert(message) {
    showAlert(message, 'success');
}

/**
 * 顯示錯誤提示訊息
 * @param {string} message - 錯誤訊息
 */
function showErrorAlert(message) {
    showAlert(message, 'danger');
}

/**
 * 顯示警告提示訊息
 * @param {string} message - 警告訊息
 */
function showWarningAlert(message) {
    showAlert(message, 'warning');
}

/**
 * 顯示資訊提示訊息
 * @param {string} message - 資訊訊息
 */
function showInfoAlert(message) {
    showAlert(message, 'info');
}

/**
 * 顯示提示訊息
 * @param {string} message - 訊息內容
 * @param {string} type - 訊息類型 (success, danger, warning, info)
 */
function showAlert(message, type) {
    var alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3" 
             role="alert" style="z-index: 9999; min-width: 300px;">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="關閉"></button>
        </div>
    `;
    
    $('body').append(alertHtml);
    
    // 3 秒後自動隱藏
    setTimeout(function () {
        $('.alert').fadeOut('slow', function () {
            $(this).remove();
        });
    }, 3000);
}

/**
 * 格式化日期
 * @param {string} dateString - 日期字串
 * @returns {string} 格式化後的日期 (YYYY/MM/DD)
 */
function formatDate(dateString) {
    var date = new Date(dateString);
    var year = date.getFullYear();
    var month = String(date.getMonth() + 1).padStart(2, '0');
    var day = String(date.getDate()).padStart(2, '0');
    return `${year}/${month}/${day}`;
}

/**
 * 格式化日期時間
 * @param {string} dateTimeString - 日期時間字串
 * @returns {string} 格式化後的日期時間 (YYYY/MM/DD HH:mm)
 */
function formatDateTime(dateTimeString) {
    var date = new Date(dateTimeString);
    var year = date.getFullYear();
    var month = String(date.getMonth() + 1).padStart(2, '0');
    var day = String(date.getDate()).padStart(2, '0');
    var hours = String(date.getHours()).padStart(2, '0');
    var minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}/${month}/${day} ${hours}:${minutes}`;
}
