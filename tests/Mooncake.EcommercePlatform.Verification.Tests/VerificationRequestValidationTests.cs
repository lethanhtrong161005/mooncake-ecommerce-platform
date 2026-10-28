namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.WebApi.Controllers;

/// <summary>Exercises MVC record metadata, which service-level validation does not cover.</summary>
public sealed class VerificationRequestValidationTests
{
    [Theory]
    [InlineData(2L, "Vi phạm bản quyền", true)]
    [InlineData(null, "Verification evidence is invalid.", false)]
    [InlineData(-1L, "Verification evidence is invalid.", false)]
    [InlineData(2L, "short", false)]
    [InlineData(2L, null, false)]
    public void RevokeUsesConstructorValidationWithoutThrowing(long? version, string? reason, bool valid)
    {
        var json = JsonSerializer.Serialize(new { expectedVersion = version, reason });
        var model = JsonSerializer.Deserialize<RevokeSupplierRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(valid, Validate(model).IsValid);
    }

    [Theory]
    [InlineData("read-through-token", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ReadAllUsesConstructorValidationWithoutThrowing(string? token, bool valid)
    {
        Assert.Equal(valid, Validate(new ReadVerificationNotificationsRequest(token!)).IsValid);
    }

    private static ModelStateDictionary Validate(object model)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        using var provider = services.BuildServiceProvider();
        var context = new ActionContext(new DefaultHttpContext { RequestServices = provider },
            new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        provider.GetRequiredService<IObjectModelValidator>().Validate(context, null, string.Empty, model);
        return context.ModelState;
    }
}
