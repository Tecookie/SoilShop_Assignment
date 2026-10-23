using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Api;

public sealed class JwtTokenIssuer(IConfiguration config) : ITokenIssuer
{
    public LoginResult Issue(User user)
    {
        var expires = DateTime.UtcNow.AddHours(2);
        var token = new JwtSecurityToken("soilshop", "soilshop-web",
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role), new Claim("version", user.Version.ToString())],
            expires: expires, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, new(user.Id, user.Username, user.DisplayName, user.Role));
    }
}
