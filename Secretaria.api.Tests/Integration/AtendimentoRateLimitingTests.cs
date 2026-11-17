using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Secretaria.api.Tests.Integration;

public class AtendimentoRateLimitingTests
{
    [Fact]
    public async Task Atendimento_ShouldReturnTooManyRequests_WhenLimitIsExceeded()
    {
        await using var factory = new RateLimitApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Atendimento-Api-Key", "rate-test-key");
        const int permitLimit = 20;

        for (var i = 0; i < permitLimit; i++)
        {
            var response = await client.PostAsJsonAsync("/api/atendimento", CreateInvalidPayload());
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        var limitedResponse = await client.PostAsJsonAsync("/api/atendimento", CreateInvalidPayload());

        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
    }

    private static object CreateInvalidPayload() => new
    {
        nome = "Cesar",
        email = "cesar@example.com",
        telefone = "+5511999999999",
        mensagem = "abc"
    };

    private sealed class RateLimitApiFactory : WebApplicationFactory<Program>
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
                    ["Authentication:AtendimentoApiKeys:0"] = "rate-test-key",
                    ["Authentication:AllowAnonymousInDevelopment"] = "false"
                });
            });
        }
    }
}
