using JobCareerPlatform.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(
    options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Views are grouped by role under Views/Admin/{Controller}/ and Views/Employer/{Controller}/
// (JobSeeker already has a single controller named "JobSeeker", so its existing
// Views/JobSeeker/ folder already satisfies the default lookup convention as-is).
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Add("/Views/Admin/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Views/Employer/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Views/CareerAdvisor/{1}/{0}" + RazorViewEngine.ViewExtension);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(
        scope.ServiceProvider,
        builder.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    // AWS Elastic Beanstalk's default single-instance environment only exposes a plain
    // HTTP endpoint via its *.elasticbeanstalk.com domain — there is no HTTPS listener
    // unless a custom domain with an ACM certificate is attached. Forcing a redirect to
    // HTTPS here would break the deployed site (redirecting to a port nothing listens on),
    // so HSTS/HTTPS redirection are left off for production. Uncomment both lines below
    // once a custom domain + ACM certificate + HTTPS listener has been configured.
    // app.UseHsts();
    // app.UseHttpsRedirection();
}

// Elastic Beanstalk's load balancer sits in front of the app, so trust its
// X-Forwarded-For / X-Forwarded-Proto headers to get the real client IP and scheme.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
