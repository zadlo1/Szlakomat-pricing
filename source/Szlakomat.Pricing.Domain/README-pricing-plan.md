# Szlakomat.Pricing — Plan rozwoju

> Oparty na archetypie: _Cena to funkcja, nie liczba._  
> Stack: .NET 10 · C# · DDD · CQRS via MediatR · Clean Architecture

---

## Zasady przewodnie

Trzy zasady determinujące każdą decyzję projektową:

1. **Cena to funkcja, nie liczba** — cennik to zestaw reguł obliczeniowych. System musi umieć wywołać tę funkcję, wyjaśnić wynik (breakdown) i odtworzyć ją dla dowolnego momentu w czasie.
2. **Trzy osie modelu są niezależne** — kalkulator odpowiada _jak liczymy_, Applicability odpowiada _czy liczymy_, Validity odpowiada _kiedy obowiązuje_. Zmiany na jednej osi nie dotykają pozostałych.
3. **Granice przedziałów to zawsze `[od, do)`** — eliminuje dwuznaczności na stykach. Obowiązuje dla wszystkich zakresów.

---

## Zasady, których NIE łamiemy

1. `Pricing.Domain` nie zna MediatR — fasada nie implementuje `IRequest`
2. Kalkulator nie zna klienta — `CustomerType` żyje w `PricingContext`, nie w kalkulatorze
3. Granice zawsze `[od, do)` — bez wyjątków, we wszystkich zakresach
4. Brak aktywnej wersji → wyjątek — system nie może wycenić czegoś cicho na zero
5. Komponenty są niemutowalne po utworzeniu — aktualizacja = nowa wersja, nie edycja
6. Pricing nie importuje agregatów Products — tylko shared kernel (`Validity`, `IApplicabilityConstraint`, `Result`)

---

## Docelowa struktura projektów

```
source/
  Szlakomat.Products.Domain/           ← bez zmian
  Szlakomat.Products.Application/      ← bez zmian
  Szlakomat.Products.Infrastructure/   ← bez zmian
  Szlakomat.Products.Api/              ← dodajemy tylko PricingController (Etap 4)

  Szlakomat.Pricing.Domain/            ✅ Etap 1
  Szlakomat.Pricing.Application/       ⬜ Etap 4
  Szlakomat.Pricing.Infrastructure/    ⬜ Etap 4
  Szlakomat.Pricing.Domain.Tests/      ✅ Etap 1
  Szlakomat.Pricing.Application.Tests/ ⬜ Etap 4
```

---

## Etap 1 — Fundamenty ✅ UKOŃCZONY

Czysta domena pricingu bez integracji z resztą systemu. Działa samodzielnie, w pełni pokryta testami.

### Checklist

- [x] `Szlakomat.Pricing.Domain.csproj` dodany do solucji
- [x] `Money` — arytmetyka, guard na ujemne, guard na walutę, niemutowalność, `InvariantCulture` w `ToString`
- [x] `PricingContext` — fabryki, konwersja do `Dictionary` dla `ApplicabilityContext`
- [x] `CustomerTypes` — stałe (`STANDARD`, `REDUCED`, `CHILD`, `SENIOR`, `STUDENT`, `B2B`)
- [x] `PricingParameters` — niemutowalna mapa, typowane gettery
- [x] `Interpretation` (enum: `Total`, `Unit`, `Marginal`)
- [x] `ICalculator` — interfejs z `Id`, `Name`, `Type`, `Formula`, `Interpretation`, `Calculate`
- [x] `SimpleFixedCalculator` — stała cena, interpretacja `Unit`
- [x] `StepFunctionCalculator` — cena schodkowa `[od, do)`, interpretacja `Total`
- [x] `DiscretePointsCalculator` — tabela punktów, brak trafienia → wyjątek, interpretacja `Total`
- [x] `DailyIncrementCalculator` — przyrost dzienny, interpretacja `Unit`
- [x] `PercentageCalculator` — procent od bazy, interpretacja `Total`
- [x] `UnitToTotalAdapter`, `TotalToUnitAdapter`, `TotalToMarginalAdapter`
- [x] `ICalculatorRepository` + `InMemoryCalculatorRepository`
- [x] `PricingFacade` — fluent API rejestracji, `Calculate` / `CalculateTotal` / `CalculateUnitPrice`, auto-adaptery
- [x] `CalculatorView` — DTO diagnostyczne
- [x] `Szlakomat.Pricing.Domain.Tests.csproj` dodany do solucji
- [x] `MoneyTests` — fabryki, zaokrąglanie, guardy, arytmetyka, równość, `ToString`
- [x] `SimpleFixedCalculatorTests`, `StepFunctionCalculatorTests`, `DiscretePointsCalculatorTests`
- [x] `DailyIncrementCalculatorTests`, `PercentageCalculatorTests`
- [x] `PricingFacadeTests` — orkiestracja, konwersja interpretacji, diagnostyka
- [x] `PricingContextTests` — fabryki, guardy, `ToApplicabilityDictionary`

