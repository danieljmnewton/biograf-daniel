using Microsoft.AspNetCore.Identity;

namespace Biograf.Api.Services;

/// <summary>Svenska felmeddelanden för de vanligaste Identity-felen vid registrering.</summary>
public class SvenskaIdentityFel : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"E-postadressen {email} används redan." };

    // Användarnamnet är samma som e-posten, så det felet täcks redan av DuplicateEmail.
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = string.Empty };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "Ogiltig e-postadress." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Lösenordet måste vara minst {length} tecken." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Lösenordet måste innehålla minst en siffra." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Lösenordet måste innehålla minst en liten bokstav." };
}
