# Newtons Biograf

Ett internt biograf- och bokningssystem för en fiktiv biograf med flera salonger.
Projektuppgift i kursen Ämnesövergripande projekt (SYSS8, HT26), Newton.

## Teknik

| Del | Teknik |
|---|---|
| Backend | ASP.NET Core 8 Web API |
| Databas | Entity Framework Core + SQLite |
| Frontend | Blazor WebAssembly |
| Tester | xUnit |

## Projektstruktur

```
src/
  Biograf.Api/      Web API, databas och affärslogik
  Biograf.Client/   Blazor WebAssembly-klienten
  Biograf.Shared/   DTO:er som delas mellan API och klient
tests/
  Biograf.Tests/    Enhets- och scenariotester
```

## Kom igång

Kräver .NET 8 SDK.

```bash
dotnet build
dotnet run --project src/Biograf.Api --launch-profile https      # https://localhost:7076/swagger
dotnet run --project src/Biograf.Client --launch-profile https   # https://localhost:7071
```

## Arbetssätt

Arbetet planeras i GitHub Projects med en backlog av user stories (issues) som delas upp i sprintar.
Varje issue utvecklas i en egen `feature/<nr>-<namn>`-branch och mergas till `main` via en pull request.
