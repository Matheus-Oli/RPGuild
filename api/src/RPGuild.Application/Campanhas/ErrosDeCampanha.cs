using RPGuild.Application.Comum;

namespace RPGuild.Application.Campanhas;

public static class ErrosDeCampanha
{
    public static readonly Erro OrdemInvalida = new(
        TipoErro.Validacao, "Ordem inválida", "A lista precisa ter exatamente as suas campanhas, sem repetir nem faltar.");

    public static readonly Erro SistemaIndisponivel = new(
        TipoErro.Validacao, "Sistema indisponível", "Escolha um dos sistemas disponíveis para campanha nova.");

    public static readonly Erro ImagemForaDaPasta = new(
        TipoErro.Validacao, "Imagem inválida", "Use uma imagem enviada por você (POST /api/uploads/sign, tipo capa ou banner).");
}
