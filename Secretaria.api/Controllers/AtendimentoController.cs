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
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> PostAsync([FromBody] AtendimentoRequest request, CancellationToken cancellationToken)
	{
		if (!ModelState.IsValid)
			return ValidationProblem(ModelState);

		var result = await _atendimentoServices.GerarAtendimentoAsync(request, cancellationToken);
		return Ok(result);
	}
}
