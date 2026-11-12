using System.ComponentModel.DataAnnotations;

namespace Secretaria.api.Model.Settings;

public class AgentSettings
{
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string ProjectEndpoint { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MinLength(2)]
    public string AgentName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AgentVersion { get; set; } = string.Empty;
}
