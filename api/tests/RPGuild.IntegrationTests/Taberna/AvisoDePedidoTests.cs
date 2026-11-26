using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class AvisoDePedidoTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Enviar_PedidoCriado_MandaOAvisoParaODestinatarioComNomeArrobaNickELink()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(2);
        var (a, b) = (usuarios[0], usuarios[1]);

        // Act
        await _factory.PedirComSucessoAsync(a, b);

        // Assert
        var aviso = _factory.Emails.UltimaPara(b.Dados.Email);
        aviso.ShouldNotBeNull();
        aviso.Assunto.ShouldBe($"{a.Dados.Nome} te enviou um pedido de amizade na RPGuild");
        aviso.Texto.ShouldContain($"{a.Dados.Nome} (@{a.Dados.Nick})");
        aviso.Texto.ShouldContain($"{RPGuildApiFactory.FrontBaseUrl}/taberna");
        aviso.Texto.ShouldNotContain(a.Dados.Email, Case.Insensitive);
    }

    [Fact]
    public async Task Enviar_EnvioDoAvisoFalhando_Retorna201MesmoAssim()
    {
        // Arrange
        await using var factory = new RPGuildApiFactory(postgres, emailSender: new EmailSenderQueFalha());
        var usuarios = await factory.CriarUsuariosLogadosAsync(2);

        // Act
        var resposta = await factory.PedirAsync(usuarios[0], usuarios[1].Dados.Nick);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
