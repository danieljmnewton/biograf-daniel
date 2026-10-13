using System.ComponentModel.DataAnnotations;

namespace Biograf.Shared.Auth;

public class RegistreraDto
{
    [Required(ErrorMessage = "Förnamn krävs")]
    [StringLength(50)]
    public string Fornamn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Efternamn krävs")]
    [StringLength(50)]
    public string Efternamn { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-post krävs")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lösenord krävs")]
    [MinLength(6, ErrorMessage = "Lösenordet måste vara minst 6 tecken")]
    public string Losenord { get; set; } = string.Empty;
}

public class LoggaInDto
{
    [Required(ErrorMessage = "E-post krävs")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lösenord krävs")]
    public string Losenord { get; set; } = string.Empty;
}

public record AnvandarDto(
    string Id,
    string Email,
    string Fornamn,
    string Efternamn,
    IReadOnlyList<string> Roller);

/// <summary>Svaret efter lyckad inloggning. Klienten sparar token och skickar den i varje anrop.</summary>
public record InloggningSvar(
    string Token,
    DateTime GiltigTill,
    string Email,
    string Namn,
    IReadOnlyList<string> Roller);
