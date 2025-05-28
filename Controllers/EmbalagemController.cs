using LojaManoel.Models;
using LojaManoel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaManoel.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmbalagemController : ControllerBase
    {
        private readonly IEmbalagemService _embalagemService;

        public EmbalagemController(IEmbalagemService embalagemService)
        {
            _embalagemService = embalagemService;
        }

        [HttpPost]
        public async Task<ActionResult<EmbalagemOutput>> Post([FromBody] EmbalagemInput input)
        {
            try
            {
                var result = await _embalagemService.ProcessarEmbalagem(input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao processar a embalagem: {ex.Message}");
            }
        }
    }
}
