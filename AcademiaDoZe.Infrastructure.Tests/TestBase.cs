//Francis Miguel Kuhl Schweitzer junior
using AcademiaDoZe.Infrastructure.Data;
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
namespace AcademiaDoZe.Infrastructure.Tests;

public abstract class TestBase
{
    // Alterne o SGBD alvo dos testes trocando apenas a constante abaixo:
    private const DatabaseType SelectedDatabaseType = DatabaseType.SqlServer;

    protected string ConnectionString { get; }
    protected DatabaseType DatabaseType { get; }

    protected TestBase()
    {
        DatabaseType = SelectedDatabaseType;

        ConnectionString = DatabaseType switch
        {
            DatabaseType.SqlServer => "Server=localhost; Database=db_academia_do_ze; User Id=francis; Password=abcBolinhas12345; TrustServerCertificate=True; Encrypt=True;",
            _ => throw new ArgumentOutOfRangeException(nameof(DatabaseType), DatabaseType, "SGBD não suportado para testes.")
        };
    }
    #region Geradores de dados aleatórios
    private static int _counter = 10000;
    protected static string GerarCep() => (80000000 + ((int)(DateTime.UtcNow.Ticks % 80000000)) + Interlocked.Increment(ref _counter)).ToString("D8")[..8];
    protected static string GerarTelefone() => (49990000000L + ((DateTime.UtcNow.Ticks % 8000000000L)) + Interlocked.Increment(ref _counter)).ToString();
    protected static string GerarEmail() => $"teste_{Guid.NewGuid().ToString("N")[..8]}@teste.com";

    protected static string GerarCpf()
    {
        var random = new Random();
        int[] cpf = new int[11];

        for (int i = 0; i < 9; i++)
        {
            cpf[i] = random.Next(0, 10);
        }

        int soma = 0;
        for (int i = 0; i < 9; i++)
        {
            soma += cpf[i] * (10 - i);
        }
        int resto = soma % 11;
        cpf[9] = resto < 2 ? 0 : 11 - resto;

        soma = 0;
        for (int i = 0; i < 10; i++)
        {
            soma += cpf[i] * (11 - i);
        }
        resto = soma % 11;
        cpf[10] = resto < 2 ? 0 : 11 - resto;

        return string.Join("", cpf);
    }
    #endregion

}