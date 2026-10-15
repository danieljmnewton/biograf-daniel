# Testscenarier

Testerna ligger i `tests/Biograf.Tests` och körs med:

```bash
dotnet test
```

Varje test har ett id (B1, V1 …) i sitt `DisplayName` så att det går att koppla till scenariot nedan.

**Testmiljö**

- **Tjänstetester** (B, V, S, F) körs mot en **SQLite-databas i minnet** (`Hjalpare/TestDatabas.cs`). Till skillnad från EF:s InMemory-provider gäller där unika index, främmande nycklar och transaktioner precis som i den riktiga databasen. Varje testklass får en ny, tom databas med:
  - en film på 100 minuter
  - Salong A, 3 × 4 platser där rad 3 är VIP
  - Salong B, 2 × 2 platser
  - en visning imorgon kl. 18:00 i Salong A, grundpris 120 kr
  - två kunder
- **Behörighetstester** (R) startar hela API:t i minnet med `WebApplicationFactory`. Det får en egen temporär databasfil med de seedade testkontona och testas med riktiga HTTP-anrop och JWT.

---

## Bokning (`BokningTester.cs`)

| Id | Givet | När | Så |
|---|---|---|---|
| B1 | En visning med grundpris 120 kr | Kunden bokar en standardplats och en VIP-plats | Bokningen lyckas, totalpriset är 120 + 170 = 290 kr och bokningsnumret har formatet `NB-XXXXX` |
| B2 | Rad 2 plats 2 är bokad av en kund | En annan kund försöker boka rad 2 plats 2 och 3 | Bokningen nekas med *Konflikt* och meddelandet nämner rad 2 plats 2 |
| B3 | Två bokningar för samma säte och visning har båda passerat tjänstens kontroll (samtidiga bokningar) | Båda sparas i databasen | Databasens filtrerade unika index stoppar den andra (`DbUpdateException`) |
| B4 | En plats som tillhör en annan salong | Kunden försöker boka den | Bokningen nekas som *Ogiltig* |
| B5 | En kund har bokat och sedan avbokat en plats | En annan kund bokar samma plats | Platsen visas som ledig i säteskartan och den nya bokningen lyckas |
| B6 | En visning som börjar om en timme | Kunden försöker avboka, och sedan personalen | Kunden nekas (gränsen är 2 timmar före start), personalen lyckas |
| B7 | Kund A har en bokning | Kund B försöker se den, avboka den och lista sina bokningar | B får *Hittades inte* och en tom lista |
| B8 | Lyssnare är kopplade till eventen `BokningSkapad` och `BokningAvbokad` | En bokning skapas och avbokas | Båda eventen utlöses i rätt ordning med rätt bokningsnummer |
| B9 | En visning som började för 10 minuter sedan | Kunden försöker boka | Bokningen nekas som *Ogiltig* |

## Biljettyper, ändringsbegäran #32 (`BiljettypTester.cs`)

| Id | Givet | När | Så |
|---|---|---|---|
| T1 | Grundpris 120 kr | Priset räknas för Vuxen, Barn och Pensionär, med och utan VIP, samt Barn på en visning för 30 kr | 120 / 80 / 95, VIP 170 / 130, och lägst 0 kr (inget negativt pris) |
| T2 | En barntillåten visning | Kunden bokar en vuxen-, en barn- och en pensionärsbiljett, den sista på en VIP-plats | Varje plats får rätt typ och pris (120, 80, 145), och totalen är 345 kr |
| T3 | En film från 15 år | Kunden bokar en barnbiljett och sedan en vuxenbiljett | Barnbiljetten nekas som *Ogiltig* med ett förklarande meddelande, vuxenbiljetten lyckas |
| T4 | – | Samma säte skickas två gånger i samma bokning | Nekas som *Ogiltig* |
| T5 | En bokad pensionärsbiljett | Databasen och Mina bokningar läses | Biljettypen är sparad och visas |

Bokningstesterna B1–B9 skrevs om till den nya `SkapaBokningDto` (plats + biljettyp) och fungerar som regressionstester. De gick igenom även efter ändringen.

## Visningar (`VisningTester.cs`)

Den befintliga visningen börjar 18:00, filmen är 100 minuter och städtiden 20 minuter. Salongen är alltså upptagen 18:00–20:00.

| Id | Givet | När | Så |
|---|---|---|---|
| V1 | Salongen är upptagen 18:00–20:00 | En ny visning läggs 18:00, 19:00, 19:50 eller 16:30 (slutar 18:30) | Varje fall nekas med *Konflikt* "Salongen är upptagen …" |
| V2 | Samma salong | En ny visning läggs exakt 20:00 | Den godkänns |
| V3 | En annan salong | En visning läggs 18:00 | Den godkänns och alla platser är lediga |
| V4 | – | En visning läggs med en starttid som har passerat | Den nekas som *Ogiltig* |
| V5 | Den befintliga visningen | Den flyttas 15 minuter | Den godkänns, eftersom visningen inte krockar med sig själv |

## Salonger och filmer (`SalongOchFilmTester.cs`)

| Id | Givet | När | Så |
|---|---|---|---|
| S1 | – | En salong genereras med 5 rader × 8 platser och 2 VIP-rader | Kapaciteten är 40, rad 4–5 är VIP och rad 1–3 standard |
| S2 | – | En salong genereras med fler VIP-rader än rader | `ArgumentException` |
| S3 | Salong A har en visning | Personalen ändrar layouten och sedan bara namnet | Layoutändringen nekas med *Konflikt*, namnbytet lyckas |
| F1 | Filmen har en kommande visning | Personalen försöker ta bort filmen | Nekas med *Konflikt* |
| F2 | Tre filmer i olika genrer och åldersgränser | Sökning på "testfilm", och filter Drama + högst 7 år | Rätt filmer returneras i bokstavsordning |
| F3 | En nyss skapad film utan visningar | Personalen tar bort den | Den tas bort och går inte längre att hämta |

## Inloggning och behörighet (`BehorighetTester.cs`)

| Id | Givet | När | Så |
|---|---|---|---|
| R1 | Seedade konton | Inloggning med fel lösenord och med okänd e-post | Båda ger 401 med identiskt svar, så att man inte kan ta reda på vilka konton som finns |
| R2 | Ej inloggad | Hämtar filmer, försöker skapa en film | 200 respektive 401 |
| R3 | Inloggad som kund, personal och admin | Skapar en film | Kund 403, personal 201, admin 201 |
| R4 | Inloggad som kund, personal och admin | Hämtar användarlistan | Kund 403, personal 403, admin 200 |

---

## Manuella tester

Gränssnittet testades manuellt i webbläsaren (Chromium, 1280 px och 390 px mobilbredd) under sprint 3. Varje PR beskriver vad som testades. De viktigaste flödena:

1. Logga in med testkontona via snabbvalen på inloggningssidan. Menyn visar rätt länkar per roll.
2. Program, välj tid, välj platser i säteskartan. Sammanfattningen räknar VIP-tillägget, och bokningen ger ett bokningsnummer.
3. Mina bokningar, avboka i två steg. Bokningen flyttas till "Tidigare och avbokade".
4. Personal, ny visning som krockar. Krockmeddelandet visas i formuläret.
5. Kund på `/admin` får "Ingen behörighet".
