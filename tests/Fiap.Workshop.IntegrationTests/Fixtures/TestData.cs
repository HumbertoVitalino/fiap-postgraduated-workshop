namespace Fiap.Workshop.IntegrationTests.Fixtures;

internal static class TestData
{
    public static string ShortString(int length) =>
        Guid.NewGuid().ToString("N")[..length];

    public static string Email() => $"{Guid.NewGuid():N}@test.com";

    public static string LicensePlate()
    {
        var letters = new string([.. Enumerable.Range(0, 3).Select(_ => (char)Random.Shared.Next('A', 'Z' + 1))]);
        var digits = Random.Shared.Next(0, 10000).ToString("D4");
        return $"{letters}{digits}";
    }

    public static string Document()
    {
        var digits = new int[9];
        for (var i = 0; i < digits.Length; i++)
            digits[i] = Random.Shared.Next(0, 9);

        var withFirstCheck = digits.Append(CalculateCheckDigit(digits)).ToArray();
        var withSecondCheck = withFirstCheck.Append(CalculateCheckDigit(withFirstCheck)).ToArray();

        return string.Concat(withSecondCheck);
    }

    private static int CalculateCheckDigit(int[] digits)
    {
        var length = digits.Length;
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += digits[i] * (length + 1 - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
