using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Biograf.Client.Services;

/// <summary>Svaret från ett API-anrop: antingen data av typen T eller ett felmeddelande att visa.</summary>
public record ApiSvar<T>(bool Lyckades, T? Data, string? Fel, HttpStatusCode Status)
{
    public static ApiSvar<T> Ok(T? data, HttpStatusCode status) => new(true, data, null, status);
    public static ApiSvar<T> Misslyckat(string fel, HttpStatusCode status) => new(false, default, fel, status);
}

/// <summary>
/// Generisk klient mot API:t. Lägger på inloggningstoken i varje anrop och
/// översätter API:ts felsvar (ProblemDetails) till ett läsbart meddelande.
/// </summary>
public class ApiKlient(HttpClient http, TokenLagring tokenLagring)
{
    public Task<ApiSvar<T>> GetAsync<T>(string url) => SkickaAsync<T>(HttpMethod.Get, url);

    public Task<ApiSvar<T>> PostAsync<T>(string url, object? innehall = null) => SkickaAsync<T>(HttpMethod.Post, url, innehall);

    public Task<ApiSvar<T>> PutAsync<T>(string url, object innehall) => SkickaAsync<T>(HttpMethod.Put, url, innehall);

    public Task<ApiSvar<object>> DeleteAsync(string url) => SkickaAsync<object>(HttpMethod.Delete, url);

    private async Task<ApiSvar<T>> SkickaAsync<T>(HttpMethod metod, string url, object? innehall = null)
    {
        using var forfragan = new HttpRequestMessage(metod, url);
        if (innehall is not null)
            forfragan.Content = JsonContent.Create(innehall);

        var token = await tokenLagring.HamtaAsync();
        if (!string.IsNullOrEmpty(token))
            forfragan.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage svar;
        try
        {
            svar = await http.SendAsync(forfragan);
        }
        catch (HttpRequestException)
        {
            return ApiSvar<T>.Misslyckat("Kunde inte nå servern. Kontrollera att API:t är igång.", HttpStatusCode.ServiceUnavailable);
        }

        if (svar.IsSuccessStatusCode)
        {
            var data = svar.StatusCode == HttpStatusCode.NoContent || svar.Content.Headers.ContentLength == 0
                ? default
                : await svar.Content.ReadFromJsonAsync<T>();
            return ApiSvar<T>.Ok(data, svar.StatusCode);
        }

        return ApiSvar<T>.Misslyckat(await LasFelmeddelandeAsync(svar), svar.StatusCode);
    }

    private static async Task<string> LasFelmeddelandeAsync(HttpResponseMessage svar)
    {
        switch (svar.StatusCode)
        {
            case HttpStatusCode.Unauthorized when svar.Content.Headers.ContentLength is 0 or null:
                return "Du behöver logga in.";
            case HttpStatusCode.Forbidden:
                return "Du har inte behörighet att göra det här.";
        }

        try
        {
            using var json = JsonDocument.Parse(await svar.Content.ReadAsStringAsync());
            var rot = json.RootElement;

            // Valideringsfel: { errors: { Falt: ["meddelande"] } }
            if (rot.TryGetProperty("errors", out var fel) && fel.ValueKind == JsonValueKind.Object)
            {
                var meddelanden = fel.EnumerateObject()
                    .SelectMany(f => f.Value.EnumerateArray().Select(m => m.GetString()))
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                return string.Join(" ", meddelanden);
            }

            if (rot.TryGetProperty("title", out var titel))
                return titel.GetString() ?? "Något gick fel.";
        }
        catch (JsonException)
        {
        }

        return $"Något gick fel ({(int)svar.StatusCode}).";
    }
}
