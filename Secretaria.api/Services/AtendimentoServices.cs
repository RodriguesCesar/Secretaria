using Microsoft.Extensions.Options;
using Secretaria.api.Model.Request;
using Secretaria.api.Model.Settings;

namespace Secretaria.api.Services;

public class AtendimentoServices(
    IAtendimentoClient atendimentoClient,
    IOptions<AtendimentoResilienceSettings> resilienceOptions,
    ILogger<AtendimentoServices> logger)
{
    private readonly IAtendimentoClient _atendimentoClient = atendimentoClient;
    private readonly AtendimentoResilienceSettings _resilience = resilienceOptions.Value;
    private readonly ILogger<AtendimentoServices> _logger = logger;

    public async Task<string> GerarAtendimentoAsync(AtendimentoRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var prompt = $"Meu nome é {request.Nome}, tenho a seguinte pergunta: {request.Mensagem}";
        var timeout = TimeSpan.FromSeconds(_resilience.TimeoutSeconds);

        for (var attempt = 0; ; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                return await _atendimentoClient
                    .GerarRespostaAsync(prompt, timeoutCts.Token)
                    .WaitAsync(timeout, cancellationToken);
            }
            catch (TimeoutException) when (attempt < _resilience.MaxRetries)
            {
                _logger.LogWarning(
                    "Timeout ao gerar atendimento. Tentativa {Attempt} de {MaxAttempts}.",
                    attempt + 1,
                    _resilience.MaxRetries + 1);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < _resilience.MaxRetries)
            {
                _logger.LogWarning(
                    "Operação cancelada por timeout interno. Tentativa {Attempt} de {MaxAttempts}.",
                    attempt + 1,
                    _resilience.MaxRetries + 1);
            }
            catch (HttpRequestException) when (attempt < _resilience.MaxRetries)
            {
                _logger.LogWarning(
                    "Falha de rede ao gerar atendimento. Tentativa {Attempt} de {MaxAttempts}.",
                    attempt + 1,
                    _resilience.MaxRetries + 1);
            }

            await Task.Delay(_resilience.RetryDelayMilliseconds, cancellationToken);
        }
    }
}
