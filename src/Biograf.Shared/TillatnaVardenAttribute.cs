using System.ComponentModel.DataAnnotations;

namespace Biograf.Shared;

/// <summary>Validerar att ett heltal är ett av de uppräknade värdena.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class TillatnaVardenAttribute(params int[] varden) : ValidationAttribute
{
    public IReadOnlyList<int> Varden { get; } = varden;

    public override bool IsValid(object? value) => value is int tal && Varden.Contains(tal);
}
