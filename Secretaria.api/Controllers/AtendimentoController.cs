using Microsoft.AspNetCore.Mvc;
using Secretaria.api.Model.Request;
using Secretaria.api.Services;

namespace Secretaria.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AtendimentoController : ControllerBase
{
	private readonly AtendimentoServices _atendimentoServices;

	public AtendimentoController(AtendimentoServices atendimentoServices)
	{
		_atendimentoServices = atendimentoServices;
	}

	[HttpPost]
	public async Task<IActionResult> PostAsync([FromBody] AtendimentoRequest request)
	{
		var result = await _atendimentoServices.GerarAtendimentoAsync(request);
		return Ok(result);
	}
}

