using FitGym.Application.Features.CheckIns;
using FitGym.Application.Features.Equipment;
using FitGym.Application.Features.Members;
using FitGym.Application.Features.Packages;
using FitGym.Application.Features.Registrations;
using FitGym.Application.Features.Training;
using FitGym.Application.Features.Warehouse;
using FitGym.Application.Features.Organization;
using FitGym.Infrastructure.Features.Organization;
using FitGym.Infrastructure.Features.CheckIns;
using FitGym.Infrastructure.Features.Equipment;
using FitGym.Infrastructure.Features.Members;
using FitGym.Infrastructure.Features.Packages;
using FitGym.Infrastructure.Features.Registrations;
using FitGym.Infrastructure.Features.Training;
using FitGym.Infrastructure.Features.Warehouse;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FitGym.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFitGymInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FitGymDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<int>>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<FitGymDbContext>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ICheckInService, CheckInService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<ITrainingFoundationService, TrainingFoundationService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<FitGym.Application.Features.Payments.IMembershipPaymentQuery, FitGym.Infrastructure.Features.Payments.MembershipPaymentQuery>();
        services.AddScoped<FitGym.Application.Features.Payments.IMembershipPaymentConfirmation, FitGym.Infrastructure.Features.Payments.MembershipPaymentConfirmation>();
        return services;
    }
}
