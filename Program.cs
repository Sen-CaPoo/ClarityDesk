using ClarityDesk.Data;
using ClarityDesk.Infrastructure.Middleware;
using ClarityDesk.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClarityDesk
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddRazorPages(options =>
            {
                // 全域授權策略: 所有頁面需登入 (除了明確允許匿名的頁面)
                options.Conventions.AuthorizePage("/Issues/Index");
                options.Conventions.AuthorizePage("/Issues/Create");
                options.Conventions.AuthorizePage("/Issues/Edit");
                options.Conventions.AuthorizePage("/Issues/Details");
                options.Conventions.AuthorizePage("/Admin/Users/Index", "Admin");
                options.Conventions.AuthorizePage("/Admin/Departments/Index", "Admin");
                options.Conventions.AuthorizePage("/Admin/Departments/Create", "Admin");
                options.Conventions.AuthorizePage("/Admin/Departments/Edit", "Admin");

                // 允許匿名訪問的頁面
                options.Conventions.AllowAnonymousToPage("/Account/Login");
                options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
                options.Conventions.AllowAnonymousToPage("/Error");
                options.Conventions.AllowAnonymousToPage("/Index");
                options.Conventions.AllowAnonymousToPage("/Privacy");
            });

            // Add API Controllers (for LINE Webhook)
            builder.Services.AddControllers();

            // 設定 DbContext 使用 SQL Server
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 設定 LINE Login Options
            builder.Services.Configure<LineLoginOptions>(
                builder.Configuration.GetSection("LineLogin"));

            // 設定 LINE Messaging Options (用於綁定頁面的 OAuth)
            builder.Configuration.GetSection("LineMessaging").Bind(new { });

            // 設定 Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "LINE";
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(365); // 永久會話
                options.SlidingExpiration = true;
            })
            .AddOAuth("LINE", options =>
            {
                options.ClientId = builder.Configuration["LineLogin:ChannelId"] ?? "";
                options.ClientSecret = builder.Configuration["LineLogin:ChannelSecret"] ?? "";
                options.CallbackPath = new PathString("/signin-line");

                options.AuthorizationEndpoint = "https://access.line.me/oauth2/v2.1/authorize";
                options.TokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
                options.UserInformationEndpoint = "https://api.line.me/v2/profile";

                options.Scope.Add("profile");
                options.Scope.Add("openid");

                options.SaveTokens = true;

                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                        var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                        // 手動新增 Claims
                        var userId = user.RootElement.GetProperty("userId").GetString();
                        var displayName = user.RootElement.GetProperty("displayName").GetString();
                        var pictureUrl = user.RootElement.TryGetProperty("pictureUrl", out var pic) ? pic.GetString() : "";

                        if (context.Identity != null)
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId ?? ""));
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName ?? ""));
                            if (!string.IsNullOrEmpty(pictureUrl))
                            {
                                context.Identity.AddClaim(new Claim("picture", pictureUrl));
                            }
                        }

                        // 呼叫 AuthenticationService 建立或更新本地使用者記錄
                        var authService = context.HttpContext.RequestServices.GetRequiredService<ClarityDesk.Services.Interfaces.IAuthenticationService>();
                        var lineProfile = new ClarityDesk.Models.DTOs.LineUserProfileDto
                        {
                            UserId = userId ?? "",
                            DisplayName = displayName ?? "",
                            PictureUrl = pictureUrl
                        };

                        var userDto = await authService.LoginOrRegisterWithLineAsync(lineProfile);

                        // 新增使用者 ID 與角色到 Claims
                        if (context.Identity != null)
                        {
                            context.Identity.AddClaim(new Claim("UserId", userDto.Id.ToString()));
                            context.Identity.AddClaim(new Claim(ClaimTypes.Role, userDto.Role.ToString()));
                        }
                    }
                };
            });

            // 設定 Authorization
            builder.Services.AddAuthorization(options =>
            {
                // 建立 Admin 政策
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            });

            // 設定 Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(365); // 永久會話
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            // 設定 Memory Cache
            builder.Services.AddMemoryCache();

            // 設定 Response Compression (Gzip/Brotli)
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
                options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
            });

            builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

            builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

            // 設定 FormOptions 以支援 LINE 圖片上傳 (最多 3 張 x 10MB = 30MB)
            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 31457280; // 30 MB
            });

            // 註冊 HttpClient for LINE Messaging API
            builder.Services.AddHttpClient("LineMessagingAPI", client =>
            {
                client.BaseAddress = new Uri("https://api.line.me/v2/bot/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // 註冊 HttpClient for LINE Content API (圖片/影片/音訊下載)
            builder.Services.AddHttpClient("LineContentAPI", client =>
            {
                client.BaseAddress = new Uri("https://api-data.line.me/v2/bot/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // 註冊服務層
            builder.Services.AddScoped<ClarityDesk.Services.Interfaces.IIssueReportService, ClarityDesk.Services.IssueReportService>();
            builder.Services.AddScoped<ClarityDesk.Services.Interfaces.IAuthenticationService, ClarityDesk.Services.AuthenticationService>();
            builder.Services.AddScoped<ClarityDesk.Services.Interfaces.IUserManagementService, ClarityDesk.Services.UserManagementService>();
            builder.Services.AddScoped<ClarityDesk.Services.Interfaces.IDepartmentService, ClarityDesk.Services.DepartmentService>();
            builder.Services.AddScoped<ClarityDesk.Services.Interfaces.ILineMessagingService, ClarityDesk.Services.LineMessagingService>();

            // 註冊背景服務
            builder.Services.AddHostedService<ClarityDesk.Services.ConversationCleanupService>();

            var app = builder.Build();

            // 初始化資料庫與種子資料
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    await context.Database.MigrateAsync();
                    await ApplicationDbContextSeed.SeedAsync(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "資料庫初始化時發生錯誤");
                }
            }

            // Configure the HTTP request pipeline

            // 使用全域例外處理 Middleware
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // 設定靜態檔案快取 (365天)
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int durationInSeconds = 60 * 60 * 24 * 365; // 365 天
                    ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
                }
            });

            // 啟用 Response Compression
            app.UseResponseCompression();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.MapRazorPages();
            app.MapControllers(); // Map API Controllers for LINE Webhook

            app.Run();
        }
    }
}
