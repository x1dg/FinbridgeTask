using System.Net.Http.Json;
using Finbridge.Web.Services;
using Microsoft.Extensions.Options;

namespace Finbridge.Web.Auth;

public sealed class AuthBootstrapService : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiSettings _settings;
    private readonly TokenStore _tokenStore;
    private readonly ILogger<AuthBootstrapService> _logger;

    public AuthBootstrapService(
        IHttpClientFactory httpClientFactory,
        IOptions<ApiSettings> settings,
        TokenStore tokenStore,
        ILogger<AuthBootstrapService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("auth-bootstrap");
            var response = await client.PostAsJsonAsync(
                $"{_settings.BaseUrl}/api/auth/token",
                new { username = "web" },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
            if (result is null || string.IsNullOrEmpty(result.Token))
            {
                _logger.LogWarning("Сервис авторизации вернул пустой токен.");
                return;
            }

            _tokenStore.Token = result.Token;
            _logger.LogInformation("JWT-токен получен, длина: {Length} символов.", result.Token.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось получить JWT-токен от API. Последующие запросы вернут 401.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private sealed record TokenResponse(string Token, DateTime ExpiresAt);
}
