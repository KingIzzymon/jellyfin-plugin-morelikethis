namespace Jellyfin.Plugin.MoreLikeThis.ScheduledTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Jellyfin.Plugin.MoreLikeThis.Services;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Entities.Movies;
    using MediaBrowser.Controller.Entities.TV;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Model.Tasks;

    /// <summary>
    /// Full recompute of the similarity graph.
    /// </summary>
    public class BuildSimilarityIndexTask : IScheduledTask
    {
        private readonly ILibraryManager libraryManager;
        private readonly SimilarityStore store;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildSimilarityIndexTask"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager instance.</param>
        /// <param name="store">The similarity store instance.</param>
        public BuildSimilarityIndexTask(ILibraryManager libraryManager, SimilarityStore store)
        {
            this.libraryManager = libraryManager;
            this.store = store;
        }

        /// <summary>
        /// Gets the name of the scheduled task.
        /// </summary>
        public string Name => "Build MoreLikeThis Index";

        /// <summary>
        /// Gets the unique key of the scheduled task.
        /// </summary>
        public string Key => "MoreLikeThisBuildIndex";

        /// <summary>
        /// Gets the description of the scheduled task.
        /// </summary>
        public string Description => "Recomputes similarity lists for every movie and show.";

        /// <summary>
        /// Gets the category of the scheduled task.
        /// </summary>
        public string Category => "Library";

        /// <summary>
        /// Gets the default triggers for the scheduled task.
        /// </summary>
        /// <returns>An enumerable of task trigger information.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var hours = Plugin.Instance?.Configuration.FullRebuildIntervalHours ?? 24;
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(hours).Ticks,
            };
        }

        /// <summary>
        /// Executes the scheduled task.
        /// </summary>
        /// <param name="progress">The progress reporter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var engine = new SimilarityEngine(Plugin.Instance?.Configuration.UseCastCrewSignal ?? true);
            var maxResults = Plugin.Instance?.Configuration.MaxResults ?? 16;

            var movies = this.libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
            { IncludeItemTypes = new[] { Jellyfin.Data.Enums.BaseItemKind.Movie } }).Cast<BaseItem>().ToList();

            var series = this.libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
            { IncludeItemTypes = new[] { Jellyfin.Data.Enums.BaseItemKind.Series } }).Cast<BaseItem>().ToList();

            this.RebuildGroup(movies, engine, maxResults, progress, 0, 50, cancellationToken);
            this.RebuildGroup(series, engine, maxResults, progress, 50, 100, cancellationToken);

            return Task.CompletedTask;
        }

        private void RebuildGroup(
            List<BaseItem> items,
            SimilarityEngine engine,
            int maxResults,
            IProgress<double> progress,
            double progressStart,
            double progressEnd,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < items.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = items[i];

                var scored = new List<SimilarResult>();
                foreach (var candidate in items)
                {
                    var s = engine.Score(source, candidate, this.libraryManager);
                    if (s > 0)
                    {
                        scored.Add(new SimilarResult(candidate.Id, s));
                    }
                }

                var top = scored.OrderByDescending(r => r.Score).Take(maxResults);
                this.store.ReplaceForItem(source.Id, top);

                var pct = progressStart + (progressEnd - progressStart) * ((double)(i + 1) / Math.Max(1, items.Count));
                progress.Report(pct);
            }
        }
    }
}