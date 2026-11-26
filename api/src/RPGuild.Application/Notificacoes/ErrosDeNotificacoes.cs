using RPGuild.Application.Comum;

namespace RPGuild.Application.Notificacoes;

public static class ErrosDeNotificacoes
{
    // Também para a notificação de outro usuário: não revela que ela existe.
    public static readonly Erro NotificacaoNaoEncontrada = new(
        TipoErro.NaoEncontrado, "Notificação não encontrada", "Essa notificação não existe.");
}
