namespace Biograf.Shared.Auth;

/// <summary>
/// Rollerna i systemet. Ligger i Shared så att både API:t och klienten
/// använder exakt samma namn.
/// </summary>
public static class Roller
{
    public const string Admin = "Admin";
    public const string Personal = "Personal";
    public const string Kund = "Kund";

    /// <summary>För [Authorize(Roles = ...)] där både personal och admin ska ha åtkomst.</summary>
    public const string PersonalEllerAdmin = Personal + "," + Admin;

    public static readonly IReadOnlyList<string> Alla = [Admin, Personal, Kund];
}
