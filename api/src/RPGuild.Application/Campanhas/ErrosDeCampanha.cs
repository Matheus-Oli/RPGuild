using RPGuild.Application.Comum;

namespace RPGuild.Application.Campanhas;

public static class ErrosDeCampanha
{
    public static readonly Erro OrdemInvalida = new(
        TipoErro.Validacao, "Ordem inválida", "A lista precisa ter exatamente as suas campanhas, sem repetir nem faltar.");
}
