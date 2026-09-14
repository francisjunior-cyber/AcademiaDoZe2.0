//Francis Miguel Kuhl Schweitzer junior

using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Services;

namespace AcademiaDoZe.Domain.ValueObjects;

public record Cpf
{
    public string Valor { get; }

    private Cpf(string valor)
    {
        Valor = valor;
    }

    public static Result<Cpf> Criar(string valor)
    {
        if (NormalizadoService.TextoVazioOuNulo(valor))
            return Result<Cpf>.Failure("Cpf", "CPF_OBRIGATORIO");

        var textoLimpo = NormalizadoService.LimparEDigitos(valor);

        if (textoLimpo.Length != 11)
            return Result<Cpf>.Failure("Cpf", "CPF_DIGITOS");

        if (!Validar(textoLimpo))
            return Result<Cpf>.Failure("Cpf", "CPF_INVALIDO");

        return Result<Cpf>.Success(new Cpf(textoLimpo));
    }

    private static bool Validar(string cpf)
    {
        if (cpf.Length != 11) return false;

        // Rejeita CPFs com todos os dígitos iguais (ex: 111.111.111-11)
        if (cpf.Distinct().Count() == 1) return false;

        var tempCpf = cpf[..9];
        var soma = 0;

        int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        for (var i = 0; i < 9; i++)
            soma += (tempCpf[i] - '0') * multiplicador1[i];

        var resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        var digito = resto.ToString();
        tempCpf += digito;

        soma = 0;
        int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        for (var i = 0; i < 10; i++)
            soma += (tempCpf[i] - '0') * multiplicador2[i];

        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        digito += resto.ToString();

        return cpf.EndsWith(digito);
    }

    public override string ToString() => Valor;
}