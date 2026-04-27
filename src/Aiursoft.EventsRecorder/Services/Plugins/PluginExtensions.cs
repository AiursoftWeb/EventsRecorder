using System.Reflection;

namespace Aiursoft.EventsRecorder.Services.Plugins;

public static class PluginServiceExtensions
{
    /// <summary>
    /// Scans the given assembly for all non-abstract IPlugin implementations and
    /// registers each as IPlugin singleton. Adding a new plugin class is the only
    /// change required — no Startup edits needed after the initial call.
    /// </summary>
    public static IServiceCollection AddAssemblyPlugins(
        this IServiceCollection services,
        Assembly assembly)
    {
        assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract)
            .ToList()
            .ForEach(t => services.AddSingleton(typeof(IPlugin), t));

        return services;
    }
}
