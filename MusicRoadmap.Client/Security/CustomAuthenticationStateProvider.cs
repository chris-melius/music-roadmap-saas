using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace MusicRoadmap.Client.Security;

public class CustomAuthenticationStateProvider(ILocalStorageService localStorage) : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await localStorage.GetItemAsync<string>("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(_anonymous);
            }

            return new AuthenticationState(BuildClaimsPrincipal(token));
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task NotifyUserLoginAsync(string accessToken, string refreshToken)
    {
        await localStorage.SetItemAsync("accessToken", accessToken);
        await localStorage.SetItemAsync("refreshToken", refreshToken);

        var authenticatedUser = BuildClaimsPrincipal(accessToken);
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        
        NotifyAuthenticationStateChanged(authState);
    }

    public async Task NotifyUserLogoutAsync()
    {
        await localStorage.RemoveItemAsync("accessToken");
        await localStorage.RemoveItemAsync("refreshToken");

        var authState = Task.FromResult(new AuthenticationState(_anonymous));
        
        NotifyAuthenticationStateChanged(authState);
    }

    private static ClaimsPrincipal BuildClaimsPrincipal(string jwt)
    {
        var identity = new ClaimsIdentity(ParseClaimsFromJwt(jwt), "jwt");
        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];

        // Adjust for standard Base64 string padding requirements
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var jsonBytes = Convert.FromBase64String(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                var valueStr = kvp.Value.ToString() ?? string.Empty;
                
                // Map common JWT structural claim names to official .NET claim strings
                if (kvp.Key == "role" || kvp.Key == ClaimTypes.Role)
                {
                    claims.Add(new Claim(ClaimTypes.Role, valueStr));
                }
                else if (kvp.Key == "unique_name" || kvp.Key == ClaimTypes.Name)
                {
                    claims.Add(new Claim(ClaimTypes.Name, valueStr));
                }
                else if (kvp.Key == "sub" || kvp.Key == ClaimTypes.NameIdentifier)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, valueStr));
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, valueStr));
                }
            }
        }

        return claims;
    }
}