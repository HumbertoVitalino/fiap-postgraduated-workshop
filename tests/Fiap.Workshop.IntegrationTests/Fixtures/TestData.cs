namespace Fiap.Workshop.IntegrationTests.Fixtures;

internal static class TestData
{
    public static string ShortString(int length) =>
        Guid.NewGuid().ToString("N")[..length];

    public static string Email() => $"{Guid.NewGuid():N}@test.com";

    public static int UniqueNumber() => Random.Shared.Next(1, int.MaxValue);
}
