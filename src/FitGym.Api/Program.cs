using FitGym.Api.Features.Member;
using FitGym.Api.Features.Organization;
using FitGym.Api.Features.Gate;
using FitGym.Infrastructure;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var installation = CustomerInstallation.Resolve(builder.Configuration["FitGymDeployment:InstallationId"], builder.Environment.IsDevelopment());
builder.Services.AddDataProtection().SetApplicationName(installation.ApplicationName);

builder.Services.AddOpenApi();
builder.Services.AddFitGymInfrastructure(
    builder.Configuration.GetConnectionString("FitGym")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:FitGym."));
builder.Services
    .AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme);
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/api/health"));

app.MapGet("/api/health", async (FitGymDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { service = "FitGym.Api", database = "connected" })
        : Results.Problem("The FitGym database is unavailable."));

app.MapMemberEndpoints();
app.MapOrganizationEndpoints();
app.MapGateEndpoints();

app.Run();