---

## Etap 2 — Komponenty ⬜ TODO

Warstwa komponentów z osią Applicability i Validity. Pierwsza prawdziwa kompozycja cennika.

### Checklist

- [ ] `IComponent` (sealed interface) z `Calculate`, `CalculateBreakdown`, `Interpretation`, `Name`, `ComponentId`
- [ ] `SimpleComponent` (liść) — opakowuje `ICalculator`, sprawdza `Validity` i `IApplicabilityConstraint`
- [ ] `CompositeComponent` (węzeł) — własne Applicability, sumowanie dzieci, `parameterDependencies`
- [ ] `PriceBreakdown` — drzewo: `ComponentName`, `Total`, `Children`
- [ ] `IComponentRepository` + in-memory implementacja
- [ ] `PricingFacade` rozszerzona o `CreateSimpleComponent`, `CreateCompositeComponent`, `CalculateComponent`, `CalculateComponentBreakdown`
- [ ] Seed: pierwsze drzewo Wawelu (Skarbiec z taryfami STANDARD / REDUCED / B2B)
- [ ] `SimpleComponentTests`, `CompositeComponentTests` — Validity, Applicability, brak pasującego dziecka → zero
- [ ] Journey testy przez `PricingFacade`: 7+ scenariuszy (STANDARD, SENIOR, B2B, brak applicability, VAT od sumy)

---

## Etap 3 — Wersjonowanie ⬜ TODO

Każdy komponent ma historię wersji. Obliczenia historyczne używają reguł z dnia zdarzenia.

### Checklist

- [ ] `ComponentVersion` — snapshot konfiguracji + `Validity` + `DefinedAt` (DateTime)
- [ ] `SimpleComponent` i `CompositeComponent` przechowują listę `ComponentVersion`
- [ ] Reguła wyboru wersji: najpóźniejszy `validFrom`, `DefinedAt` jako tiebreaker
- [ ] Brak aktywnej wersji → wyjątek (nie cicha zero)
- [ ] `PricingFacade.UpdateComponent(name, newConfig, validity)` — dodaje nową wersję, stare niemodyfikowalne
- [ ] Strategia walidacji: `RejectIdentical` (domyślna), `AllowAll` (testy)
- [ ] `HistoricalCalculationTests` — obliczenia historyczne, wygasanie promocji, nakładające się wersje

---

## Etap 4 — Integracja i API ⬜ TODO

Pricing dostępny przez HTTP. Integracja z `CatalogEntry`. Nowy bounded context w kontenerze DI.

### Checklist

- [ ] `Szlakomat.Pricing.Application.csproj` i `Szlakomat.Pricing.Infrastructure.csproj` dodane do solucji
- [ ] `CatalogEntryPricing` — mapowanie `CatalogEntryId → ComponentName`
- [ ] Komenda `CalculatePrice` + handler (sealed, CQRS przez MediatR)
- [ ] `PricingController` — `POST /api/pricing/calculate`
- [ ] `PricingServiceExtensions.AddPricingModule` — rejestracja DI + seed
- [ ] `WawelPricingSeed` — pełny seed przez `PricingFacade`
- [ ] Rejestracja w `Program.cs` obok `AddProductModule()`
- [ ] Journey testy przez MediatR: 6+ scenariuszy (turysta, senior, B2B, brak dostępności, nieznany wpis)

---

## Etap 5 — Pełne drzewo Wawelu i migracja ⬜ TODO

Kompletny cennik Wawelu jako drzewo komponentów. Usunięcie cen z `Metadata`.

### Checklist

- [ ] Pełne drzewo Wawelu: 13 wystaw + 8 pakietów + 5 usług
- [ ] Sezonowość: `Validity.Between` dla Smoczej Jamy, Ogrodów, Baszty
- [ ] Bilet roczny: `Validity` per zakup
- [ ] Dual-write seed (Metadata + PricingEngine) — stary i nowy kod równolegle
- [ ] Journey testy porównujące wyniki Metadata vs PricingEngine
- [ ] Testy architektury dla nowego BC: 6 reguł w `Products.Architecture.Tests`
- [ ] Usunięcie `price_standard_pln` / `price_reduced_pln` z `WawelCatalogSeed.cs`
