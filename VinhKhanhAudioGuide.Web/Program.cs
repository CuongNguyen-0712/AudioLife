using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using VinhKhanhAudioGuide.Web.Configuration;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Endpoints;
using VinhKhanhAudioGuide.Web.Services;
using VinhKhanhAudioGuide.Web.Middleware;
using FluentValidation;
using VinhKhanhAudioGuide.Web.Validators;
using VinhKhanhAudioGuide.Web.Services.Simulation;
using VinhKhanhAudioGuide.Web.Hubs;


var builder = WebApplication.CreateBuilder(args);

var forceLanHttp = builder.Configuration.GetValue<bool>("LanHosting:ForceHttp")
                   || string.Equals(
                       Environment.GetEnvironmentVariable("LAN_HTTP_ONLY"),
                       "true",
                       StringComparison.OrdinalIgnoreCase);

var authSection = builder.Configuration.GetSection("Auth");
builder.Services.Configure<AuthOptions>(authSection);
var authOptions = authSection.Get<AuthOptions>();

builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthUserStore, AuthUserStore>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAudioStorageService, CloudinaryAudioStorageService>();
builder.Services.AddScoped<ITextToSpeechService, EdgeTextToSpeechService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddValidatorsFromAssemblyContaining<MobileHeartbeatRequestValidator>();
builder.Services.AddMemoryCache();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer("MobileJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptions?.JwtIssuer,
            ValidAudience = authOptions?.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions?.JwtSecret ?? "a-very-long-secret-key-that-should-be-in-settings"))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminOnly", policy => 
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(RoleNames.SystemAdmin);
    });
    options.AddPolicy("PoiAdminOnly", policy => 
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(RoleNames.PoiAdmin);
    });
    options.AddPolicy("MobileApi", policy =>
    {
        policy.AuthenticationSchemes.Add("MobileJwt");
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed-mobile", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("fixed-tts", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("fixed-login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToFolder("/Account");

    options.Conventions.AuthorizeFolder("/Admin", "SystemAdminOnly");
    options.Conventions.AuthorizeFolder("/Categories", "SystemAdminOnly");
    options.Conventions.AuthorizeFolder("/Locations", "SystemAdminOnly");
    options.Conventions.AuthorizeFolder("/Tours", "SystemAdminOnly");
    options.Conventions.AuthorizeFolder("/AudioGuides", "SystemAdminOnly");

    options.Conventions.AuthorizeFolder("/Shop", "PoiAdminOnly");
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPoiChangeRequestService, DbPoiChangeRequestService>();
builder.Services.AddScoped<IPoiAdminAssignmentService, PoiAdminAssignmentService>();
builder.Services.AddScoped<IPaymentPackageService, PaymentPackageService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddHostedService<SessionCleanupBackgroundService>();

// ── POI Load Simulation ──────────────────────────────────────────────────────
// Singleton vì cần giữ trạng thái batch xuyên suốt vòng đời ứng dụng
builder.Services.AddSingleton<IPoiSimulationService, PoiSimulationService>();
builder.Services.AddSignalR();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                               | ForwardedHeaders.XForwardedProto
                               | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    DbInitializer.Seed(context, passwordHasher);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    if (!forceLanHttp)
    {
        app.UseHsts();
    }
}

// Global JSON error handling for API
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseGlobalExceptionHandler();
});

app.UseForwardedHeaders();

if (!forceLanHttp && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapMobileApi();
app.MapHub<SimulationHub>("/hubs/simulation");

app.Run();
