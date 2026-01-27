using FunkoApi.Configuration;
using FunkoApi.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Unitarios.Services;

[TestFixture]
public class EmailServiceTests
{
    private Mock<IOptions<EmailSettings>> _optionsMock;
    private Mock<ILogger<EmailService>> _loggerMock;
    private EmailService _service;
    private EmailSettings _settings;

    [SetUp]
    public void SetUp()
    {
        _settings = new EmailSettings
        {
            FromEmail = "test@test.com",
            FromName = "Test",
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            SmtpUser = "user",
            SmtpPass = "pass"
        };

        _optionsMock = new Mock<IOptions<EmailSettings>>();
        _optionsMock.Setup(o => o.Value).Returns(_settings);

        _loggerMock = new Mock<ILogger<EmailService>>();

        _service = new EmailService(_optionsMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task SendEmailAsync_CuandoFallaConexion_RegistraError()
    {
        // ARRANGE
        var to = "dest@test.com";
        var subject = "Test Subject";
        var htmlMessage = "<h1>Test</h1>";

        // Este test va a fallar la conexión SMTP porque no hay servidor real
        // pero cubrirá el catch block

        // ACT
        await _service.SendEmailAsync(to, subject, htmlMessage);

        // ASSERT
        // Verificar que se registró un error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error enviando email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
