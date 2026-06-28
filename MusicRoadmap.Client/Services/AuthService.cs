using System.Net.Http.Json;
using MusicRoadmap.Shared.DTOs;

namespace MusicRoadmap.Client.Services;

public class AuthService(HttpClient http)
{
    public async Task<HttpResponseMessage> Register(RegisterDto registerDto)
    {
        return await http.PostAsJsonAsync("api/auth/register", registerDto);
    }

    public async Task<HttpResponseMessage> Login(LoginDto loginDto)
    {
        return await http.PostAsJsonAsync("api/auth/login", loginDto);
    }
}