namespace Jellyfin.Plugin.MoreLikeThis.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using MediaBrowser.Common.Configuration;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// Stores precomputed "similar item" edges in a small local SQLite database
    /// under the plugin's own data folder (NOT Jellyfin's main library.db)
    /// Reads are O(log n) index lookups so the API controller can serve requests
    /// instantly instead of scoring on every page view.
    /// </summary>
    public class SimilarityStore
    {
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimilarityStore"/> class.
        /// </summary>
        /// <param name="appPaths">The application paths used to locate the plugin data directory.</param>
        public SimilarityStore(IApplicationPaths appPaths)
        {
            var dataDir = Path.Combine(appPaths.PluginsPath, "MoreLikeThis");
            Directory.CreateDirectory(dataDir);
            var dbPath = Path.Combine(dataDir, "similarity.db");
            this.connectionString = $"Data Source={dbPath}";

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Similarity (
                    SourceItemId   TEXT NOT NULL,
                    SimilarItemId  TEXT NOT NULL,
                    Score          REAL NOT NULL,
                    PRIMARY KEY (SourceItemId, SimilarItemId)
                );
                CREATE INDEX IF NOT EXISTS IX_Similarity_Source ON Similarity(SourceItemId, Score DESC);
            ";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Replaces all similarity results for a given source item.
        /// </summary>
        /// <param name="sourceItemId">The source item ID.</param>
        /// <param name="results">The similar results to store.</param>
        public void ReplaceForItem(Guid sourceItemId, IEnumerable<SimilarResult> results)
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();

            using (var del = connection.CreateCommand())
            {
                del.Transaction = tx;
                del.CommandText = "DELETE FROM Similarity WHERE SourceItemId = $src";
                del.Parameters.AddWithValue("$src", sourceItemId.ToString("N"));
                del.ExecuteNonQuery();
            }

            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = tx;
                insert.CommandText = "INSERT INTO Similarity (SourceItemId, SimilarItemId, Score) VALUES ($src, $sim, $score)";
                var pSrc = insert.Parameters.Add("$src", SqliteType.Text);
                var pSim = insert.Parameters.Add("$sim", SqliteType.Text);
                var pScore = insert.Parameters.Add("$score", SqliteType.Real);

                foreach (var r in results)
                {
                    pSrc.Value = sourceItemId.ToString("N");
                    pSim.Value = r.ItemId.ToString("N");
                    pScore.Value = r.Score;
                    insert.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }

        /// <summary>
        /// Gets the similar results for a given source item.
        /// </summary>
        /// <param name="sourceItemId">The source item ID.</param>
        /// <param name="limit">The maximum number of results to return.</param>
        /// <returns>A read-only list of similar results ordered by score descending.</returns>
        public IReadOnlyList<SimilarResult> GetForItem(Guid sourceItemId, int limit)
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT SimilarItemId, Score FROM Similarity
                WHERE SourceItemId = $src
                ORDER BY Score DESC
                LIMIT $limit";
            cmd.Parameters.AddWithValue("$src", sourceItemId.ToString("N"));
            cmd.Parameters.AddWithValue("$limit", limit);

            var results = new List<SimilarResult>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new SimilarResult(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetDouble(1)));
            }

            return results;
        }

        /// <summary>
        /// Removes all similarity entries for a given item.
        /// </summary>
        /// <param name="itemId">The item ID.</param>
        public void RemoveItem(Guid itemId)
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Similarity WHERE SourceItemId = $id OR SimilarItemId = $id";
            cmd.Parameters.AddWithValue("$id", itemId.ToString("N"));
            cmd.ExecuteNonQuery();
        }
    }
}
