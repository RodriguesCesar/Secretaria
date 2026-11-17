using System.ComponentModel.DataAnnotations;

namespace Secretaria.api.Model.Settings;

public class AtendimentoResilienceSettings
{
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 5)]
    public int MaxRetries { get; set; } = 2;

    [Range(50, 10000)]
    public int RetryDelayMilliseconds { get; set; } = 300;
}
