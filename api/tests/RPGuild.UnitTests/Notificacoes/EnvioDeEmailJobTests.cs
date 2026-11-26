using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using RPGuild.Application.Notificacoes;
using RPGuild.Infrastructure.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

public class EnvioDeEmailJobTests
{
    [Fact]
    public async Task EnviarAsync_EnvioFalhando_LogaODestinatarioMascaradoEDeixaAExcecaoSubir()
    {
        // Arrange
        var mensagem = new EmailMessage("matmig17@gmail.com", "Boas-vindas à RPGuild", "Corpo com dados-do-corpo");
        var sender = Substitute.For<IEmailSender>();
        sender.EnviarAsync(mensagem, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("SMTP fora"));
        var logger = new LoggerEmMemoria<EnvioDeEmailJob>();

        // O job é criado pela DI, como o Hangfire faz ao executá-lo.
        using var servicos = new ServiceCollection()
            .AddSingleton(sender)
            .AddSingleton<ILogger<EnvioDeEmailJob>>(logger)
            .BuildServiceProvider();
        var job = ActivatorUtilities.CreateInstance<EnvioDeEmailJob>(servicos);

        // Act: a exceção sobe para o Hangfire tentar de novo.
        await Should.ThrowAsync<InvalidOperationException>(() => job.EnviarAsync(mensagem, CancellationToken.None));

        // Assert
        var log = logger.Mensagens.ShouldHaveSingleItem();
        log.ShouldContain("ma***@gmail.com");
        log.ShouldContain("Boas-vindas à RPGuild");
        log.ShouldNotContain("matmig17");
        log.ShouldNotContain("dados-do-corpo");
    }
}
