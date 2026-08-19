using CpfCnpjLibrary;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Fiap.Workshop.Application.Commons;

[ExcludeFromCodeCoverage]
public static partial class StringExtensions
{
    private const int CPF_SIZE = 11;
    private const int CNPJ_SIZE = 14;

    [GeneratedRegex("^[A-Z]{3}[0-9]([A-Z][0-9]{2}|[0-9]{3})$")]
    private static partial Regex LicensePlatePattern();

    public static string NormalizeLicensePlate(this string licensePlate) =>
        licensePlate.Trim().ToUpperInvariant().Replace("-", "");

    public static bool IsValidLicensePlate(this string licensePlate) =>
        LicensePlatePattern().IsMatch(licensePlate);

    public static string StandardizeDocument(this string document)
    {
        return document.Length switch
        {
            CPF_SIZE => Cpf.FormatarSemPontuacao(document),
            CNPJ_SIZE => Cnpj.FormatarSemPontuacao(document),
            _ => throw new ArgumentException("Invalid document length. Must be either 11 (CPF) or 14 (CNPJ) characters long.", nameof(document))
        };
    }

    public static bool IsValidDocument(this string document)
    {
        return document.Length switch
        {
            CPF_SIZE => Cpf.Validar(document),
            CNPJ_SIZE => Cnpj.Validar(document),
            _ => false
        };
    }
}
