// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;
namespace AcademiaDoZe.Application.Services;

public class AcessoService : IAcessoService
{
    private static readonly TimeSpan LimiteDiarioClt = TimeSpan.FromHours(8);
    private static readonly TimeSpan LimiteDiarioEstagio = TimeSpan.FromHours(6);
    private const string MensagemForaDoHorario = "Acesso negado: fora do horário de funcionamento (06:00 às 22:00).";

    private readonly Func<IAcessoAlunoRepository> _acessoAlunoRepoFactory;
    private readonly Func<IAcessoColaboradorRepository> _acessoColaboradorRepoFactory;
    private readonly Func<IMatriculaRepository> _matriculaRepoFactory;
    private readonly Func<IAlunoRepository> _alunoRepoFactory;
    private readonly Func<IColaboradorRepository> _colaboradorRepoFactory;

    public AcessoService(
        Func<IAcessoAlunoRepository> acessoAlunoRepoFactory,
        Func<IAcessoColaboradorRepository> acessoColaboradorRepoFactory,
        Func<IMatriculaRepository> matriculaRepoFactory,
        Func<IAlunoRepository> alunoRepoFactory,
        Func<IColaboradorRepository> colaboradorRepoFactory)
    {
        _acessoAlunoRepoFactory = acessoAlunoRepoFactory ?? throw new ArgumentNullException(nameof(acessoAlunoRepoFactory));
        _acessoColaboradorRepoFactory = acessoColaboradorRepoFactory ?? throw new ArgumentNullException(nameof(acessoColaboradorRepoFactory));
        _matriculaRepoFactory = matriculaRepoFactory ?? throw new ArgumentNullException(nameof(matriculaRepoFactory));
        _alunoRepoFactory = alunoRepoFactory ?? throw new ArgumentNullException(nameof(alunoRepoFactory));
        _colaboradorRepoFactory = colaboradorRepoFactory ?? throw new ArgumentNullException(nameof(colaboradorRepoFactory));
    }

    #region Aluno

