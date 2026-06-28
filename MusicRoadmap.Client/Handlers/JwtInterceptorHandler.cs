using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MusicRoadmap.Shared.DTOs;
using Blazored.LocalStorage;

namespace MusicRoadmap.Client.Handlers;

public class JwtInterceptorHandler(ILocalStorageService localStorage, IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await localStorage.GetItemAsync<string>("accessToken");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refreshSuccessful = await AttemptTokenRefreshAsync();
            if (refreshSuccessful)
            {
                var newToken = await localStorage.GetItemAsync<string>("accessToken");
                var clonedRequest = await CloneRequestAsync(request);
                clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                
                return await base.SendAsync(clonedRequest, cancellationToken);
            }
        }

        return response;
    }

    private async Task<bool> AttemptTokenRefreshAsync()
    {
        try
        {
            var accessToken = await localStorage.GetItemAsync<string>("accessToken");
            var refreshToken = await localStorage.GetItemAsync<string>("refreshToken");

            if (string.IsNullOrEmpty(refreshToken)) return false;

            var refreshDto = new RefreshTokenRequestDto 
            { 
                AccessToken = accessToken ?? string.Empty, 
                RefreshToken = refreshToken 
            };

            // THE LOCK-BREAKER: Create a dedicated "AnonymousClient" from the factory.
            // This client is entirely isolated from the interceptor, stopping the recursive loop dead.
            var anonymousClient = httpClientFactory.CreateClient("AnonymousClient");
            var refreshResponse = await anonymousClient.PostAsJsonAsync("api/auth/refresh", refreshDto);

            if (refreshResponse.IsSuccessStatusCode)
            {
                var newTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (newTokens != null)
                {
                    await localStorage.SetItemAsync("accessToken", newTokens.AccessToken);
                    await localStorage.SetItemAsync("refreshToken", newTokens.RefreshToken);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SECURITY ERROR] Silent token exchange failed: {ex.Message}");
        }

        return false;
    }

    private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage req)
    {
        var targetUri = req.RequestUri;
        if (targetUri != null && !targetUri.IsAbsoluteUri)
        {
            // Safely resolve the base URL using our factory-isolated client instance
            var anonymousClient = httpClientFactory.CreateClient("AnonymousClient");
            var baseAddress = anonymousClient.BaseAddress ?? new Uri("http://localhost:5213/");
            targetUri = new Uri(baseAddress, targetUri);
        }

        var clone = new HttpRequestMessage(req.Method, targetUri) { Version = req.Version };
        
        foreach (var option in req.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }
        
        foreach (var header in req.Headers) 
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);
            foreach (var header in req.Content.Headers) 
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        return clone;
    }
}