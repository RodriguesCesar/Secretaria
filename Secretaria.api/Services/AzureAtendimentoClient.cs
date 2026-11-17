using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using Secretaria.api.Model.Settings;

namespace Secretaria.api.Services;

public class AzureAtendimentoClient(IOptions<AgentSettings> options) : IAtendimentoClient
{
    private readonly AgentSettings _settings = options.Value;
    private readonly DefaultAzureCredential _credential = new();

    public Task<string> GerarRespostaAsync(string prompt, CancellationToken cancellationToken)
    {
#pragma warning disable OPENAI001
        cancellationToken.ThrowIfCancellationRequested();

        AIProjectClient projectClient = new(endpoint: new Uri(_settings.ProjectEndpoint), tokenProvider: _credential);
        AgentReference agentReference = new(name: _settings.AgentName, version: _settings.AgentVersion);
        ProjectResponsesClient responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentReference);

        ResponseResult response = responseClient.CreateResponse(prompt);
        return Task.FromResult(response.GetOutputText());
    }
}
