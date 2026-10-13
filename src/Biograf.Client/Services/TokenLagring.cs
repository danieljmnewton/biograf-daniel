using Microsoft.JSInterop;

namespace Biograf.Client.Services;

/// <summary>Sparar inloggningstoken i webbläsarens localStorage så att man förblir inloggad vid omladdning.</summary>
public class TokenLagring(IJSRuntime js)
{
    private const string Nyckel = "biograf.token";

    public async Task<string?> HamtaAsync()
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", Nyckel);
        }
        catch (JSException)
        {
            // T.ex. privat läge där localStorage är avstängt.
            return null;
        }
    }

    public ValueTask SparaAsync(string token) => js.InvokeVoidAsync("localStorage.setItem", Nyckel, token);

    public ValueTask TaBortAsync() => js.InvokeVoidAsync("localStorage.removeItem", Nyckel);
}
