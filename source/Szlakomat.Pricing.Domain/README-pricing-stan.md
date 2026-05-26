# Szlakomat.Pricing — Stan implementacji

> Etap 1–3 ukończone · .NET 10 · C# · DDD · xUnit · FluentAssertions

---

## Projekty

| Projekt | Opis |
|---|---|
| `Szlakomat.Pricing.Domain` | Czysta domena pricingu — bez zależności od MediatR, HTTP ani baz danych |
| `Szlakomat.Pricing.Domain.Tests` | Testy jednostkowe + journey domeny — Etap 1–3 |

---

## Kontekst architektoniczny

Pricing jest osobnym bounded contextem — nie modyfikuje istniejących projektów `Products.*`.

Zależności zewnętrzne `Pricing.Domain`:
- `CommunityToolkit.Diagnostics` — guardy walidacyjne
- Shared kernel z `Products.Domain` — `IApplicabilityConstraint`, `ApplicabilityContext`, `Validity`

---

## Co jest zaimplementowane

### Money (`Money/Money.cs`)

Niemutowalny typ wartościowy (`sealed record`) łączący kwotę z walutą ISO.

- `Amount` — decimal, zaokrąglony do 2 miejsc (`MidpointRounding.AwayFromZero`), nigdy ujemny
- `Currency` — string ISO, normalizowany do uppercase
- Fabryki: `Pln(amount)`, `Of(amount, currency)`, `Zero(currency)`
- Arytmetyka: `Add`, `Subtract`, `Multiply`, `Divide` — każda operacja zwraca nową instancję
- Guardy: niezgodność walut → `InvalidOperationException`; ujemny wynik / czynnik → `ArgumentOutOfRangeException`
- `ToString()` → `"PLN 47.00"` zawsze z kropką (`CultureInfo.InvariantCulture`)

### PricingContext (`Context/PricingContext.cs`)

Niemutowalny parametr zewnętrzny przekazywany do kalkulatorów.

- `VisitDate` (DateOnly), `Timestamp` (DateTime), `CustomerType` (string), `GroupSize` (int ≥ 1)
- Fabryki: `Individual(visitDate)`, `For(visitDate, customerType)`, `ForGroup(visitDate, customerType, groupSize)`
- `ToApplicabilityDictionary()` — konwersja do formatu `ApplicabilityContext` z `Products.Domain`

### CustomerTypes / ApplicabilityKeys (`Context/`)

Stałe string eliminujące magic strings:
- Typy klientów: `STANDARD`, `REDUCED`, `CHILD`, `SENIOR`, `STUDENT`, `B2B`
- Klucze applicability: `CustomerType`, `GroupSize`, `VisitDate`

### PricingParameters (`Parameters/PricingParameters.cs`)

Niemutowalna mapa `string → object`. Każde `With(key, value)` zwraca nową instancję.

- Fabryki: `Empty()`, `Of(key, value)`
- Typowane gettery: `GetMoney`, `GetDecimal`, `GetInt`, `GetString`, `Contains`
- Błędny typ lub brak klucza → wyjątek z czytelnym komunikatem

### ICalculator i Interpretation (`Calculators/ICalculator.cs`)

Interfejs kalkulatora: `Calculate(PricingParameters) → Money`. Deklaruje `Interpretation`, `Formula`, `Type`.

Enum `Interpretation`: `Total` (kwota za całą grupę), `Unit` (cena za osobę), `Marginal` (cena n-tej jednostki).

### Pięć kalkulatorów

| Kalkulator | Formuła | Interpretacja | Zastosowanie |
|---|---|---|---|
| `SimpleFixedCalculator` | `f(x) = kwota` | Unit | Skarbiec 47 PLN, Smocza Jama 15 PLN |
| `StepFunctionCalculator` | `base + floor(qty/step) × inc` | Total | Cennik grupowy, opłata za nadwyżkę |
| `DiscretePointsCalculator` | tabela `qty → cena` | Total | Bilet normalny/ulgowy/dziecięcy |
| `DailyIncrementCalculator` | `start + days × increment` | Unit | Early bird (cena rośnie z dniem) |
| `PercentageCalculator` | `base × rate%` | Total | VAT 8%, rabat B2B 15% |

