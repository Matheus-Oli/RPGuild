namespace RPGuild.Application.Contas;

/// <param name="Token">Mesmo formato opaco do token de redefinição: id do usuário e token do Identity.</param>
public sealed record DadosDeTrocaDeEmail(string Nome, string NovoEmail, TokenOpaco Token);
