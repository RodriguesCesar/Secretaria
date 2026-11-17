using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Secretaria.api.Tests.Integration;

public class MetricsEndpointTests
{
    [Fact]
    public async Task MetricsEndpoint_ShouldExposeSecurityCounters()
    {
        await using var factory = new MetricsApiFactory();
        using var client = factory.CreateClient();

        var authFailureResponse = await client.PostAsJsonAsync("/api/atendimento", CreateInvalidPayload());
        Assert.Equal(HttpStatusCode.Unauthorized, authFailureResponse.StatusCode);

        var metricsResponse = await client.GetAsync("/metrics");
        var metricsBody = await metricsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
        Assert.Contains("atendimento_auth_failures_total", metricsBody);
    }

    private static object CreateInvalidPayload() => new
    {
        nome = "Cesar",
        email = "cesar@example.com",
        telefone = "+5511999999999",
        mensagem = "abc"
    };

    private sealed class MetricsApiFactory : WebApplicationFactory<Program>
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
                    ["Authentication:AtendimentoApiKeys:0"] = "metrics-test-key",
                    ["Authentication:AllowAnonymousInDevelopment"] = "false"
                });
            });
        }
    }
}
