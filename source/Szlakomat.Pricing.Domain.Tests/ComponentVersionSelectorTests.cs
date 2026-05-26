using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Components.Versioning;

namespace Szlakomat.Pricing.Domain.Tests;

public class ComponentVersionSelectorTests
{
    [Fact]
    public void Select_WhenNoVersionMatchesVisitDate_ShouldThrow()
    {
        // Arrange
        IReadOnlyList<IComponentVersionData> versions =
        [
            new SimpleComponentVersionData(
                Validity.Until(new DateOnly(2025, 6, 30)),
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "calc",
                ApplicabilityConstraint.AlwaysTrue(),
                new Dictionary<string, string>(),
                true),
        ];

        // Act
        var act = () => Select(versions, new DateOnly(2025, 7, 1));

        // Assert
        act.Should().Throw<NoActiveComponentVersionException>();
    }

    [Fact]
    public void Select_WhenMultipleValidVersions_ShouldPickLatestValidFrom()
    {
        // Arrange
        var older = new SimpleComponentVersionData(
            Validity.Always(),
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "old",
            ApplicabilityConstraint.AlwaysTrue(),
            new Dictionary<string, string>(),
            true);
        var newer = new SimpleComponentVersionData(
            Validity.FromDate(new DateOnly(2025, 7, 1)),
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            "new",
            ApplicabilityConstraint.AlwaysTrue(),
            new Dictionary<string, string>(),
            true);
        IReadOnlyList<IComponentVersionData> versions = [older, newer];

        // Act
        var selected = Select(versions, new DateOnly(2025, 8, 1));

        // Assert
        selected.Should().Be(newer);
    }

    private static IComponentVersionData Select(IReadOnlyList<IComponentVersionData> versions, DateOnly visitDate) =>
        ComponentVersionSelector.Select(
            versions,
            visitDate,
            "test",
            v => v.Validity,
            v => v.DefinedAt);
}
