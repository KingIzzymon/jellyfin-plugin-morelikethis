namespace Jellyfin.Plugin.MoreLikeThis.Services
{
    using System;

    /// <summary>
    /// Represents a result for a similar item, including the item's identifier and a similarity score.
    /// </summary>
    /// <param name="ItemId">The identifier of the similar item.</param>
    /// <param name="Score">The similarity score for the item.</param>
    public record SimilarResult(Guid ItemId, double Score);
}