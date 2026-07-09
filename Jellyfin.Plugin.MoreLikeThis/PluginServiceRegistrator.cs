namespace Jellyfin.Plugin.MoreLikeThis
{
    using Jellyfin.Plugin.MoreLikeThis.Services;
    using MediaBrowser.Controller;
    using MediaBrowser.Controller.Plugins;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The plugin service registrator.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <summary>
        /// Registers the services for the plugin.
        /// </summary>
        /// <param name="serviceCollection">The service collection to which plugin services should be added.</param>
        /// <param name="applicationHost">The server application host instance.</param>
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<SimilarityStore>();
            serviceCollection.AddSingleton<IServerEntryPoint, LibraryEventHandler>();
        }
    }
}
