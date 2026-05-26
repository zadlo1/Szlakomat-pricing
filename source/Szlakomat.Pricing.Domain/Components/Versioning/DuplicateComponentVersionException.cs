namespace Szlakomat.Pricing.Domain.Components.Versioning;

/// <summary>
/// Próba dodania wersji identycznej z istniejącą przy strategii <see cref="VersionAdditionStrategy.RejectIdentical"/>.
/// </summary>
public sealed class DuplicateComponentVersionException : InvalidOperationException
{
    public DuplicateComponentVersionException(string componentName)
        : base($"Komponent '{componentName}' ma już wersję o identycznej konfiguracji.")
    {
        ComponentName = componentName;
    }

    public string ComponentName { get; }
}
