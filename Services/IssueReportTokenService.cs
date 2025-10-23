using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace ClarityDesk.Services
{
    /// <summary>
    /// 回報單 Token 加密/解密服務實作
    /// 使用 ASP.NET Core Data Protection API 提供安全的 Token 產生與驗證
    /// </summary>
    public class IssueReportTokenService : IIssueReportTokenService
    {
        private readonly IDataProtector _protector;
        private readonly ILogger<IssueReportTokenService> _logger;
        private const string Purpose = "IssueReportToken";

        public IssueReportTokenService(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<IssueReportTokenService> logger)
        {
            _protector = dataProtectionProvider.CreateProtector(Purpose);
            _logger = logger;
        }

        public string GenerateToken(int issueReportId)
        {
            try
            {
                // 將 ID 與時間戳記組合,防止 Token 被重複使用
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var data = $"{issueReportId}|{timestamp}";
                
                var encryptedData = _protector.Protect(data);
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptedData));

                _logger.LogDebug("產生 Token: IssueReportId={IssueReportId}", issueReportId);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "產生 Token 失敗: IssueReportId={IssueReportId}", issueReportId);
                throw;
            }
        }

        public int? ValidateToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Token 為空值");
                    return null;
                }

                var encryptedData = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var decryptedData = _protector.Unprotect(encryptedData);

                var parts = decryptedData.Split('|');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Token 格式無效");
                    return null;
                }

                if (!int.TryParse(parts[0], out var issueReportId))
                {
                    _logger.LogWarning("無法解析 IssueReportId");
                    return null;
                }

                if (!long.TryParse(parts[1], out var timestamp))
                {
                    _logger.LogWarning("無法解析時間戳記");
                    return null;
                }

                // 檢查 Token 是否過期 (24 小時有效期)
                var tokenAge = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp;
                if (tokenAge > 86400) // 24 hours in seconds
                {
                    _logger.LogWarning("Token 已過期: IssueReportId={IssueReportId}, Age={Age}秒",
                        issueReportId, tokenAge);
                    return null;
                }

                _logger.LogDebug("Token 驗證成功: IssueReportId={IssueReportId}", issueReportId);
                return issueReportId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token 驗證失敗");
                return null;
            }
        }
    }
}
