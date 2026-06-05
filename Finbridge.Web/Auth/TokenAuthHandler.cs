using System.Net.Http.Headers;

namespace Finbridge.Web.Auth;

public sealed class TokenAuthHandler : DelegatingHandler
{
    private readonly TokenStore _store;

    public TokenAuthHandler(TokenStore store)
    {
        _store = store;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_store.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _store.Token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
