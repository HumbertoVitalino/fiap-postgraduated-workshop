using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;

namespace Fiap.Workshop.UnitTests.Common;

public abstract class LoggerTestBase<TCategory>
{
    protected readonly Mock<ILogger<TCategory>> LoggerMock = new();

    [SuppressMessage("Performance", "CA1873", Justification = "Expressão de verificação do Moq: o ILogger.Log nunca é executado aqui, só inspecionado.")]
    protected void VerifyLog(LogLevel level, string message, Times times)
    {
        LoggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString() == message),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times
        );
    }
}
