using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Attachments controller.
    /// </summary>
    [Route("Videos")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class VideoAttachmentsController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IAttachmentExtractor _attachmentExtractor;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoAttachmentsController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="attachmentExtractor">Instance of the <see cref="IAttachmentExtractor"/> interface.</param>
        public VideoAttachmentsController(
            ILibraryManager libraryManager,
            IAttachmentExtractor attachmentExtractor)
        {
            _libraryManager = libraryManager;
            _attachmentExtractor = attachmentExtractor;
        }

        /// <summary>
        /// Get video attachment.
        /// </summary>
        /// <param name="videoId">Video ID.</param>
        /// <param name="mediaSourceId">Media Source ID.</param>
        /// <param name="index">Attachment Index.</param>
        /// <response code="200">Attachment retrieved.</response>
        /// <response code="404">Video or attachment not found.</response>
        /// <returns>An <see cref="FileStreamResult"/> containing the attachment stream on success, or a <see cref="NotFoundResult"/> if the attachment could not be found.</returns>
        [HttpGet("{videoId}/{mediaSourceId}/Attachments/{index}")]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FileStreamResult>> GetAttachment(
            [FromRoute] Guid videoId,
            [FromRoute] string mediaSourceId,
            [FromRoute] int index)
        {
            try
            {
                var item = _libraryManager.GetItemById(videoId);
                if (item == null)
                {
                    return NotFound();
                }

                var (attachment, stream) = await _attachmentExtractor.GetAttachment(
                        item,
                        mediaSourceId,
                        index,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var contentType = string.IsNullOrWhiteSpace(attachment.MimeType)
                    ? MediaTypeNames.Application.Octet
                    : attachment.MimeType;

                return new FileStreamResult(stream, contentType);
            }
            catch (ResourceNotFoundException e)
            {
                return NotFound(e.Message);
            }
        }
    }
}
