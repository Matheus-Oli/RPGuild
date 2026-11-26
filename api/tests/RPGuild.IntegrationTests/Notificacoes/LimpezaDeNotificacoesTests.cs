using Microsoft.Extensions.DependencyInjection;
using RPGuild.Application.Notificacoes;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes;

[Collection(IntegrationCollection.Name)]
public sealed class LimpezaDeNotificacoesTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Executar_NotificacoesAntigasENovas_ApagaSoAsDeMaisDe90Dias()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var agora = DateTimeOffset.UtcNow;
        var de91Dias = await _factory.CriarNotificacaoAsync(logado.Id, agora.AddDays(-91));
        var deUmAno = await _factory.CriarNotificacaoAsync(logado.Id, agora.AddDays(-365), lida: true);
        var quase90Dias = await _factory.CriarNotificacaoAsync(logado.Id, agora.AddDays(-90).AddMinutes(1));
        var deOntem = await _factory.CriarNotificacaoAsync(logado.Id, agora.AddDays(-1));

        // Act
        using (var escopo = _factory.Services.CreateScope())
        {
            await escopo.ServiceProvider.GetRequiredService<LimpezaDeNotificacoes>().ExecutarAsync(CancellationToken.None);
        }

        // Assert
        var restantes = (await _factory.NotificacoesNoBancoAsync(logado.Id)).Select(n => n.Id).ToList();
        restantes.ShouldNotContain(de91Dias);
        restantes.ShouldNotContain(deUmAno);
        restantes.ShouldContain(quase90Dias);
        restantes.ShouldContain(deOntem);
        restantes.Count.ShouldBe(3);
    }
}
