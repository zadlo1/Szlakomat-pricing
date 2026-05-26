using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Components;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Facade;
using Szlakomat.Pricing.Domain.Calculators;

namespace Szlakomat.Pricing.Domain.Seed;
/// <summary>
/// Pierwsze drzewo komponentów Wawelu — Skarbiec Koronny z taryfami STANDARD / ulgową / B2B.
/// </summary>
public static class WawelSkarbiecTicketSeed
{
    public const string WawelTicketComponent = "wawel_ticket";
    public const string WawelTicketWithVatComponent = "wawel_ticket_with_vat";
    public static PricingFacade Seed(PricingFacade facade)
    {
        facade
            .AddFixedCalculator("skarbiec_standard_unit", Money.Pln(47m))
            .AddFixedCalculator("skarbiec_reduced_unit", Money.Pln(35m))
            .AddPercentageCalculator("skarbiec_b2b_discount", 85m)
            .AddPercentageCalculator("vat_8_percent", 8m)
            // Base node for dependencies: produces Total = 47 PLN × quantity
            // It is not part of the final sum/breakdown.
            .CreateSimpleComponent(
                "skarbiec_base",
                "skarbiec_standard_unit",
                ApplicabilityConstraint.AlwaysTrue(),
                contributesToTotal: false)
            .CreateSimpleComponent(
                "skarbiec_standard",
                "skarbiec_standard_unit",
                ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard))
            .CreateSimpleComponent(
                "skarbiec_reduced",
                "skarbiec_reduced_unit",
                ApplicabilityConstraint.In(
                    ApplicabilityKeys.CustomerType,
                    CustomerTypes.Reduced,
                    CustomerTypes.Child,
                    CustomerTypes.Senior,
                    CustomerTypes.Student))
            .CreateSimpleComponent(
                "skarbiec_b2b",
                "skarbiec_b2b_discount",
                ApplicabilityConstraint.And(
                    ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.B2B),
                    ApplicabilityConstraint.GreaterThan(ApplicabilityKeys.GroupSize, 9)))
            .CreateCompositeComponent(
                WawelTicketComponent,
                ["skarbiec_base", "skarbiec_standard", "skarbiec_reduced", "skarbiec_b2b"],
                parameterDependencies:
                [
                    new ParameterDependency(
                        "skarbiec_b2b",
                        PercentageCalculator.BaseAmountKey,
                        ["skarbiec_base"]),
                ])
            .CreateSimpleComponent(
                "vat_8",
                "vat_8_percent",
                ApplicabilityConstraint.AlwaysTrue())
            .CreateCompositeComponent(
                WawelTicketWithVatComponent,
                ["skarbiec_base", "skarbiec_standard", "skarbiec_reduced", "skarbiec_b2b", "vat_8"],
                parameterDependencies:
                [
                    new ParameterDependency(
                        "skarbiec_b2b",
                        PercentageCalculator.BaseAmountKey,
                        ["skarbiec_base"]),
                    new ParameterDependency(
                        "vat_8",
                        PercentageCalculator.BaseAmountKey,
                        ["skarbiec_standard", "skarbiec_reduced", "skarbiec_b2b"]),
                ]);
        return facade;
    }
}
