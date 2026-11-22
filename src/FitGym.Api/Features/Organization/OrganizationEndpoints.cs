using FitGym.Application.Features.Organization;

namespace FitGym.Api.Features.Organization;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/organization", async (IOrganizationService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(await service.GetPublicAsync(cancellationToken));
        }).AllowAnonymous();
        endpoints.MapGet("/api/organization/logo", async (IOrganizationService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            var logo = await service.GetLogoAsync(cancellationToken);
            return logo is null ? Results.NotFound() : Results.File(logo.Content, logo.ContentType);
        }).AllowAnonymous();
        return endpoints;
    }
}
