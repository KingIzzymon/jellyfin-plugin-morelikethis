namespace Jellyfin.Plugin.MoreLikeThis
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Jellyfin.Plugin.MoreLikeThis.Services;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Controller.Plugins;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// IServerEntryPoint runs once at startup and stays resident, which is how
    /// we listen for real-time library events instead of waiting for the
    /// nightly scheduled task. Jellyfin's ILibraryManager fires ItemAdded /
    /// ItemUpdated / ItemRemoved for every add, metadata refresh, and delete.
    ///
    /// Strategy:
    ///  - New/updated item -> immediately score it against the rest of its
    ///    library type and store its own row.
    ///  - Removed item -> purge its rows everywhere.
    /// </summary>
    public class LibraryEventHandler : IServerEntryPoint
    {
        private readonly ILibraryManager libraryManager;
        private readonly SimilarityStore store;
        private readonly ILogger<LibraryEventHandler> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryEventHandler"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager service.</param>
        /// <param name="store">The similarity store service.</param>
        /// <param name="logger">The logger service.</param>
        public LibraryEventHandler(ILibraryManager libraryManager, SimilarityStore store, ILogger<LibraryEventHandler> logger)
        {
            this.libraryManager = libraryManager;
            this.store = store;
            this.logger = logger;
        }

        /// <summary>
        /// Runs the plugin's main logic.
        /// </summary>
        public Task RunAsync()
        {
            this.libraryManager.ItemAdded += this.OnItemAddedOrUpdated;
            this.libraryManager.ItemUpdated += this.OnItemAddedOrUpdated;
            this.libraryManager.ItemRemoved += this.OnItemRemoved;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes of the plugin and unregisters event handlers.
        /// </summary>
        public void Dispose()
        {
            this.libraryManager.ItemAdded -= this.OnItemAddedOrUpdated;
            this.libraryManager.ItemUpdated -= this.OnItemAddedOrUpdated;
            this.libraryManager.ItemRemoved -= this.OnItemRemoved;
        }

        private void OnItemAddedOrUpdated(object? sender, ItemChangeEventArgs e)
        {
            if (e.Item is not Movie && e.Item is not Series)
            {
                return;
            }

            try
            {
                var engine = new SimilarityEngine(Plugin.Instance?.Configuration.UseCastCrewSignal ?? true);
                var maxResults = Plugin.Instance?.Configuration.MaxResults ?? 16;

                var kind = e.Item.GetType();
                var peers = this.libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
                {
                    IncludeItemTypes = new[] { kind == typeof(Movie) ? Jellyfin.Data.Enums.BaseItemKind.Movie : Jellyfin.Data.Enums.BaseItemKind.Series },
                });

                var scored = peers
                    .Where(p => p.Id != e.Item.Id)
                    .Select(p => new SimilarResult(p.Id, engine.Score(e.Item, p, this.libraryManager)))
                    .Where(r => r.Score > 0)
                    .OrderByDescending(r => r.Score)
                    .Take(maxResults);

                this.store.ReplaceForItem(e.Item.Id, scored);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to incrementally update similarity for {ItemName}", e.Item.Name);
            }
        }

        private void OnItemRemoved(object? sender, ItemChangeEventArgs e)
        {
            this.store.RemoveItem(e.Item.Id);
        }
    }
}