Konwencja granic w `StepFunctionCalculator` i `DiscretePointsCalculator`: `[od, do)`.

### Trzy adaptery interpretacji (`Calculators/Adapters/InterpretationAdapters.cs`)

Wzorzec Decorator — opakowują `ICalculator` i zmieniają jego interpretację.

| Adapter | Formuła |
|---|---|
| `UnitToTotalAdapter` | `Total(q) = Unit(q) × quantity` |
| `TotalToUnitAdapter` | `Unit(q) = Total(q) / quantity` |
| `TotalToMarginalAdapter` | `Marginal(n) = Total(n) − Total(n−1)` |

### ICalculatorRepository + InMemoryCalculatorRepository (`Repository/`)

Interfejs: `Save`, `FindByName`, `FindById`, `FindAll`. Implementacja słownikowa in-memory na potrzeby Etapu 1 i testów.

### PricingFacade (`Facade/PricingFacade.cs`)

Główny punkt wejścia. Fluent API do rejestracji kalkulatorów i orkiestracji obliczeń.

Rejestracja: `AddFixedCalculator`, `AddPercentageCalculator`, `AddStepFunctionCalculator`, `AddDiscreteCalculator`, `AddDailyIncrementCalculator`.

Obliczenia: `Calculate` (natywna interpretacja), `CalculateTotal` (zawsze Total), `CalculateUnitPrice` (zawsze Unit) — adaptery dobierane automatycznie.

Diagnostyka: `ListCalculators() → IReadOnlyList<CalculatorView>`.

---

## Etap 2 — Komponenty (Applicability + Validity)

Etap 2 dodaje warstwę komponentów (wzorzec Composite) nad kalkulatorami — to pierwsza „semantyka cennika”.

### IComponent / SimpleComponent / CompositeComponent (`Components/`)

- `IComponent` — wspólny kontrakt dla liścia i kompozytu: `Calculate`, `CalculateBreakdown`, `Interpretation`, `Name`, `ComponentId`.
- `SimpleComponent` — liść:
  - sprawdza `Validity.IsValidAt(context.VisitDate)` oraz `IApplicabilityConstraint.IsSatisfiedBy(context.ToApplicabilityContext())`
  - wzbogaca parametry o `quantity = context.GroupSize`
  - normalizuje wynik do `Total` przez adaptery interpretacji (Unit→Total itp.)
  - wspiera aliasy parametrów przez `parameterMappings`
  - może być oznaczony jako **niekontrybuujący do sumy** (`contributesToTotal=false`) — używane jako węzeł bazowy dla zależności
- `CompositeComponent` — kompozyt:
  - własne Applicability + Validity
  - liczy dzieci w kolejności, wspiera `ParameterDependency` (wstrzykiwanie sumy poprzednich dzieci do parametru zależnego dziecka)
  - do sumy i breakdownu bierze wyłącznie dzieci, które kontrybuują i zwróciły kwotę > 0
  - jeśli żadne dziecko nie jest stosowalne → zwraca `Money.Zero`

### PriceBreakdown (`Components/PriceBreakdown.cs`)

Drzewo rozbicia ceny: `ComponentName`, `Total`, `Children`, `IsLeaf`.

### IComponentRepository + InMemoryComponentRepository (`Repository/`)

Repozytorium komponentów analogiczne do repozytorium kalkulatorów: `Save`, `FindByName`, `FindById`, `FindAll`.

### PricingFacade — API Etapu 2

Rozszerzenia fasady o rejestrację i obliczenia komponentów:
- `CreateSimpleComponent(...)`
- `CreateCompositeComponent(...)`
- `CalculateComponent(...)`
- `CalculateComponentBreakdown(...)`
- `ListComponents() → IReadOnlyList<ComponentView>`

### Seed — pierwsze drzewo Wawelu (`Seed/WawelSkarbiecTicketSeed.cs`)

Pierwsze drzewo komponentów dla Skarbca:
- `wawel_ticket` — taryfy STANDARD / REDUCED / B2B
- `wawel_ticket_with_vat` — VAT 8% jako dziecko liczone od sumy netto

