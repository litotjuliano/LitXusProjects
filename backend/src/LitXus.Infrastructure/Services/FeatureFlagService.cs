using LitXus.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Services;

/// <summary>
/// Caches the current Licenses row in memory; toggling flags in Admin > Feature Flags calls
/// InvalidateAsync to force a re-read. See docs/16_Feature_Flags.md §16.1.
/// </summary>
public class FeatureFlagService(IAppDbContext db) : IFeatureFlagService
{
    private IReadOnlyList<Module>? _cachedEnabledModules;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public IReadOnlyList<Module> EnabledModules => _cachedEnabledModules ?? LoadSync();

    public bool IsEnabled(Module module) => EnabledModules.Contains(module);

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _cachedEnabledModules = null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private IReadOnlyList<Module> LoadSync()
    {
        var license = db.Licenses.AsNoTracking().OrderByDescending(l => l.IssuedAtUtc).FirstOrDefault();
        if (license is null)
        {
            _cachedEnabledModules = [];
            return _cachedEnabledModules;
        }

        var modules = license.GetEnabledModuleList()
            .Select(m => Enum.TryParse<Module>(m, out var parsed) ? parsed : (Module?)null)
            .Where(m => m.HasValue)
            .Select(m => m!.Value)
            .ToList();

        _cachedEnabledModules = modules;
        return modules;
    }
}
