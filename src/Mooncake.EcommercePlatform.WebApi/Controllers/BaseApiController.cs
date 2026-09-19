namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Utils;

/// <summary>
/// Base controller providing convenience methods that wrap <see cref="ResponseHelper"/>
/// with the current request path and trace identifier.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>Returns a 200 response with a typed payload.</summary>
    protected IActionResult Success<T>(T data, string message) =>
        Ok(ResponseHelper.SuccessWithData(
            data,
            message,
            Request.Path.Value ?? string.Empty,
            HttpContext.Items["TraceId"]?.ToString() ?? string.Empty));

    /// <summary>Returns a 200 response with a message only.</summary>
    protected IActionResult Success(string message) =>
        Ok(ResponseHelper.Success(
            message,
            Request.Path.Value ?? string.Empty,
            HttpContext.Items["TraceId"]?.ToString() ?? string.Empty));

    /// <summary>Returns a 201 Created response with a typed payload.</summary>
    protected IActionResult Created<T>(T data, string message) =>
        StatusCode(StatusCodes.Status201Created, ResponseHelper.SuccessCreated(
            data,
            message,
            Request.Path.Value ?? string.Empty,
            HttpContext.Items["TraceId"]?.ToString() ?? string.Empty));
}
