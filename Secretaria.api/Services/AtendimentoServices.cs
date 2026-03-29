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

    public AtendimentoServices(IOptions<AgentSettings> agentOptions)
    {
        // Keep a local copy of the loaded AgentSettings values
        _agentSettings = agentOptions.Value;
    }

    public async Task<string> GerarAtendimentoAsync(AtendimentoRequest request)
    {
        #pragma warning disable OPENAI001

        AIProjectClient projectClient = new(endpoint: new Uri(_agentSettings.ProjectEndpoint), tokenProvider: new DefaultAzureCredential());

        AgentReference agentReference = new(name: _agentSettings.AgentName, version: _agentSettings.AgentVersion);
        ProjectResponsesClient responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentReference);

        ResponseResult response = responseClient.CreateResponse(
           $"Meu nome é {request.Nome}, tenho a seguinte pergunta: {request.Mensagem}"
        );

        return response.GetOutputText();
    }
}
