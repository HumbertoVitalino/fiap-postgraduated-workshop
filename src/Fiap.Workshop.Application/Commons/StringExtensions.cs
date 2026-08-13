using CpfCnpjLibrary;
using System.Diagnostics.CodeAnalysis;

namespace Fiap.Workshop.Application.Commons;

[ExcludeFromCodeCoverage]
public static class StringExtensions
{
    private const int CPF_SIZE = 11;
    private const int CNPJ_SIZE = 14;

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
