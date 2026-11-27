using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RPGuild.Application.Campanhas;
using RPGuild.IntegrationTests.Campanhas.Convites;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class FalhaAoEmpurrarTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Aceitar_HubFora_Retorna204EOJogadorEntra()
    {
        // Arrange
        var logs = new LogsCapturados();
        await using var factory = new RPGuildApiFactory(
            postgres,
            configurarServicos: services => services
                .AddSingleton<ILoggerProvider>(logs)
                .RemoveAll<ICampanhaTempoReal>()
                .AddSingleton<ICampanhaTempoReal, TempoRealQueFalha>());
        var (mestre, amigos) = await factory.CriarMestreComAmigosAsync(1);
        var convidado = amigos[0];
        var campanha = await factory.CriarCampanhaAsync(mestre);
        var conviteId = await factory.ConvidarNoBancoAsync(campanha.Id, mestre, convidado);

        // Act
        var resposta = await factory.AceitarAsync(convidado, conviteId);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await factory.StatusNoBancoAsync(conviteId)).ShouldBe("aceito");
        (await factory.LinhasDaCampanhaAsync(campanha.Id)).Membros.ShouldBe(1);

        var avisos = logs.Linhas.Where(l => l.StartsWith($"Warning {typeof(EventosDaCampanha).FullName}:")).ToList();
        avisos.ShouldNotBeEmpty();
        avisos.ShouldAllBe(l => !l.Contains(convidado.Dados.Nick) && !l.Contains(convidado.Dados.Email) && !l.Contains(convidado.Id.ToString()));
    }

    private sealed class TempoRealQueFalha : ICampanhaTempoReal
    {
        public Task MembroMudouAsync(MembroMudouEvento evento, CancellationToken cancellationToken) => Falhar();

        public Task CampanhaAtualizadaAsync(CampanhaAtualizadaEvento evento, CancellationToken cancellationToken) => Falhar();

        public Task CampanhaExcluidaAsync(CampanhaExcluidaEvento evento, CancellationToken cancellationToken) => Falhar();

        public Task ConvitesMudaramAsync(ConvitesMudaramEvento evento, CancellationToken cancellationToken) => Falhar();

        private static Task Falhar() => throw new InvalidOperationException("O hub caiu.");
    }
}
