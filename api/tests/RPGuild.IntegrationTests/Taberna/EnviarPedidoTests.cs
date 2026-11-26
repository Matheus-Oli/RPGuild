using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class EnviarPedidoTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("email")]
    [InlineData("email-em-outra-caixa")]
    [InlineData("nick")]
    [InlineData("@nick")]
    [InlineData("nick-em-outra-caixa")]
    [InlineData("@nick-em-outra-caixa")]
    public async Task Enviar_EmailOuNick_Retorna201EApareceNosDoisLados(string forma)
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        var emailOuNick = forma switch
        {
            "email" => b.Dados.Email,
            "email-em-outra-caixa" => b.Dados.Email.ToUpperInvariant(),
            "nick" => b.Dados.Nick,
            "@nick" => $"@{b.Dados.Nick}",
            "nick-em-outra-caixa" => b.Dados.Nick.ToUpperInvariant(),
            _ => $"@{b.Dados.Nick.ToUpperInvariant()}",
        };

        // Act
        var resposta = await _factory.PedirAsync(a, emailOuNick);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var pedido = (await resposta.Content.ReadFromJsonAsync<PedidoEnviadoResponse>())!;
        pedido.Id.ShouldNotBe(Guid.Empty);
        pedido.Para.ShouldBe(new PessoaResponse(b.Id, b.Dados.Nome, b.Dados.Nick, null));

        var enviado = (await _factory.PedidosDeAsync(a)).Enviados.ShouldHaveSingleItem();
        enviado.Id.ShouldBe(pedido.Id);
        enviado.Para.UserId.ShouldBe(b.Id);

        var recebido = (await _factory.PedidosDeAsync(b)).Recebidos.ShouldHaveSingleItem();
        recebido.Id.ShouldBe(pedido.Id);
        recebido.De.ShouldBe(new PessoaResponse(a.Id, a.Dados.Nome, a.Dados.Nick, null));
    }

    private async Task<(UsuarioLogado A, UsuarioLogado B)> DoisUsuariosAsync()
    {
        var usuarios = await _factory.CriarUsuariosLogadosAsync(2);
        return (usuarios[0], usuarios[1]);
    }
}
