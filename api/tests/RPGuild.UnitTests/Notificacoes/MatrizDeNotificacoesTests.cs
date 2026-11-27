using RPGuild.Application.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

public class MatrizDeNotificacoesTests
{
    [Theory]
    [InlineData(TiposDeNotificacao.BoasVindas, true, EnvioPorEmail.Fila)]
    [InlineData(TiposDeNotificacao.RedefinicaoDeSenha, false, EnvioPorEmail.Direto)]
    [InlineData(TiposDeNotificacao.SenhaAlterada, false, EnvioPorEmail.Fila)]
    [InlineData(TiposDeNotificacao.EmailAlterado, false, EnvioPorEmail.Fila)]
    [InlineData(TiposDeNotificacao.ConfirmacaoDeEmail, false, EnvioPorEmail.Direto)]
    [InlineData(TiposDeNotificacao.AmizadeRecebida, true, EnvioPorEmail.Fila)]
    [InlineData(TiposDeNotificacao.AmizadeAceita, true, EnvioPorEmail.Nenhum)]
    [InlineData(TiposDeNotificacao.AmizadeRecusada, true, EnvioPorEmail.Nenhum)]
    [InlineData(TiposDeNotificacao.CampanhaEditada, true, EnvioPorEmail.Nenhum)]
    public void CanaisDe_TipoConhecido_TemOsCanaisDaMatriz(string tipo, bool sininho, EnvioPorEmail email)
    {
        // Act
        var canais = MatrizDeNotificacoes.CanaisDe(tipo);

        // Assert
        canais.ShouldBe(new CanaisDaNotificacao(sininho, email));
    }

    [Fact]
    public void Tipos_ValoresGravados_SaoOsTextosDaMatriz()
    {
        // Assert: é o texto que vai para notifications.tipo e que o front lê.
        TiposDeNotificacao.BoasVindas.ShouldBe("boas_vindas");
        TiposDeNotificacao.RedefinicaoDeSenha.ShouldBe("redefinicao_senha");
        TiposDeNotificacao.SenhaAlterada.ShouldBe("senha_alterada");
        TiposDeNotificacao.EmailAlterado.ShouldBe("email_alterado");
        TiposDeNotificacao.ConfirmacaoDeEmail.ShouldBe("confirmacao_email");
        TiposDeNotificacao.AmizadeRecebida.ShouldBe("amizade_recebida");
        TiposDeNotificacao.AmizadeAceita.ShouldBe("amizade_aceita");
        TiposDeNotificacao.AmizadeRecusada.ShouldBe("amizade_recusada");
        TiposDeNotificacao.CampanhaEditada.ShouldBe("campanha_editada");
    }

    [Theory]
    [InlineData("tipo_que_nao_existe")]
    [InlineData("")]
    [InlineData("BOAS_VINDAS")]
    public void CanaisDe_TipoDesconhecido_LancaArgumentException(string tipo)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => MatrizDeNotificacoes.CanaisDe(tipo));
    }
}
