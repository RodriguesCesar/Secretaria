namespace Secretaria.api.Services;

public interface IAtendimentoClient
{
    Task<string> GerarRespostaAsync(string prompt, CancellationToken cancellationToken);
}
