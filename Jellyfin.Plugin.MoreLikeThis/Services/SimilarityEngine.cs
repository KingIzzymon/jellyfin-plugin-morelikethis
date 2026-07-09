namespace Jellyfin.Plugin.MoreLikeThis.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Jellyfin.Plugin.MoreLikeThis.Configuration;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Entities.Movies;
    using MediaBrowser.Controller.Entities.TV;
    using MediaBrowser.Controller.Library;

    /// <summary>
    /// Scores how similar two BaseItems are using metadata already indexed by
    /// Jellyfin's library (genres, tags, studios, people, year, rating).
    /// </summary>
    public class SimilarityEngine
    {
        private readonly bool useCastCrew;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimilarityEngine"/> class.
        /// </summary>
        /// <param name="useCastCrew">If true, include cast and crew (people) when scoring similarity.</param>
        public SimilarityEngine(bool useCastCrew)
        {
            this.useCastCrew = useCastCrew;
        }

        /// <summary>
        /// Calculates a similarity score between two items using shared metadata.
        /// </summary>
        /// <param name="source">The source item to compare from.</param>
        /// <param name="candidate">The candidate item to compare against.</param>
        /// <param name="libraryManager">The library manager used to resolve people metadata when enabled.</param>
        /// <returns>A score representing how similar the candidate is to the source.</returns>
        public double Score(BaseItem source, BaseItem candidate, ILibraryManager libraryManager)
        {
            if (source.Id == candidate.Id)
            {
                return 0;
            }

            // Different media type entirely (e.g. movie vs series) is a hard exclude — "more like this" should stay within the same kind.
            if (source.GetType() != candidate.GetType())
            {
                return 0;
            }

            double score = 0;

            score += 3.0 * JaccardOverlap(source.Genres, candidate.Genres);
            score += 2.0 * JaccardOverlap(source.Tags, candidate.Tags);
            score += 1.5 * JaccardOverlap(source.Studios, candidate.Studios);

            if (this.useCastCrew)
            {
                var sourcePeople = libraryManager.GetPeopleNames(new InternalPeopleQuery { ItemId = source.Id });
                var candidatePeople = libraryManager.GetPeopleNames(new InternalPeopleQuery { ItemId = candidate.Id });
                score += 2.5 * JaccardOverlap(sourcePeople, candidatePeople);
            }

            if (source.ProductionYear.HasValue && candidate.ProductionYear.HasValue)
            {
                var yearGap = Math.Abs(source.ProductionYear.Value - candidate.ProductionYear.Value);
                score += Math.Max(0, 1.0 - (yearGap / 20.0)); // full credit within same year, tapers over 20 yrs
            }

            if (source.CommunityRating.HasValue && candidate.CommunityRating.HasValue)
            {
                var ratingGap = Math.Abs(source.CommunityRating.Value - candidate.CommunityRating.Value);
                score += Math.Max(0, 0.5 - (ratingGap / 10.0 * 0.5));
            }

            return score;
        }

        private static double JaccardOverlap(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
        {
            if (a is null || b is null || a.Count == 0 || b.Count == 0)
            {
                return 0;
            }

            var setA = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
            var setB = new HashSet<string>(b, StringComparer.OrdinalIgnoreCase);

            var intersection = setA.Intersect(setB, StringComparer.OrdinalIgnoreCase).Count();
            if (intersection == 0)
            {
                return 0;
            }

            var union = setA.Union(setB, StringComparer.OrdinalIgnoreCase).Count();
            return (double)intersection / union;
        }
    }
}