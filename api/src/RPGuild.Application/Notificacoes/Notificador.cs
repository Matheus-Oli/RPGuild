using System.Text.Json;
using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Domain.Notificacoes;

namespace RPGuild.Application.Notificacoes;

public interface INotificador
{
    Task NotificarAsync(Guid userId, Aviso aviso, CancellationToken cancellationToken);

    /// <param name="emailDestino">
    /// Para onde vai o e-mail, quando não é o e-mail atual do usuário: o antigo (nº 4) ou o novo (confirmação).
    /// </param>
    Task NotificarAsync(Guid userId, Aviso aviso, string emailDestino, CancellationToken cancellationToken);
}

/// <summary>
/// Serviço central de notificações: decide os canais pela <see cref="MatrizDeNotificacoes"/>, grava o sininho e manda
/// o e-mail (pela fila ou, nos que levam token, direto).
/// </summary>
public sealed class Notificador(
    INotificacaoRepository notificacoes,
    IFilaDeEmail fila,
    EnvioDeEmail envioDireto,
    IIdentityService identity,
    EventosDoSininho eventos,
    FrontOptions front,
    TimeProvider relogio) : INotificador
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task NotificarAsync(Guid userId, Aviso aviso, CancellationToken cancellationToken) =>
        ProcessarAsync(userId, aviso, emailDestino: null, cancellationToken);

    public Task NotificarAsync(Guid userId, Aviso aviso, string emailDestino, CancellationToken cancellationToken) =>
        ProcessarAsync(userId, aviso, emailDestino, cancellationToken);

    private async Task ProcessarAsync(Guid userId, Aviso aviso, string? emailDestino, CancellationToken cancellationToken)
    {
        var canais = MatrizDeNotificacoes.CanaisDe(aviso.Tipo);

        if (canais.Sininho)
        {
            // Serializa pelo tipo concreto: os dados são as propriedades do aviso, sem o tipo.
            var dados = JsonSerializer.Serialize(aviso, aviso.GetType(), Json);
            var notificacao = Notificacao.Criar(userId, aviso.Tipo, dados, relogio.GetUtcNow());
            await notificacoes.AdicionarAsync(notificacao, cancellationToken);
            await notificacoes.SalvarAlteracoesAsync(cancellationToken);
            await eventos.CriadaAsync(userId, notificacao, cancellationToken);
        }

        if (canais.Email == EnvioPorEmail.Nenhum)
        {
            return;
        }

        var usuario = await identity.BuscarPerfilAsync(userId, cancellationToken);
        if (usuario is null)
        {
            return;
        }

        var mensagem = Email(aviso, usuario.Nome, emailDestino ?? usuario.Email);
        if (canais.Email == EnvioPorEmail.Direto)
        {
            await envioDireto.EnviarAsync(mensagem, cancellationToken);
        }
        else
        {
            fila.Enfileirar(mensagem);
        }
    }

    private EmailMessage Email(Aviso aviso, string nome, string para) => aviso switch
    {
        BoasVindas => EmailTemplates.BoasVindas(nome, para),
        RedefinicaoDeSenha redefinicao => EmailTemplates.RedefinicaoDeSenha(nome, para, redefinicao.Link),
        SenhaAlterada => EmailTemplates.SenhaAlterada(nome, para, front.BaseUrl),
        EmailAlterado alterado => EmailTemplates.EmailAlterado(nome, para, alterado.EmailNovo),
        ConfirmacaoDeEmail confirmacao => EmailTemplates.ConfirmacaoDeNovoEmail(nome, para, confirmacao.Link),
        AmizadeRecebida pedido => EmailTemplates.PedidoDeAmizade(nome, para, pedido.DeNome, pedido.DeNick, Link("taberna")),
        ConviteRecebido convite => EmailTemplates.ConviteDeCampanha(
            nome, para, convite.CampanhaNome, convite.MestreNome, convite.MestreNick, Link($"convites/{convite.ConviteId}")),
        JogadorEntrou entrou => EmailTemplates.JogadorEntrou(
            nome, para, entrou.CampanhaNome, entrou.Nome, entrou.Nick, Link($"campanhas/{entrou.CampanhaId}")),
        CampanhaExcluida excluida => EmailTemplates.CampanhaExcluida(nome, para, excluida.CampanhaNome, excluida.MestreNome),
        _ => throw new InvalidOperationException($"O tipo \"{aviso.Tipo}\" tem e-mail na matriz, mas não tem modelo de e-mail."),
    };

    private string Link(string rota) => $"{front.BaseUrl.TrimEnd('/')}/{rota}";
}
