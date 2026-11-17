using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Secretaria.api.Tests.Integration;

public class HealthEndpointTests
{
    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Agent:ProjectEndpoint"] = "https://example.org/projects/secretaria",
                    ["Agent:AgentName"] = "SecretariaTestAgent",
                    ["Agent:AgentVersion"] = "1",
                    ["AtendimentoResilience:TimeoutSeconds"] = "30",
                    ["AtendimentoResilience:MaxRetries"] = "1",
                    ["AtendimentoResilience:RetryDelayMilliseconds"] = "100"
                });
            });
        }
    }
}
