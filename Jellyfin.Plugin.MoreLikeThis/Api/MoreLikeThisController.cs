namespace Jellyfin.Plugin.MoreLikeThis.Api
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Jellyfin.Plugin.MoreLikeThis.Services;
    using MediaBrowser.Controller.Dto;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Model.Dto;
    using MediaBrowser.Model.Querying;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Controller that exposes endpoints for retrieving "more like this" items
    /// based on precomputed similarity data.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("MoreLikeThis")]
    public class MoreLikeThisController : ControllerBase
    {
        private readonly SimilarityStore store;
        private readonly ILibraryManager libraryManager;
        private readonly IDtoService dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoreLikeThisController"/> class.
        /// </summary>
        /// <param name="store">The similarity store containing precomputed edges.</param>
        /// <param name="libraryManager">Library manager for retrieving items.</param>
        /// <param name="dtoService">DTO service for converting items to DTOs.</param>
        public MoreLikeThisController(SimilarityStore store, ILibraryManager libraryManager, IDtoService dtoService)
        {
            this.store = store;
            this.libraryManager = libraryManager;
            this.dtoService = dtoService;
        }

        /// <summary>
        /// GET /MoreLikeThis/{itemId}
        /// Returns the precomputed similar-items shelf for a movie or show.
        /// This is a pure lookup against the local similarity.db .
        /// </summary>
        /// <returns>A query result containing matching BaseItemDto instances.</returns>
        [HttpGet("{itemId}")]
        public ActionResult<QueryResult<BaseItemDto>> GetMoreLikeThis(
            [FromRoute] Guid itemId,
            [FromQuery] int limit = 16)
        {
            var edges = this.store.GetForItem(itemId, limit);
            if (edges.Count == 0)
            {
                return this.Ok(new QueryResult<BaseItemDto> { Items = Array.Empty<BaseItemDto>(), TotalRecordCount = 0 });
            }

            var options = new DtoOptions(true);
            var dtos = edges
                .Select(e => this.libraryManager.GetItemById(e.ItemId))
                .Where(i => i is not null)
                .Select(i => this.dtoService.GetBaseItemDto(i!, options))
                .ToArray();

            return this.Ok(new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = dtos.Length,
            });
        }
    }
}
