using System.Threading;
using System.Threading.Tasks;

namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// 回報單 Token 加密/解密服務介面
    /// </summary>
    public interface IIssueReportTokenService
    {
        /// <summary>
        /// 產生加密的回報單 Token
        /// </summary>
        /// <param name="issueReportId">回報單 ID</param>
        /// <returns>加密後的 Token 字串</returns>
        string GenerateToken(int issueReportId);

        /// <summary>
        /// 驗證並解密 Token
        /// </summary>
        /// <param name="token">加密的 Token</param>
        /// <returns>回報單 ID,若 Token 無效則回傳 null</returns>
        int? ValidateToken(string token);
    }
}
