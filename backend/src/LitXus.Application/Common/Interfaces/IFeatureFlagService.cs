namespace LitXus.Application.Common.Interfaces;

public enum Module
{
    Accounting,
    Sales,
    Inventory,
}

/// <summary>See docs/16_Feature_Flags.md — backed by the Licenses table, cached in memory, invalidated on toggle.</summary>
public interface IFeatureFlagService
{
    bool IsEnabled(Module module);
    IReadOnlyList<Module> EnabledModules { get; }
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
