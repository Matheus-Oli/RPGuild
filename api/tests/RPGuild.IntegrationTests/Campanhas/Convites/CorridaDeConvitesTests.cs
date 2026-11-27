using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using RPGuild.Infrastructure.Persistence;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class CorridaDeConvitesTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Convidar_OutroConviteDaMesmaPessoaGravadoAoMesmoTempo_Retorna409SoComQuemColidiu()
    {
        // Arrange: enquanto este request grava, o outro já gravou o convite pendente de B.
        UsuarioLogado mestre = null!, b = null!;
        Guid campanhaId = default;
        RPGuildApiFactory factory = null!;
        factory = Factory(new ConvitePendenteDuplicadoNoBanco(async () => await factory.ConvidarNoBancoAsync(campanhaId, mestre, b)));
        await using var _ = factory;
        var (m, amigos) = await factory.CriarMestreComAmigosAsync(2);
        (mestre, b) = (m, amigos[1]);
        campanhaId = (await factory.CriarCampanhaAsync(mestre)).Id;

        // Act
        var resposta = await factory.ConvidarAsync(mestre, campanhaId, amigos[0].Id, b.Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.ConvidadosInvalidosAsync()).ShouldBe([new ConvidadoInvalidoResponse(b.Id, "ja_convidado")]);
        (await factory.ConvitesNoBancoAsync(campanhaId)).ShouldBe(1, "só o do outro request");
        (await factory.NotificacoesNoBancoAsync(amigos[0].Id)).ShouldNotContain(n => n.Tipo == "convite_recebido");
    }

    [Fact]
    public async Task Convidar_ViolacaoSemSaberDeQuem_Retorna409ComTodosDaListaComoJaConvidado()
    {
        // Arrange
        await using var factory = Factory(new ConvitePendenteDuplicadoNoBanco());
        var (mestre, amigos) = await factory.CriarMestreComAmigosAsync(1);
        var campanha = await factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await factory.ConvidarAsync(mestre, campanha.Id, amigos[0].Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.ConvidadosInvalidosAsync()).ShouldBe([new ConvidadoInvalidoResponse(amigos[0].Id, "ja_convidado")]);
        (await factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Aceitar_OutroAceiteGravadoAoMesmoTempo_Retorna409ENadaFicaPelaMetade()
    {
        // Arrange: o convite é criado sem o interceptador; só o aceite esbarra na PK de campaign_members.
        await using var factory = Factory(new JogadorDuplicadoNoBanco());
        var (mestre, amigos) = await factory.CriarMestreComAmigosAsync(1);
        var convidado = amigos[0];
        var propria = await factory.CriarCampanhaAsync(convidado);
        var campanha = await factory.CriarCampanhaAsync(mestre);
        var conviteId = await factory.ConvidarNoBancoAsync(campanha.Id, mestre, convidado);

        // Act
        var resposta = await factory.AceitarAsync(convidado, conviteId);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.Content.ReadFromJsonAsync<ProblemaResponse>())!.Detail.ShouldBe("Este convite não está mais pendente.");
        (await factory.StatusNoBancoAsync(conviteId)).ShouldBe("pendente");
        (await factory.LinhasDaCampanhaAsync(campanha.Id)).Membros.ShouldBe(0);
        (await factory.OrdemNoBancoAsync(convidado.Id)).ShouldBe([propria.Id]);
        (await factory.NotificacoesNoBancoAsync(mestre.Id)).ShouldNotContain(n => n.Tipo == "convite_aceito");
    }

    private RPGuildApiFactory Factory(SaveChangesInterceptor interceptador) => new(
        postgres,
        configurarServicos: services => services.ConfigureDbContext<AppDbContext>(options => options.AddInterceptors(interceptador)));
}
