using System.IdentityModel.Tokens.Jwt;
using Finbridge.Api.Auth;
using Finbridge.Application.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Finbridge.Tests.Api;

public class AuthControllerTests
{
    private const string ValidSigningKey = "test-signing-key-must-be-at-least-32-characters-long";
    private const string Issuer = "finbridge-test";
    private const string Audience = "finbridge-test";

    [Fact]
    public void IssueToken_WithValidUsername_Returns200AndJwt()
    {
        var controller = BuildController();

        var result = controller.IssueToken(new TokenRequest("alice"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<TokenResponse>(ok.Value);
        Assert.False(string.IsNullOrEmpty(payload.Token));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.Token);
        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == Audience);
        Assert.Equal("alice", jwt.Subject);
    }

    [Fact]
    public void IssueToken_WithEmptyUsername_Returns400()
    {
        var controller = BuildController();

        var result = controller.IssueToken(new TokenRequest(""));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void IssueToken_WithShortSigningKey_Returns500()
    {
        var controller = BuildController(signingKey: "short");

        var result = controller.IssueToken(new TokenRequest("alice"));

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
    }

    [Fact]
    public void IssueToken_HonorsExpirationHours()
    {
        var controller = BuildController(expirationHours: 2);

        var result = controller.IssueToken(new TokenRequest("alice"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<TokenResponse>(ok.Value);
        var diff = payload.ExpiresAt - DateTime.UtcNow;
        Assert.InRange(diff.TotalHours, 1.9, 2.5);
    }

    private static AuthController BuildController(string signingKey = ValidSigningKey, int expirationHours = 24)
    {
        var settings = Options.Create(new JwtSettings
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = signingKey,
            ExpirationHours = expirationHours
        });
        return new AuthController(settings);
    }
}
