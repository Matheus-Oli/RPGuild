using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes;

[Collection(IntegrationCollection.Name)]
public sealed class BoasVindasTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_ContaNova_CriaBoasVindasNoSininhoEMandaOEmailPelaFila()
    {
        // Act
        var (dados, criado) = await _factory.RegistrarUsuarioAsync();

        // Assert
        var notificacao = (await _factory.NotificacoesNoBancoAsync(criado.Id)).ShouldHaveSingleItem();
        notificacao.Tipo.ShouldBe("boas_vindas");
        notificacao.Dados.ShouldBe("{}");
        notificacao.Lida.ShouldBeFalse();

        _factory.Fila.PassouPelaFila(dados.Email).ShouldBeTrue();
        _factory.Emails.UltimaPara(dados.Email)!.Assunto.ShouldBe("Boas-vindas à RPGuild");
    }
}
