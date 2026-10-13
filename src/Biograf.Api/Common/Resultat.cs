namespace Biograf.Api.Common;

public enum FelTyp
{
    HittadesInte,
    Ogiltig,
    Konflikt
}

/// <summary>
/// Generiskt resultat från en tjänst: antingen ett värde av typen T eller ett fel.
/// Tjänsterna behöver då inte kasta undantag för förväntade fel (t.ex. "filmen finns inte"),
/// och controllern kan översätta felet till rätt HTTP-statuskod.
/// </summary>
public class Resultat<T>
{
    public bool Lyckades { get; }
    public T? Varde { get; }
    public FelTyp? Fel { get; }
    public string? Meddelande { get; }

    private Resultat(T varde)
    {
        Lyckades = true;
        Varde = varde;
    }

    private Resultat(FelTyp fel, string meddelande)
    {
        Lyckades = false;
        Fel = fel;
        Meddelande = meddelande;
    }

    public static Resultat<T> Ok(T varde) => new(varde);
    public static Resultat<T> HittadesInte(string meddelande) => new(FelTyp.HittadesInte, meddelande);
    public static Resultat<T> Ogiltig(string meddelande) => new(FelTyp.Ogiltig, meddelande);
    public static Resultat<T> Konflikt(string meddelande) => new(FelTyp.Konflikt, meddelande);

    /// <summary>Gör om ett lyckat värde till en annan typ, men behåller ett eventuellt fel.</summary>
    public Resultat<TNy> Map<TNy>(Func<T, TNy> omvandla) =>
        Lyckades ? Resultat<TNy>.Ok(omvandla(Varde!)) : Resultat<TNy>.MedFel(Fel!.Value, Meddelande!);

    internal static Resultat<T> MedFel(FelTyp fel, string meddelande) => new(fel, meddelande);
}

/// <summary>Används när en operation lyckas utan att returnera något värde, t.ex. borttagning.</summary>
public readonly record struct Inget;
