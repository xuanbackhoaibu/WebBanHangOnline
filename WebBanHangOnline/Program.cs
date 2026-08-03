using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using System.Globalization;
using WebBanHangOnline.Controllers;
using WebBanHangOnline.Data;
using WebBanHangOnline.Hubs;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.Momo;
using WebBanHangOnline.Services;
using WebBanHangOnline.Services.Momo;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// 1️⃣ DATABASE & IDENTITY
// ===============================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(connectionString));

builder.Services.AddSignalR();
builder.Services.AddScoped<ChatBotController>();
builder.Services.AddHttpClient();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddTransient<IEmailSender, LocalEmailSender>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();
// Connect momo
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddHttpClient<IMomoService, MomoService>();
// ===============================
// 2️⃣ MVC + Razor Pages
// ===============================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebBanHangOnline API",
        Version = "v1",
        Description = "Catalog, admin analytics, and demo payment webhook APIs for the online fashion e-commerce project."
    });

    options.AddSecurityDefinition("IdentityCookie", new OpenApiSecurityScheme
    {
        Name = ".AspNetCore.Identity.Application",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Description = "Admin endpoints use ASP.NET Core Identity cookie authentication."
    });
});

// ===============================
// 3️⃣ SESSION (GIỎ HÀNG / TOAST)
// ===============================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===============================
// 4️⃣ LOCALIZATION
// ===============================
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// ===============================
// 5️⃣ REQUEST PIPELINE
// ===============================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebBanHangOnline API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "WebBanHangOnline API Docs";
    });
}

// ===============================
// 6️⃣ LOCALIZATION PIPELINE
// ===============================
var supportedCultures = new[]
{
    new CultureInfo("vi"),
    new CultureInfo("en")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider
        {
            QueryStringKey = "lang",
            UIQueryStringKey = "lang"
        },
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ]
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ===============================
// 7️⃣ ROUTES
// ===============================

// 🔥 SEO URL sản phẩm
// /san-pham/ao-thun-nam-15
// 🔥 AREAS - phải đặt trước DEFAULT
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// product seo
app.MapControllerRoute(
    name: "product-seo",
    pattern: "san-pham/{slug}-{id:int}",
    defaults: new { controller = "Product", action = "Details" }
);

// default
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ===============================
// 8️⃣ SEED DATABASE
// ===============================
await SeedDatabaseWithRetryAsync(app);
app.MapHub<ChatHub>("/chatHub");
app.Run();

static async Task SeedDatabaseWithRetryAsync(WebApplication app)
{
    const int maxAttempts = 12;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await DbInitializer.SeedAsync(scope.ServiceProvider);
            return;
        }
        catch (SqlException) when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
