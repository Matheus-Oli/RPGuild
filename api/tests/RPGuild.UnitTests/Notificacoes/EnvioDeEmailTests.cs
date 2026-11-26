using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RPGuild.Application.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

public class EnvioDeEmailTests
{
    [Fact]
    public async Task EnviarAsync_EnvioFalhando_LogaODestinatarioMascaradoSemQuebrar()
    {
        // Arrange
        var mensagem = new EmailMessage("matmig17@gmail.com", "Assunto", "Corpo com token-secreto");
        var sender = Substitute.For<IEmailSender>();
        sender.EnviarAsync(mensagem, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("SMTP fora"));
        var logger = new LoggerEmMemoria<EnvioDeEmail>();

        // Act
        await new EnvioDeEmail(sender, logger).EnviarAsync(mensagem, CancellationToken.None);

        // Assert
        var log = logger.Mensagens.ShouldHaveSingleItem();
        log.ShouldContain("ma***@gmail.com");
        log.ShouldNotContain("matmig17");
        log.ShouldNotContain("token-secreto");
    }
}
