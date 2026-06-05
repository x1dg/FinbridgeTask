namespace Finbridge.Api.Auth;

public sealed record TokenRequest(string Username);

public sealed record TokenResponse(string Token, DateTime ExpiresAt);
