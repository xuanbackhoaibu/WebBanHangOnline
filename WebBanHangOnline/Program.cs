using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebBanHangOnline.Controllers;
using WebBanHangOnline.Data;
using WebBanHangOnline.Hubs;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.Momo;
using WebBanHangOnline.Services.Momo;

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

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();
// Connect momo
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();
// ===============================
// 2️⃣ MVC + Razor Pages
// ===============================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

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
    SupportedUICultures = supportedCultures
});

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
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.SeedAsync(services);
}
app.MapHub<ChatHub>("/chatHub");
app.Run();
