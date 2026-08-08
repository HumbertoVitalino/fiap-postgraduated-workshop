using Microsoft.Extensions.Logging;
using Moq;

namespace Fiap.Workshop.UnitTests.Common;

public abstract class LoggerTestBase<TCategory>
{
    protected readonly Mock<ILogger<TCategory>> LoggerMock = new();

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
