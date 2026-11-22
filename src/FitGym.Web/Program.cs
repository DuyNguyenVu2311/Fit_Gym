using FitGym.Infrastructure;
using FitGym.Infrastructure.Identity;
using FitGym.Infrastructure.Persistence;
using FitGym.Web.Features.Accounts;
using FitGym.Web.Features.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var installation = CustomerInstallation.Resolve(builder.Configuration["FitGymDeployment:InstallationId"], builder.Environment.IsDevelopment());
builder.Services.AddDataProtection().SetApplicationName(installation.ApplicationName);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddFitGymInfrastructure(
    builder.Configuration.GetConnectionString("FitGym")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:FitGym."));
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = installation.CookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnValidatePrincipal = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
        if (!await AccountSession.IsCurrentAsync(context.Principal, userManager))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<AccountAdministrationService>();
builder.Services.AddScoped<CurrentEmployeeService>();
builder.Services.AddScoped<FitGym.Web.Features.Members.MemberAccountService>();

var app = builder.Build();

await FitGymIdentityInitializer.InitializeAsync(
    app.Services,
    app.Environment.IsDevelopment(),
    app.Logger);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
