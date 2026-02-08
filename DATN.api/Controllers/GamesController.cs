using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using MyProject.Application.Models.Store;
using MyProject.Application.Features.Game.Queries;

namespace MyProject.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly ILogger<GamesController> _logger;
        private readonly IMediator _mediator;

        public GamesController(ILogger<GamesController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }


        [HttpGet("GetAll")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<GameBaseRespone>>> GetAllGame()
        {
            try
            {
                var query = new GetGamesPagingQuery();
                var result = await _mediator.Send(query);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllGame");
                return StatusCode(500, new { error = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<GameBaseRespone>> GetGameById([FromRoute] Guid id)
        {
            var query = new GetGameByIdQuery(id);
            var result = await _mediator.Send(query);
            if(result == null) {
                return NotFound(new { message = $"Game with id: {id} not found"});
            }
            return Ok(result);
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "API is working", timestamp = DateTime.UtcNow });
        }
    }
}
