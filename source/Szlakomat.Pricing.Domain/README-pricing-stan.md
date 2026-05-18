# Szlakomat.Pricing — Stan implementacji

> Etap 1 ukończony · .NET 10 · C# · DDD · xUnit · FluentAssertions

---

## Projekty

| Projekt | Opis |
|---|---|
| `Szlakomat.Pricing.Domain` | Czysta domena pricingu — bez zależności od MediatR, HTTP ani baz danych |
| `Szlakomat.Pricing.Domain.Tests` | Testy jednostkowe domeny — pełne pokrycie Etapu 1 |

---

## Kontekst architektoniczny

Pricing jest osobnym bounded contextem — nie modyfikuje istniejących projektów `Products.*`.

Zależności zewnętrzne `Pricing.Domain`:
- `CommunityToolkit.Diagnostics` — guardy walidacyjne
- Shared kernel z `Products.Domain` — `IApplicabilityConstraint`, `ApplicabilityContext`

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
