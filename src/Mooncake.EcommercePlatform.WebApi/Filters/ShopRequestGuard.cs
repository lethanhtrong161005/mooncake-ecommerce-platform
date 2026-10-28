namespace Mooncake.EcommercePlatform.WebApi.Filters;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

/// <summary>Bounds JSON before model binding and rejects ambiguous or overposted request shapes.</summary>
public sealed class ShopRequestGuard : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ShopApiAttribute>() == null) { await next(); return; }
        var request = context.HttpContext.Request;
        var descriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var allowed = descriptor.Parameters.Where(p => p.BindingInfo?.BindingSource?.Id == "Query")
            .SelectMany(p => p.ParameterType.GetProperties().Select(x => x.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (request.Query.Any(p => !allowed.Contains(p.Key) || p.Value.Count != 1)) { Reject(context); return; }
        if (!HttpMethods.IsGet(request.Method)) {
            ShopPolicyHelper.Key(request.Headers["Idempotency-Key"].ToString());
            if (request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true) {
                if (request.ContentLength > 32768) { Reject(context, 413); return; }
                request.EnableBuffering();
                var bytes = new byte[32769]; var length = 0;
                while (length < bytes.Length) {
                    var count = await request.Body.ReadAsync(bytes.AsMemory(length), context.HttpContext.RequestAborted);
                    if (count == 0) break;
                    length += count;
                }
                request.Body.Position = 0;
                if (length > 32768) { Reject(context, 413); return; }
                try {
                    using var json = JsonDocument.Parse(bytes.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 8 });
                    if (!ShopJsonHelper.Unambiguous(json.RootElement)) { Reject(context); return; }
                }
                catch (JsonException) { Reject(context); return; }
            }
        }
        await next();
    }
    private static void Reject(ResourceExecutingContext context, int status = 400)
    {
        context.Result = new ObjectResult(new ApiResponse<object> { Status = status, Message = C.Error, ErrorCode = C.Invalid,
            Path = context.HttpContext.Request.Path, TraceId = context.HttpContext.TraceIdentifier }) { StatusCode = status };
    }
}

internal static class ShopJsonHelper
{
    public static bool Unambiguous(JsonElement node, bool rejectNulls = false)
    {
        if (node.ValueKind == JsonValueKind.Object) {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in node.EnumerateObject()) {
                if (!names.Add(property.Name) || rejectNulls && property.Value.ValueKind == JsonValueKind.Null) return false;
                if (!Unambiguous(property.Value, property.Name.Equals("overrides", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("defaults", StringComparison.OrdinalIgnoreCase) || property.Name.Equals("layout", StringComparison.OrdinalIgnoreCase))) return false;
            }
        } else if (node.ValueKind == JsonValueKind.Array) foreach (var item in node.EnumerateArray()) if (!Unambiguous(item)) return false;
        return true;
    }
}
