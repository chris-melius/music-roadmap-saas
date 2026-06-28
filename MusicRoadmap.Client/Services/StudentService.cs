using System.Net.Http.Json;
using MusicRoadmap.Domain.Entities;
using System.Text.Json;
using MusicRoadmap.Shared.DTOs;

namespace MusicRoadmap.Client.Services;

public class StudentService(HttpClient http)
{
    // Use the factory to create the "Api" client with the JWT handler attached


 //   public async Task<List<Student>> GetStudentsAsync()
   // {
        // The ?? ensures we never return a null list to the UI
     //   return await _http.GetFromJsonAsync<List<Student>>("/api/students") 
       //        ?? new List<Student>();
    //}

public async Task<List<Student>> GetStudentsAsync()
    {
        // 1. Fire the raw network request down the pipe
        var response = await http.GetAsync("api/students");

        // 2. SUCCESS PASS: If the server says 200 OK, return the clean collection
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Student>>() ?? new();
        }

        // 3. ERROR PASS: The server crashed with our custom 500 Global Exception packet!
        try
        {
            // Read and parse the backend's custom JSON error envelope
            var errorDetails = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
            if (errorDetails != null)
            {
                // FORCE THE EXCEPTION: This guarantees the catch block inside Students.razor wakes up!
                throw new HttpRequestException(errorDetails.Message);
            }
        }
        catch (HttpRequestException)
        {
            // Allow our forced exception to bubble straight out to Students.razor untouched!
            throw;
        }
        catch (Exception ex)
        {
            
        }

        // Generic fallback throw if the error payload was malformed
        throw new HttpRequestException($"API connection failed with status code: {(int)response.StatusCode}");
    }

    public async Task<bool> AddStudentAsync(StudentDto dto)
    {
        var response = await http.PostAsJsonAsync("api/students", dto);
        return response.IsSuccessStatusCode;
    }

public async Task<byte[]> GetRoadmapPdfAsync(string studentId)
{
   try
    {
        // 1. Get the raw response (Don't use EnsureSuccessStatusCode yet)
        var response = await http.GetAsync($"api/roadmaps/generate/{studentId}");
        
        // 2. Read the body as a string to see the error message
        var errorContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        else
        {
            // This is the 401/400/500 diagnostic
            Console.Error.WriteLine($"[DEBUG] API FAILED.");
            Console.Error.WriteLine($"[DEBUG] Reason Phrase: {response.ReasonPhrase}");
            Console.Error.WriteLine($"[DEBUG] Response Content: {errorContent}");
            
            return Array.Empty<byte>();
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[DEBUG] Critical C# Exception: {ex.GetType().Name}");
        Console.Error.WriteLine($"[DEBUG] Message: {ex.Message}");
        return Array.Empty<byte>();
    }
}
}