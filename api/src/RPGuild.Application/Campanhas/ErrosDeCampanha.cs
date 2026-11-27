using RPGuild.Application.Comum;

namespace RPGuild.Application.Campanhas;

public static class ErrosDeCampanha
{
    public static readonly Erro OrdemInvalida = new(
        TipoErro.Validacao, "Ordem inválida", "A lista precisa ter exatamente as suas campanhas, sem repetir nem faltar.");

    public static readonly Erro CampanhaNaoEncontrada = new(
        TipoErro.NaoEncontrado, "Campanha não encontrada", "A campanha não existe ou você não participa dela.");

    public static readonly Erro SoOMestre = new(
        TipoErro.Proibido, "Só o mestre", "Só o mestre da campanha pode fazer isso.");

    public static readonly Erro ConfirmacaoIncorreta = new(
        TipoErro.Validacao,
        "Confirmação incorreta",
        $"Digite o nome da campanha e a frase \"{ConfirmacaoDeExclusao.Frase}\" exatamente como aparecem.");

    public static readonly Erro SistemaIndisponivel = new(
        TipoErro.Validacao, "Sistema indisponível", "Escolha um dos sistemas disponíveis para campanha nova.");

    public static readonly Erro ImagemForaDaPasta = new(
        TipoErro.Validacao, "Imagem inválida", "Use uma imagem enviada por você (POST /api/uploads/sign, tipo capa ou banner).");
}
