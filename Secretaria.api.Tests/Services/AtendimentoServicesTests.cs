using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Secretaria.api.Model.Request;
using Secretaria.api.Model.Settings;
using Secretaria.api.Services;
using Xunit;

namespace Secretaria.api.Tests.Services;

public class AtendimentoServicesTests
{
    [Fact]
    public async Task GerarAtendimentoAsync_ShouldRetryAndSucceed_WhenTransientTimeoutOccurs()
    {
        var fakeClient = new FakeAtendimentoClient([new TimeoutException(), "Resposta final"]);
        var options = Options.Create(new AtendimentoResilienceSettings
        {
            TimeoutSeconds = 5,
            MaxRetries = 2,
            RetryDelayMilliseconds = 1
        });
        var service = new AtendimentoServices(fakeClient, options, NullLogger<AtendimentoServices>.Instance);

        var result = await service.GerarAtendimentoAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("Resposta final", result);
        Assert.Equal(2, fakeClient.Attempts);
    }

    [Fact]
    public async Task GerarAtendimentoAsync_ShouldThrowAfterRetries_WhenTimeoutPersists()
    {
        var fakeClient = new FakeAtendimentoClient([new TimeoutException(), new TimeoutException()]);
        var options = Options.Create(new AtendimentoResilienceSettings
        {
            TimeoutSeconds = 5,
            MaxRetries = 1,
            RetryDelayMilliseconds = 1
        });
        var service = new AtendimentoServices(fakeClient, options, NullLogger<AtendimentoServices>.Instance);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            service.GerarAtendimentoAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(2, fakeClient.Attempts);
    }

    private static AtendimentoRequest CreateRequest() =>
        new("Cesar", "cesar@example.com", "+5511999999999", "Preciso de ajuda para matrícula.");

    private sealed class FakeAtendimentoClient(object[] outcomes) : IAtendimentoClient
    {
        private readonly Queue<object> _outcomes = new(outcomes);
        public int Attempts { get; private set; }

        public Task<string> GerarRespostaAsync(string prompt, CancellationToken cancellationToken)
        {
            Attempts++;
            var outcome = _outcomes.Dequeue();
            return outcome switch
            {
                Exception ex => Task.FromException<string>(ex),
                string response => Task.FromResult(response),
                _ => throw new InvalidOperationException("Resultado inválido no teste.")
            };
        }
    }
}
