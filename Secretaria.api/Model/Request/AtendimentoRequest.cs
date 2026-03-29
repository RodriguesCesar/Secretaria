namespace Secretaria.api.Model.Request;

public record AtendimentoRequest(
    string Nome, //Nome da pessoa
    string Email, //Email da pessoa
    string Telefone, //Telefone da pessoa
    string Mensagem //Mensagem da pessoa
);