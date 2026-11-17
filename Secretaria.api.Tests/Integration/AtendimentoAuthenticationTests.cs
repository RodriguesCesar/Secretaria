using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Secretaria.api.Tests.Integration;

public class AtendimentoAuthenticationTests
{
    [Fact]
    public async Task Atendimento_ShouldReturnUnauthorized_WhenApiKeyHeaderIsMissing()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/atendimento", CreateInvalidPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Atendimento_ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Atendimento-Api-Key", "invalid-key");

        var response = await client.PostAsJsonAsync("/api/atendimento", CreateInvalidPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Atendimento_ShouldProceed_WhenApiKeyIsValid()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Atendimento-Api-Key", "active-api-key");

        var response = await client.PostAsJsonAsync("/api/atendimento", CreateInvalidPayload());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Atendimento_ShouldReturnServerError_WhenNoApiKeyConfiguredOutsideDevelopment()
    {
        await using var factory = new MissingKeyApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/atendimento", CreateInvalidPayload());
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Configuração inválida", problem.Title);
    }

    private static object CreateInvalidPayload() => new
    {
        nome = "Cesar",
        email = "cesar@example.com",
        telefone = "+5511999999999",
        mensagem = "abc"
    };

    private sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Agent:ProjectEndpoint"] = "https://example.org/projects/secretaria",
                    ["Agent:AgentName"] = "SecretariaTestAgent",
                    ["Agent:AgentVersion"] = "1",
                    ["AtendimentoResilience:TimeoutSeconds"] = "5",
                    ["AtendimentoResilience:MaxRetries"] = "1",
                    ["AtendimentoResilience:RetryDelayMilliseconds"] = "50",
                    ["RateLimiting:Atendimento:PermitLimit"] = "20",
                    ["RateLimiting:Atendimento:WindowSeconds"] = "60",
                    ["RateLimiting:Atendimento:QueueLimit"] = "0",
                    ["Authentication:AtendimentoApiKeys:0"] = "active-api-key",
                    ["Authentication:AtendimentoApiKeys:1"] = "transition-api-key",
                    ["Authentication:AllowAnonymousInDevelopment"] = "false"
                });
            });
        }
    }

    private sealed class MissingKeyApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Agent:ProjectEndpoint"] = "https://example.org/projects/secretaria",
                    ["Agent:AgentName"] = "SecretariaTestAgent",
                    ["Agent:AgentVersion"] = "1",
                    ["AtendimentoResilience:TimeoutSeconds"] = "5",
                    ["AtendimentoResilience:MaxRetries"] = "1",
                    ["AtendimentoResilience:RetryDelayMilliseconds"] = "50",
                    ["RateLimiting:Atendimento:PermitLimit"] = "20",
                    ["RateLimiting:Atendimento:WindowSeconds"] = "60",
                    ["RateLimiting:Atendimento:QueueLimit"] = "0",
                    ["Authentication:AllowAnonymousInDevelopment"] = "false"
                });
            });
        }
    }
}
