using System.ComponentModel.DataAnnotations;

namespace Secretaria.api.Model.Request;

public record AtendimentoRequest(
    [Required(AllowEmptyStrings = false)]
    [MinLength(2)]
    [MaxLength(120)]
    string Nome,
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [MaxLength(180)]
    string Email,
    [Required(AllowEmptyStrings = false)]
    [Phone]
    [MaxLength(30)]
    string Telefone,
    [Required(AllowEmptyStrings = false)]
    [MinLength(5)]
    [MaxLength(4000)]
    string Mensagem
);