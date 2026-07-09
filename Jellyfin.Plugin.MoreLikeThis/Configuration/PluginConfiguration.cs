namespace Jellyfin.Plugin.MoreLikeThis.Configuration
{
    using MediaBrowser.Model.Plugins;

    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets how many similar items to show in the row.
        /// </summary>
        public int MaxResults { get; set; } = 16;

        /// <summary>
        /// Gets or sets the minutes between full similarity index rebuilds (scheduled task).
        /// A full rebuild re-scores every item against every other item in the
        /// same library type, so keep this infrequent on large libraries.
        /// </summary>
        public int FullRebuildIntervalHours { get; set; } = 24;

        /// <summary>
        /// Gets or sets a value indicating whether to also weigh cast/crew overlap,
        /// not just genres/tags/studio. More accurate, but more CPU per comparison.
        /// </summary>
        public bool UseCastCrewSignal { get; set; } = true;
    }
}
