using System.ComponentModel.DataAnnotations;

namespace Biograf.Shared.Auth;

/// <summary>Används när admin skapar ett konto åt personalen (eller en annan admin).</summary>
public class SkapaAnvandareDto : RegistreraDto
{
    [Required]
    public string Roll { get; set; } = Roller.Personal;
}

public class BytRollDto
{
    [Required(ErrorMessage = "Välj en roll")]
    public string Roll { get; set; } = string.Empty;
}
