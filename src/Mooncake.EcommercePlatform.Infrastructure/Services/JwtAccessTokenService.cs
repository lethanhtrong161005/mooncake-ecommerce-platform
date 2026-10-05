namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Creates short-lived HMAC-signed JWT access tokens.</summary>
public sealed class JwtAccessTokenService : IAccessTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(60);
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtAccessTokenService()
    {
        _secret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? string.Empty;
        _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "Mooncake.EcommercePlatform";
        _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "Mooncake.Client";
    }

    public DateTime GetExpirationUtc() => DateTime.UtcNow.Add(TokenLifetime);

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_issuer, _audience, claims, expires: GetExpirationUtc(), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
