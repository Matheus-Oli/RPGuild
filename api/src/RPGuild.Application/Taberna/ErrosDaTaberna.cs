using RPGuild.Application.Comum;

namespace RPGuild.Application.Taberna;

public static class ErrosDaTaberna
{
    public static readonly Erro PedidoParaSiMesmo = new(
        TipoErro.Validacao, "Pedido inválido", "Você não pode enviar um pedido de amizade para si mesmo.");

    public static readonly Erro UsuarioNaoEncontrado = new(
        TipoErro.NaoEncontrado, "Usuário não encontrado", "Não encontramos ninguém com esse e-mail ou nick.");

    public static readonly Erro JaSaoAmigos = new(
        TipoErro.Conflito, "Já são amigos", "Vocês já são amigos.");

    public static readonly Erro PedidoJaEnviado = new(
        TipoErro.Conflito, "Pedido já enviado", "Você já enviou um pedido para essa pessoa. Aguarde a resposta.");

    public static readonly Erro PedidoRecebidoPendente = new(
        TipoErro.Conflito, "Pedido recebido", "Essa pessoa já te enviou um pedido. Aceite na sua lista.");

    public static readonly Erro PedidoNaoEncontrado = new(
        TipoErro.NaoEncontrado, "Pedido não encontrado", "Esse pedido de amizade não existe ou não é seu.");

    public static readonly Erro PedidoNaoPendente = new(
        TipoErro.Conflito, "Pedido já respondido", "Esse pedido de amizade não está mais pendente.");
}
