namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Job diário (Hangfire): apaga as notificações criadas há mais de 90 dias, lidas ou não.
/// </summary>
public sealed class LimpezaDeNotificacoes(INotificacaoRepository notificacoes, TimeProvider relogio)
{
    public const string IdDoJob = "limpeza-de-notificacoes";

    public static readonly TimeSpan Retencao = TimeSpan.FromDays(90);

    public Task ExecutarAsync(CancellationToken cancellationToken) =>
        notificacoes.ApagarCriadasAntesDeAsync(relogio.GetUtcNow() - Retencao, cancellationToken);
}