B2B jest realizowane jako procent od bazowej kwoty po przeskalowaniu przez quantity:
- `skarbiec_base` (niekontrybuujący) liczy `47 PLN × quantity`
- `skarbiec_b2b` (`PercentageCalculator` 85%) liczy rabat od `skarbiec_base.Total`

---

## Etap 3 — Wersjonowanie

Etap 3 dodaje historię wersji komponentów. `CreateSimpleComponent` / `CreateCompositeComponent` rejestrują `VersionedComponent` z pierwszą wersją.

### VersionedComponent (`Components/Versioning/VersionedComponent.cs`)

- Przechowuje listę snapshotów (`SimpleComponentVersionData` / `CompositeComponentVersionData`)
- Przy `Calculate` / `CalculateBreakdown` wybiera wersję przez `ComponentVersionSelector` i materializuje liść lub kompozyt
- Dzieci kompozytu rozwiązywane po nazwie z repozytorium — wersjonowane dzieci dostają własną wersję na `VisitDate`
- `ContributesToTotal` delegowane ze snapshotu (np. `skarbiec_base` pozostaje niekontrybuujący)

### ComponentVersion (`Components/Versioning/VersionedComponent.cs`)

Publiczny widok metadanych: `Validity` + `DefinedAt`.

### Wybór wersji (`ComponentVersionSelector`)

1. Filtruj wersje, gdzie `Validity.IsValidAt(visitDate)`
2. Wybierz najpóźniejszy `validFrom` (`Validity.From`)
3. Przy remisie — najpóźniejszy `DefinedAt`
4. Brak kandydatów → `NoActiveComponentVersionException`

### PricingFacade — API Etapu 3

- `UpdateSimpleComponent(name, calculatorName, validity, …)` — nowa wersja liścia
- `UpdateCompositeComponent(name, childNames, validity, …)` — nowa wersja kompozytu
- `VersionAdditionStrategy.RejectIdentical` (domyślna) / `AllowAll`
- `DuplicateComponentVersionException` przy duplikacie konfiguracji

### Wyjątki

| Wyjątek | Kiedy |
|---|---|
| `NoActiveComponentVersionException` | Brak wersji obowiązującej w dniu wizyty |
| `DuplicateComponentVersionException` | Identyczna wersja przy `RejectIdentical` |

---

## Testy

Wzorzec: Arrange / Act / Assert + FluentAssertions. Każdy test niezależny — brak wspólnego stanu.

| Klasa testowa | Co pokrywa |
|---|---|
| `MoneyTests` | Fabryki, zaokrąglanie, guardy, arytmetyka, równość, `ToString` z InvariantCulture |
| `SimpleFixedCalculatorTests` | Stała kwota niezależnie od parametrów, przypadek 0 PLN |
| `StepFunctionCalculatorTests` | Granice `[od, do)`: qty 1–4 / 5–9 / 10–14 / 15+ |
| `DiscretePointsCalculatorTests` | Dokładne trafienie w punkt, brak punktu → wyjątek |
| `DailyIncrementCalculatorTests` | Ten sam dzień, +1 dzień, data wcześniejsza niż referencyjna |
| `PercentageCalculatorTests` | VAT 23%, rabat B2B 15%, zaokrąglanie, brak parametru → wyjątek |
| `PricingFacadeTests` | Orkiestracja, auto-konwersja interpretacji, wiele kalkulatorów, diagnostyka |
| `PricingContextTests` | Fabryki, guardy, `ToApplicabilityDictionary()` |
| `SimpleComponentTests` | Validity, Applicability, quantity→Total, parameterMappings |
| `CompositeComponentTests` | Applicability rodzica, brak pasującego dziecka → zero, `ParameterDependency` (np. VAT) |
| `WawelTicketPricingJourneyTests` | Journey Etapu 2 przez `PricingFacade` (STANDARD, SENIOR, B2B, breakdown, VAT od sumy) |
| `HistoricalCalculationTests` | Journey Etapu 3 — zmiana ceny w czasie, wygasanie promocji, tiebreaker `DefinedAt`, wersjonowane dzieci |
| `ComponentVersionSelectorTests` | Reguła wyboru wersji, brak aktywnej wersji |
