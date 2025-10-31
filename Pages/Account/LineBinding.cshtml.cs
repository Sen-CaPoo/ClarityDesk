using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

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
        /// 發起 LINE Login OAuth 流程
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

            // 構建 LINE Login OAuth URL
            var channelId = _configuration["LineMessaging:ChannelId"];
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

                // 取得 LINE 使用者資料
                var profile = await GetLineProfileAsync(tokenResponse.AccessToken);
                if (profile == null)
                {
                    TempData["ErrorMessage"] = "取得 LINE 使用者資料失敗";
                    return RedirectToPage();
                }

                // 檢查此 LINE 帳號是否已被其他使用者綁定
                var existingBinding = await _context.LineBindings
                    .Where(lb => lb.LineUserId == profile.UserId && lb.IsActive && lb.UserId != userId)
                    .FirstOrDefaultAsync();

                if (existingBinding != null)
                {
                    TempData["ErrorMessage"] = "此 LINE 帳號已被其他使用者綁定";
                    return RedirectToPage();
                }

                // 檢查當前使用者是否已有綁定
                var currentBinding = await _context.LineBindings
                    .Where(lb => lb.UserId == userId && lb.IsActive)
                    .FirstOrDefaultAsync();

                if (currentBinding != null)
                {
                    // 更新現有綁定
                    currentBinding.LineUserId = profile.UserId;
                    currentBinding.DisplayName = profile.DisplayName;
                    currentBinding.PictureUrl = profile.PictureUrl;
                    currentBinding.BoundAt = DateTime.UtcNow;
                }
                else
                {
                    // 建立新綁定
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
        /// 交換 authorization code 為 access token
        /// </summary>
        private async Task<LineTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var channelId = _configuration["LineMessaging:ChannelId"];
            var channelSecret = _configuration["LineMessaging:ChannelSecret"];
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
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LINE Token 交換失敗: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LineTokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// 取得 LINE 使用者資料
        /// </summary>
        private async Task<LineUserProfileDto?> GetLineProfileAsync(string accessToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await httpClient.GetAsync("https://api.line.me/v2/profile");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("取得 LINE 使用者資料失敗: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LineUserProfileDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
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
            public string AccessToken { get; set; } = string.Empty;
            public int ExpiresIn { get; set; }
            public string IdToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string Scope { get; set; } = string.Empty;
            public string TokenType { get; set; } = string.Empty;
        }
    }
}