    public async Task<RegistroAcessoResultadoDto> RegistrarAcessoAlunoAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        var alunoRepo = _alunoRepoFactory();
        var acessoRepo = _acessoAlunoRepoFactory();
        var matriculaRepo = _matriculaRepoFactory();
        try
        {
            var aluno = await alunoRepo.ObterPorId(alunoId, cancellationToken);
            if (aluno == null)
            {
                return new RegistroAcessoResultadoDto
                {
                    Autorizado = false,
                    Mensagem = "Acesso negado: aluno não encontrado.",
                    DataHora = DateTime.Now,
                    TipoPessoa = "Aluno",
                    PessoaId = alunoId
                };
            }

            var agora = DateTime.Now;
            var hoje = DateOnly.FromDateTime(agora);
            var registrosHoje = (await acessoRepo.ObterAcessosPorAlunoPeriodo(alunoId, hoje, hoje, cancellationToken)).ToList();
            bool estaDentro = registrosHoje.Count % 2 == 1;

            // Matrícula ativa e vigente: validada na entrada (quem já está dentro sempre pode registrar a saída)
            var matriculaVigente = await ObterMatriculaVigenteAsync(matriculaRepo, alunoId, hoje, cancellationToken);
            if (!estaDentro && matriculaVigente == null)
            {
                return CriarResultadoAluno(aluno, false, null, "Acesso negado: o aluno não possui matrícula ativa.", string.Empty, agora);
            }

            var acessoResult = AcessoAluno.Criar(0, aluno, agora);
            if (acessoResult.IsFailure)
            {
                return CriarResultadoAluno(aluno, false, null, MensagemForaDoHorario, string.Empty, agora);
            }

            if (!estaDentro)
            {
                await acessoRepo.Adicionar(acessoResult.Value!, cancellationToken);
                int diasRestantes = Math.Max(0, matriculaVigente!.DataFim.DayNumber - hoje.DayNumber);
                string resumo = $"{diasRestantes} dia(s)";
                return CriarResultadoAluno(aluno, true, AppTipoMovimentoAcesso.Entrada, $"Entrada autorizada. Dias restantes de plano: {resumo}.", resumo, agora);
            }
            else
            {
                var ultimaEntrada = registrosHoje.OrderBy(r => r.DataHora).ThenBy(r => r.Id).Last();
                await acessoRepo.Adicionar(acessoResult.Value!, cancellationToken);
                string permanencia = FormatarDuracao(agora - ultimaEntrada.DataHora);
                return CriarResultadoAluno(aluno, true, AppTipoMovimentoAcesso.Saida, $"Saída registrada. Tempo de permanência: {permanencia}.", permanencia, agora);
            }
        }
        finally
        {
            Liberar(alunoRepo, acessoRepo, matriculaRepo);
        }
    }

    public async Task<bool> AlunoEstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        var acessoRepo = _acessoAlunoRepoFactory();
        try
        {
            return await acessoRepo.EstaNaAcademia(alunoId, cancellationToken);
        }
        finally
        {
            Liberar(acessoRepo);
        }
    }

    private static async Task<Matricula?> ObterMatriculaVigenteAsync(IMatriculaRepository matriculaRepo, int alunoId, DateOnly hoje, CancellationToken cancellationToken)
    {
        var matriculas = await matriculaRepo.ObterPorAluno(alunoId, cancellationToken);
        return matriculas
            .Where(m => m.DataInicio <= hoje && m.DataFim >= hoje)
            .OrderByDescending(m => m.DataFim)
            .FirstOrDefault();
    }

    private static RegistroAcessoResultadoDto CriarResultadoAluno(Aluno aluno, bool autorizado, AppTipoMovimentoAcesso? movimento, string mensagem, string resumo, DateTime dataHora)
    {
        return new RegistroAcessoResultadoDto
        {
            Autorizado = autorizado,
            TipoMovimento = movimento,
            Mensagem = mensagem,
            NomePessoa = aluno.Nome,
            Cpf = aluno.Cpf.Valor,
            Foto = aluno.Foto?.Conteudo != null ? new ArquivoDto { Conteudo = aluno.Foto.Conteudo } : null,
            DataHora = dataHora,
            ResumoTempo = resumo,
            TipoPessoa = "Aluno",
            PessoaId = aluno.Id
        };
    }

    #endregion

    #region Colaborador

    public async Task<RegistroAcessoResultadoDto> RegistrarAcessoColaboradorAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var colaboradorRepo = _colaboradorRepoFactory();
        var acessoRepo = _acessoColaboradorRepoFactory();
        try
        {
            var colaborador = await colaboradorRepo.ObterPorId(colaboradorId, cancellationToken);
            if (colaborador == null)
            {
                return new RegistroAcessoResultadoDto
                {
                    Autorizado = false,
                    Mensagem = "Acesso negado: colaborador não encontrado.",
                    DataHora = DateTime.Now,
                    TipoPessoa = "Colaborador",
                    PessoaId = colaboradorId
                };
            }

            var agora = DateTime.Now;
            var hoje = DateOnly.FromDateTime(agora);
            var registrosHoje = (await acessoRepo.ObterAcessosPorColaboradorPeriodo(colaboradorId, hoje, hoje, cancellationToken)).ToList();
            bool estaDentro = registrosHoje.Count % 2 == 1;
            var limite = ObterLimiteDiario(colaborador.Vinculo);

            if (!estaDentro)
            {
                var trabalhadoAteAgora = await acessoRepo.ObterHorasTrabalhadasNoDia(colaboradorId, hoje, cancellationToken);
                if (trabalhadoAteAgora >= limite)
                {
                    string vinculo = colaborador.Vinculo == ColaboradorVinculo.Estagio ? "Estágio" : "CLT";
                    return CriarResultadoColaborador(colaborador, false, null,
                        $"Acesso negado: o colaborador já atingiu o limite diário de {limite.TotalHours:0}h ({vinculo}). Horas trabalhadas hoje: {FormatarDuracao(trabalhadoAteAgora)}.",
                        FormatarDuracao(trabalhadoAteAgora), agora);
                }

                var entradaResult = AcessoColaborador.Criar(0, colaborador, agora);
                if (entradaResult.IsFailure)
                {
                    return CriarResultadoColaborador(colaborador, false, null, MensagemForaDoHorario, string.Empty, agora);
                }
                await acessoRepo.Adicionar(entradaResult.Value!, cancellationToken);
                string resumoEntrada = FormatarDuracao(trabalhadoAteAgora);
                return CriarResultadoColaborador(colaborador, true, AppTipoMovimentoAcesso.Entrada,
                    $"Entrada registrada. Horas trabalhadas hoje até o momento: {resumoEntrada}.", resumoEntrada, agora);
            }

            var saidaResult = AcessoColaborador.Criar(0, colaborador, agora);
            if (saidaResult.IsFailure)
            {
                return CriarResultadoColaborador(colaborador, false, null, MensagemForaDoHorario, string.Empty, agora);
            }
            await acessoRepo.Adicionar(saidaResult.Value!, cancellationToken);
            // após a saída todos os pares do dia estão fechados: soma total acumulada no dia
            var totalDia = await acessoRepo.ObterHorasTrabalhadasNoDia(colaboradorId, hoje, cancellationToken);
            string resumoSaida = FormatarDuracao(totalDia);
            return CriarResultadoColaborador(colaborador, true, AppTipoMovimentoAcesso.Saida,
                $"Saída registrada. Tempo total trabalhado hoje: {resumoSaida}.", resumoSaida, agora);
        }
        finally
        {
            Liberar(colaboradorRepo, acessoRepo);
        }
    }

    public async Task<bool> ColaboradorEstaNaAcademiaAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var acessoRepo = _acessoColaboradorRepoFactory();
        try
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var registrosHoje = await acessoRepo.ObterAcessosPorColaboradorPeriodo(colaboradorId, hoje, hoje, cancellationToken);
            return registrosHoje.Count() % 2 == 1;
        }
        finally
        {
            Liberar(acessoRepo);
        }
    }

    private static TimeSpan ObterLimiteDiario(ColaboradorVinculo vinculo)
    {
        return vinculo == ColaboradorVinculo.Estagio ? LimiteDiarioEstagio : LimiteDiarioClt;
    }

    private static RegistroAcessoResultadoDto CriarResultadoColaborador(Colaborador colaborador, bool autorizado, AppTipoMovimentoAcesso? movimento, string mensagem, string resumo, DateTime dataHora)
    {
        return new RegistroAcessoResultadoDto
        {
            Autorizado = autorizado,
            TipoMovimento = movimento,
            Mensagem = mensagem,
            NomePessoa = colaborador.Nome,
            Cpf = colaborador.Cpf.Valor,
            Foto = colaborador.Foto?.Conteudo != null ? new ArquivoDto { Conteudo = colaborador.Foto.Conteudo } : null,
            DataHora = dataHora,
            ResumoTempo = resumo,
            TipoPessoa = "Colaborador",
            PessoaId = colaborador.Id
        };
    }

    #endregion

    #region Acessos de hoje

    public async Task<IEnumerable<RegistroAcessoResultadoDto>> ObterAcessosHojeAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var resultados = new List<RegistroAcessoResultadoDto>();

        var alunoRepo = _alunoRepoFactory();
        var acessoAlunoRepo = _acessoAlunoRepoFactory();
        var matriculaRepo = _matriculaRepoFactory();
        var colaboradorRepo = _colaboradorRepoFactory();
        var acessoColaboradorRepo = _acessoColaboradorRepoFactory();
        try
        {
            // Alunos: posição par = entrada, posição ímpar = saída (dentro de cada aluno, no dia)
            var acessosAlunos = await acessoAlunoRepo.ObterAcessosPorAlunoPeriodo(null, hoje, hoje, cancellationToken);
            foreach (var grupo in acessosAlunos.GroupBy(a => a.AlunoId))
            {
                var aluno = await alunoRepo.ObterPorId(grupo.Key, cancellationToken);
                if (aluno == null) continue;
                var matriculaVigente = await ObterMatriculaVigenteAsync(matriculaRepo, grupo.Key, hoje, cancellationToken);
                var ordenados = grupo.OrderBy(a => a.DataHora).ThenBy(a => a.Id).ToList();
                for (int i = 0; i < ordenados.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        int dias = matriculaVigente == null ? 0 : Math.Max(0, matriculaVigente.DataFim.DayNumber - hoje.DayNumber);
                        string resumo = $"{dias} dia(s)";
                        resultados.Add(CriarResultadoAluno(aluno, true, AppTipoMovimentoAcesso.Entrada, $"Entrada autorizada. Dias restantes de plano: {resumo}.", resumo, ordenados[i].DataHora));
                    }
                    else
                    {
                        string permanencia = FormatarDuracao(ordenados[i].DataHora - ordenados[i - 1].DataHora);
                        resultados.Add(CriarResultadoAluno(aluno, true, AppTipoMovimentoAcesso.Saida, $"Saída registrada. Tempo de permanência: {permanencia}.", permanencia, ordenados[i].DataHora));
                    }
                }
            }

            // Colaboradores: horas acumuladas no dia até cada registro
            var acessosColaboradores = await acessoColaboradorRepo.ObterAcessosPorColaboradorPeriodo(null, hoje, hoje, cancellationToken);
            foreach (var grupo in acessosColaboradores.GroupBy(a => a.ColaboradorId))
            {
                var colaborador = await colaboradorRepo.ObterPorId(grupo.Key, cancellationToken);
                if (colaborador == null) continue;
                var ordenados = grupo.OrderBy(a => a.DataHora).ThenBy(a => a.Id).ToList();
                var acumulado = TimeSpan.Zero;
                for (int i = 0; i < ordenados.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        string resumo = FormatarDuracao(acumulado);
                        resultados.Add(CriarResultadoColaborador(colaborador, true, AppTipoMovimentoAcesso.Entrada, $"Entrada registrada. Horas trabalhadas hoje até o momento: {resumo}.", resumo, ordenados[i].DataHora));
                    }
                    else
                    {
                        acumulado += ordenados[i].DataHora - ordenados[i - 1].DataHora;
                        string resumo = FormatarDuracao(acumulado);
                        resultados.Add(CriarResultadoColaborador(colaborador, true, AppTipoMovimentoAcesso.Saida, $"Saída registrada. Tempo total trabalhado hoje: {resumo}.", resumo, ordenados[i].DataHora));
                    }
                }
            }
        }
        finally
        {
            Liberar(alunoRepo, acessoAlunoRepo, matriculaRepo, colaboradorRepo, acessoColaboradorRepo);
        }

        return [.. resultados.OrderByDescending(r => r.DataHora)];
    }

    #endregion

    // Formata a duração como HH:mm:ss (as horas podem passar de 24 sem reiniciar)
    private static string FormatarDuracao(TimeSpan duracao)
    {
        if (duracao < TimeSpan.Zero) duracao = TimeSpan.Zero;
        return $"{(int)duracao.TotalHours:D2}:{duracao.Minutes:D2}:{duracao.Seconds:D2}";
    }

    // Os repositórios mantêm uma conexão aberta; libera ao final de cada operação
    private static void Liberar(params object?[] recursos)
    {
        foreach (var recurso in recursos)
        {
            (recurso as IDisposable)?.Dispose();
        }
    }
}
