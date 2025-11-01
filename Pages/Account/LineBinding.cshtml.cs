using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClarityDesk.Pages.Account
{
    /// <summary>
    /// LINE 官方帳號綁定頁面
    /// </summary>
    [Authorize]
    public class LineBindingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LineBindingModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public LineBindingModel(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<LineBindingModel> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public LineBindingDto? Binding { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        /// <summary>
        /// 載入綁定狀態
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                ErrorMessage = "無法取得使用者資訊";
                return Page();
            }

            var binding = await _context.LineBindings
                .Where(lb => lb.UserId == userId && lb.IsActive)
                .FirstOrDefaultAsync();

            if (binding != null)
            {
                Binding = new LineBindingDto
                {
                    Id = binding.Id,
                    UserId = binding.UserId,
                    LineUserId = binding.LineUserId,
                    DisplayName = binding.DisplayName,
                    PictureUrl = binding.PictureUrl,
                    IsActive = binding.IsActive,
                    BoundAt = binding.BoundAt,
                    UnboundAt = binding.UnboundAt
                };
            }

            return Page();
        }

        /// <summary>
        /// 發起 LINE Login OAuth 流程 (用於綁定 LINE 官方帳號)
        /// 注意: 使用 LINE Login Channel，與網站登入共用同一個 Channel
        /// </summary>
        public IActionResult OnPostBind()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "無法取得使用者資訊";
                return RedirectToPage();
            }

            // 檢查是否為訪客帳號
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Guest")
            {
                TempData["ErrorMessage"] = "訪客帳號無法綁定 LINE 官方帳號";
                return RedirectToPage();
            }

            // 構建 LINE Login OAuth URL (與網站登入使用同一個 LINE Login Channel)
            var channelId = _configuration["LineLogin:ChannelId"];
            var callbackUrl = Url.Page("/Account/LineBinding", "Callback", null, Request.Scheme);
            var state = Guid.NewGuid().ToString("N");

            // 將 state 和 userId 儲存到 Session 以便驗證
            HttpContext.Session.SetString("LineBindingState", state);
            HttpContext.Session.SetInt32("LineBindingUserId", userId.Value);

            var authUrl = $"https://access.line.me/oauth2/v2.1/authorize?" +
                          $"response_type=code&" +
                          $"client_id={channelId}&" +
                          $"redirect_uri={Uri.EscapeDataString(callbackUrl!)}&" +
                          $"state={state}&" +
                          $"scope=profile%20openid";

            return Redirect(authUrl);
        }

        /// <summary>
        /// 處理 LINE OAuth 回呼
        /// </summary>
        public async Task<IActionResult> OnGetCallbackAsync(string? code, string? state, string? error, string? error_description)
        {
            // 檢查是否有錯誤
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("LINE OAuth 錯誤: {Error} - {Description}", error, error_description);
                TempData["ErrorMessage"] = error == "access_denied" ? "使用者拒絕授權" : $"授權失敗: {error_description}";
                return RedirectToPage();
            }

            // 驗證 state
            var savedState = HttpContext.Session.GetString("LineBindingState");
            if (string.IsNullOrEmpty(savedState) || savedState != state)
            {
                _logger.LogWarning("LINE OAuth state 不符: 預期 {Expected}, 實際 {Actual}", savedState, state);
                TempData["ErrorMessage"] = "驗證失敗，請重試";
                return RedirectToPage();
            }

            var userId = HttpContext.Session.GetInt32("LineBindingUserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "會話已過期，請重試";
                return RedirectToPage();
            }

            try
            {
                // 交換 access token
                var tokenResponse = await ExchangeCodeForTokenAsync(code!);
                if (tokenResponse == null)
                {
                    TempData["ErrorMessage"] = "取得 Access Token 失敗";
                    return RedirectToPage();
                }

                // 從 ID Token 取得使用者資料
                var profile = ExtractProfileFromIdToken(tokenResponse.IdToken);
                if (profile == null)
                {
                    TempData["ErrorMessage"] = "取得 LINE 使用者資料失敗";
                    return RedirectToPage();
                }

                // 檢查此 LINE 帳號是否已被其他使用者綁定（包含非活動狀態）
                var existingBindingByLineUser = await _context.LineBindings
                    .Where(lb => lb.LineUserId == profile.UserId && lb.UserId != userId)
                    .FirstOrDefaultAsync();

                if (existingBindingByLineUser != null)
                {
                    TempData["ErrorMessage"] = "此 LINE 帳號已被其他使用者綁定";
                    return RedirectToPage();
                }

                // 檢查當前使用者是否已有綁定記錄（包含非活動狀態）
                var currentBinding = await _context.LineBindings
                    .Where(lb => lb.UserId == userId)
                    .FirstOrDefaultAsync();

                if (currentBinding != null)
                {
                    // 更新現有綁定（無論是否 IsActive）
                    currentBinding.LineUserId = profile.UserId;
                    currentBinding.DisplayName = profile.DisplayName;
                    currentBinding.PictureUrl = profile.PictureUrl;
                    currentBinding.IsActive = true;
                    currentBinding.BoundAt = DateTime.UtcNow;
                    currentBinding.UnboundAt = null;
                }
                else
                {
                    // 建立新綁定（首次綁定）
                    var newBinding = new LineBinding
                    {
                        UserId = userId.Value,
                        LineUserId = profile.UserId,
                        DisplayName = profile.DisplayName,
                        PictureUrl = profile.PictureUrl,
                        IsActive = true,
                        BoundAt = DateTime.UtcNow
                    };
                    _context.LineBindings.Add(newBinding);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("使用者 {UserId} 成功綁定 LINE 帳號 {LineUserId}", userId, profile.UserId);
                TempData["SuccessMessage"] = "LINE 官方帳號綁定成功！";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "綁定 LINE 帳號時發生錯誤");
                TempData["ErrorMessage"] = "綁定失敗，請稍後再試";
            }
            finally
            {
                // 清除 Session
                HttpContext.Session.Remove("LineBindingState");
                HttpContext.Session.Remove("LineBindingUserId");
            }

            return RedirectToPage();
        }

        /// <summary>
        /// 解除綁定
        /// </summary>
        public async Task<IActionResult> OnPostUnbindAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "無法取得使用者資訊";
                return RedirectToPage();
            }

            try
            {
                var binding = await _context.LineBindings
                    .Where(lb => lb.UserId == userId && lb.IsActive)
                    .FirstOrDefaultAsync();

                if (binding != null)
                {
                    binding.IsActive = false;
                    binding.UnboundAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("使用者 {UserId} 解除 LINE 綁定 {LineUserId}", userId, binding.LineUserId);
                    TempData["SuccessMessage"] = "已成功解除 LINE 官方帳號綁定";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解除 LINE 綁定時發生錯誤");
                TempData["ErrorMessage"] = "解除綁定失敗，請稍後再試";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// 交換 authorization code 為 access token (使用 LINE Login Channel)
        /// </summary>
        private async Task<LineTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var channelId = _configuration["LineLogin:ChannelId"];
                var channelSecret = _configuration["LineLogin:ChannelSecret"];
                var callbackUrl = Url.Page("/Account/LineBinding", "Callback", null, Request.Scheme);

                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", callbackUrl!),
                    new KeyValuePair<string, string>("client_id", channelId!),
                    new KeyValuePair<string, string>("client_secret", channelSecret!)
                });

                var response = await httpClient.PostAsync("https://api.line.me/oauth2/v2.1/token", requestContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("LINE Token 交換失敗: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseBody);
                    return null;
                }

                _logger.LogInformation("LINE Token API 回應: {Response}", responseBody);

                return JsonSerializer.Deserialize<LineTokenResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "呼叫 LINE Token API 時發生例外");
                return null;
            }
        }

        /// <summary>
        /// 從 ID Token 提取使用者資料
        /// </summary>
        private LineUserProfileDto? ExtractProfileFromIdToken(string idToken)
        {
            try
            {
                // ID Token 格式: header.payload.signature
                var parts = idToken.Split('.');
                if (parts.Length != 3)
                {
                    _logger.LogWarning("ID Token 格式不正確");
                    return null;
                }

                // 解碼 payload (Base64Url)
                var payload = parts[1];
                // Base64Url 解碼: 替換字符並補齊 padding
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var jsonBytes = Convert.FromBase64String(payload);
                var json = Encoding.UTF8.GetString(jsonBytes);
                
                _logger.LogInformation("ID Token Payload: {Payload}", json);

                // 解析 JSON
                var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (claims == null)
                {
                    _logger.LogWarning("無法解析 ID Token payload");
                    return null;
                }

                // 提取使用者資訊
                return new LineUserProfileDto
                {
                    UserId = claims.ContainsKey("sub") ? claims["sub"].GetString() ?? "" : "",
                    DisplayName = claims.ContainsKey("name") ? claims["name"].GetString() ?? "" : "",
                    PictureUrl = claims.ContainsKey("picture") ? claims["picture"].GetString() : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 ID Token 時發生錯誤");
                return null;
            }
        }

        /// <summary>
        /// 取得 LINE 使用者資料 (保留作為備用方法)
        /// </summary>
        private async Task<LineUserProfileDto?> GetLineProfileAsync(string accessToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await httpClient.GetAsync("https://api.line.me/v2/profile");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("取得 LINE 使用者資料失敗: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseBody);
                    return null;
                }

                _logger.LogInformation("LINE 使用者資料 API 回應: {Response}", responseBody);

                return JsonSerializer.Deserialize<LineUserProfileDto>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "呼叫 LINE Profile API 時發生例外");
                return null;
            }
        }

        /// <summary>
        /// 取得當前登入使用者 ID
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// LINE Token Response
        /// </summary>
        private class LineTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;
            
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
            
            [JsonPropertyName("id_token")]
            public string IdToken { get; set; } = string.Empty;
            
            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; } = string.Empty;
            
            [JsonPropertyName("scope")]
            public string Scope { get; set; } = string.Empty;
            
            [JsonPropertyName("token_type")]
            public string TokenType { get; set; } = string.Empty;
        }
    }
}
