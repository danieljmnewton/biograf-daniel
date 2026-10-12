# CLAUDE.md

Anteckningar för Claude Code, som används som AI-stöd i projektet.

- .NET 8-solution: `src/Biograf.Api`, `src/Biograf.Client` (Blazor WASM), `src/Biograf.Shared`, `tests/Biograf.Tests`.
- Bygg och testa med `dotnet build` och `dotnet test` innan en PR öppnas.
- En branch per issue (`feature/<nr>-<namn>`), PR mot `main` med "Closes #<nr>".
- Kod, kommentarer och commit-meddelanden skrivs på svenska. Klassnamn och egenskaper är också på svenska (`Film`, `Visning`, `Bokning`).
