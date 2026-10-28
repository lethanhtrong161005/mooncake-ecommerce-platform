namespace Mooncake.EcommercePlatform.WebApi.Middleware;

using System.Security.Claims;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Replaces stale JWT role claims with current database authority before authorization.</summary>
public sealed class CurrentAccountMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISupplierRepository repository)
    {
        if (context.User.Identity is ClaimsIdentity { IsAuthenticated: true } identity &&
            Guid.TryParse(identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id))
        {
            var account = await repository.GetUserAsync(id, context.RequestAborted);
            if (account is not { IsActive: true, IsDeleted: false })
            {
                context.Response.StatusCode = 403;
                var error = ResponseHelper.Error<object>(403, C.Error, context.Request.Path,
                    context.Items["TraceId"]?.ToString() ?? context.TraceIdentifier);
                error.ErrorCode = C.Forbidden;
                await context.Response.WriteAsJsonAsync(error, context.RequestAborted);
                return;
            }
            foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray()) identity.RemoveClaim(claim);
            identity.AddClaim(new Claim(identity.RoleClaimType, account.Role.ToString()));
            foreach (var claim in identity.FindAll("supplier_fulfillment").ToArray()) identity.RemoveClaim(claim);
            if (account.RequestedRole == Mooncake.EcommercePlatform.Domain.Enums.UserRole.Supplier)
            {
                var profile = await repository.GetProfileByUserIdAsync(id, false, context.RequestAborted);
                if (profile?.RevokedAtUtc != null)
                    identity.AddClaim(new Claim("supplier_fulfillment", "true"));
            }
        }
        await next(context);
    }
}
