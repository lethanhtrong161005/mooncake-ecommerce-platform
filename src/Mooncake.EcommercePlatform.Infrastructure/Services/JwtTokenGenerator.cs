namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>JWT token generator implementing <see cref="IJwtTokenGenerator"/>.</summary>
public class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    private const string DefaultSecret = "MooncakeEcommercePlatformSuperSecretSecurityKey2026!";

    public string GenerateToken(User user, long? customerId = null, long? supplierId = null)
    {
        var secret = configuration["Jwt:SecretKey"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? DefaultSecret;
        var issuer = configuration["Jwt:Issuer"] ?? "MooncakePlatform";
        var audience = configuration["Jwt:Audience"] ?? "MooncakePlatformAudience";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("userId", user.Id.ToString())
        };

        if (customerId.HasValue)
        {
            claims.Add(new Claim("customerId", customerId.Value.ToString()));
        }

        if (supplierId.HasValue)
        {
            claims.Add(new Claim("supplierId", supplierId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
