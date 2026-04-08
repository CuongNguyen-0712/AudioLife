using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Configuration;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Endpoints;
using VinhKhanhAudioGuide.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthUserStore, AuthUserStore>();
builder.Services.AddScoped<IAudioStorageService, CloudinaryAudioStorageService>();
builder.Services.AddScoped<ITextToSpeechService, EdgeTextToSpeechService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminOnly", policy => policy.RequireRole(RoleNames.SystemAdmin));
    options.AddPolicy("PoiAdminOnly", policy => policy.RequireRole(RoleNames.PoiAdmin));
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

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Seed(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapMobileApi();

app.Run();
