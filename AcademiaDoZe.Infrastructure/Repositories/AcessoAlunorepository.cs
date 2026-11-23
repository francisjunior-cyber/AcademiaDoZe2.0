// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
namespace AcademiaDoZe.Infrastructure.Repositories;

// Repositório de acessos (catraca) de alunos. Utiliza a tabela tb_acesso com pessoa_tipo = 0 (Aluno).
public class AcessoAlunoRepository : BaseRepository, IAcessoAlunoRepository
{
    private const int PessoaTipoAluno = 0;

    public AcessoAlunoRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }

    private static string BaseSelectQuery => """
SELECT
ac.id_acesso, ac.pessoa_tipo, ac.pessoa_id, ac.data_hora,
a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email, a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_acesso ac
INNER JOIN tb_aluno a ON ac.pessoa_id = a.id_aluno
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
WHERE ac.pessoa_tipo = 0
""";

    public static AcessoAluno Map(DbDataReader reader)
    {
        try
        {
            int id = reader.GetInt32Value("id_acesso");
            DateTime dataHora = reader.GetDateTimeValue("data_hora");
            var aluno = AlunoRepository.Map(reader);
            var result = AcessoAluno.Criar(id, aluno, dataHora);
            if (result.IsFailure)
            {
                throw new InfrastructureException("ERRO_DOMINIO_MAPEAMENTO", $"Erro de domínio ao mapear acesso ID {id}: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
            }
            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_ACESSO_ALUNO", $"Erro ao mapear dados do acesso do aluno: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} AND ac.id_acesso = @Id";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_ID", $"Erro ao obter acesso de aluno por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} ORDER BY ac.data_hora DESC, ac.id_acesso DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<AcessoAluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }
            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS", $"Erro ao obter todos os acessos de alunos: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Adicionar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES (@PessoaTipo, @PessoaId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@PessoaId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO_ALUNO", "Falha ao obter ID inserido para o acesso do aluno.", cancellationToken);
            var idProperty = typeof(Entity).GetProperty("Id");
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO_ALUNO", $"Erro ao adicionar acesso para o aluno ID {entity.AlunoId}: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Atualizar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "UPDATE tb_acesso SET pessoa_id = @PessoaId, data_hora = @DataHora WHERE id_acesso = @Id AND pessoa_tipo = 0";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@PessoaId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
            {
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Nenhum acesso de aluno encontrado com o ID {entity.Id} para atualização.");
            }
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO_ALUNO", $"Erro ao atualizar acesso ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = 0";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            var result = await command.ExecuteNonQueryAsync(cancellationToken);
            return result > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO_ALUNO", $"Erro ao remover acesso ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = BaseSelectQuery;
            if (alunoId.HasValue) query += " AND ac.pessoa_id = @AlunoId";
            if (inicio.HasValue) query += " AND ac.data_hora >= @Inicio";
            if (fim.HasValue) query += " AND ac.data_hora < @FimExclusivo";
            query += " ORDER BY ac.data_hora ASC, ac.id_acesso ASC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            if (alunoId.HasValue) command.AddParameter("@AlunoId", alunoId.Value, DbType.Int32);
            if (inicio.HasValue) command.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            if (fim.HasValue) command.AddParameter("@FimExclusivo", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<AcessoAluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }
            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_PERIODO", $"Erro ao obter acessos de alunos por período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno?> ObterUltimoAcesso(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} AND ac.pessoa_id = @AlunoId ORDER BY ac.data_hora DESC, ac.id_acesso DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter o último acesso do aluno ID {alunoId}: {ex.Message}", ex);
        }
    }

    // O aluno está na academia quando possui uma quantidade ímpar de registros no dia (entrada sem a respectiva saída)
    public async Task<bool> EstaNaAcademia(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hoje = DateTime.Today;
            string query = "SELECT COUNT(*) FROM tb_acesso WHERE pessoa_tipo = 0 AND pessoa_id = @AlunoId AND data_hora >= @Inicio AND data_hora < @FimExclusivo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            command.AddParameter("@Inicio", hoje, DbType.DateTime);
            command.AddParameter("@FimExclusivo", hoje.AddDays(1), DbType.DateTime);
            var resultado = await command.ExecuteScalarAsync(cancellationToken);
            int total = resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
            return total % 2 == 1;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ESTA_NA_ACADEMIA", $"Erro ao verificar presença do aluno ID {alunoId}: {ex.Message}", ex);
        }
    }

    // Registros brutos (aluno, data/hora) do mês informado, ordenados para permitir o pareamento entrada/saída
    private async Task<List<(int AlunoId, DateTime DataHora)>> ObterRegistrosDoMesAsync(int mes, CancellationToken cancellationToken)
    {
        string query = $"SELECT pessoa_id, data_hora FROM tb_acesso WHERE pessoa_tipo = 0 AND {GetDateMonthExpression("data_hora")} = @Mes ORDER BY pessoa_id ASC, data_hora ASC, id_acesso ASC";
        await using var command = await CreateCommandAsync(query, cancellationToken);
        command.AddParameter("@Mes", mes, DbType.Int32);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var registros = new List<(int AlunoId, DateTime DataHora)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            registros.Add((reader.GetInt32Value("pessoa_id"), reader.GetDateTimeValue("data_hora")));
        }
        return registros;
    }

    // Horário de maior procura no mês, considerando apenas as entradas (registros em posição par de cada dia)
    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var registros = await ObterRegistrosDoMesAsync(mes, cancellationToken);
            var contagem = new Dictionary<TimeOnly, int>();
            foreach (var grupo in registros.GroupBy(r => (r.AlunoId, Dia: r.DataHora.Date)))
            {
                var ordenados = grupo.OrderBy(r => r.DataHora).ToList();
                for (int i = 0; i < ordenados.Count; i += 2)
                {
                    var hora = new TimeOnly(ordenados[i].DataHora.Hour, 0);
                    contagem[hora] = contagem.TryGetValue(hora, out int atual) ? atual + 1 : 1;
                }
            }
            return contagem.OrderByDescending(c => c.Value).ThenBy(c => c.Key).ToDictionary(c => c.Key, c => c.Value);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_HORARIO_MAIS_PROCURADO", $"Erro ao obter o horário mais procurado do mês {mes}: {ex.Message}", ex);
        }
    }

    // Permanência média no mês: média da duração de cada par entrada/saída
    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var registros = await ObterRegistrosDoMesAsync(mes, cancellationToken);
            var duracoes = new List<TimeSpan>();
            foreach (var grupo in registros.GroupBy(r => (r.AlunoId, Dia: r.DataHora.Date)))
            {
                var ordenados = grupo.OrderBy(r => r.DataHora).ToList();
                for (int i = 0; i + 1 < ordenados.Count; i += 2)
                {
                    duracoes.Add(ordenados[i + 1].DataHora - ordenados[i].DataHora);
                }
            }
            var resultado = new Dictionary<int, TimeSpan>();
            if (duracoes.Count > 0)
            {
                resultado[mes] = TimeSpan.FromTicks((long)duracoes.Average(d => d.Ticks));
            }
            return resultado;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_PERMANENCIA_MEDIA", $"Erro ao obter a permanência média do mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(int dias, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = """
SELECT
a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email, a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_aluno a
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
WHERE NOT EXISTS (SELECT 1 FROM tb_acesso ac WHERE ac.pessoa_tipo = 0 AND ac.pessoa_id = a.id_aluno AND ac.data_hora >= @Limite)
ORDER BY a.nome
""";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Limite", DateTime.Now.AddDays(-dias), DbType.DateTime);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var alunos = new List<Aluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                alunos.Add(AlunoRepository.Map(reader));
            }
            return alunos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ALUNOS_SEM_ACESSO", $"Erro ao obter alunos sem acesso nos últimos {dias} dias: {ex.Message}", ex);
        }
    }
}
