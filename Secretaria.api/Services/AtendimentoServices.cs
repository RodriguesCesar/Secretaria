using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using Secretaria.api.Model.Request;
using Secretaria.api.Model.Settings;

namespace Secretaria.api.Services;

public class AtendimentoServices
{
    private readonly AgentSettings _agentSettings;
    private readonly DefaultAzureCredential _credential = new();

    public AtendimentoServices(IOptions<AgentSettings> agentOptions)
    {
        _agentSettings = agentOptions.Value;
    }

    public Task<string> GerarAtendimentoAsync(AtendimentoRequest request, CancellationToken cancellationToken)
    {
        #pragma warning disable OPENAI001

        cancellationToken.ThrowIfCancellationRequested();

        AIProjectClient projectClient = new(endpoint: new Uri(_agentSettings.ProjectEndpoint), tokenProvider: _credential);
        AgentReference agentReference = new(name: _agentSettings.AgentName, version: _agentSettings.AgentVersion);
        ProjectResponsesClient responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentReference);

        ResponseResult response = responseClient.CreateResponse(
           $"Meu nome é {request.Nome}, tenho a seguinte pergunta: {request.Mensagem}"
        );

        return Task.FromResult(response.GetOutputText());
    }
}
