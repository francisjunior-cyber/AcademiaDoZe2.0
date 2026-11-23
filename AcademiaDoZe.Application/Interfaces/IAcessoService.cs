// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Application.DTOs;
namespace AcademiaDoZe.Application.Interfaces;
/// <summary>
/// Contrato de serviço para o controle de acesso (catraca) de alunos e colaboradores.
/// </summary>
public interface IAcessoService
{
    /// <summary>
    /// Registra a entrada ou a saída do aluno, validando a matrícula ativa na entrada.
    /// Na entrada informa os dias restantes do plano; na saída informa o tempo de permanência.
    /// </summary>
    Task<RegistroAcessoResultadoDto> RegistrarAcessoAlunoAsync(int alunoId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Registra a entrada ou a saída do colaborador, validando o limite diário (CLT = 8h, Estágio = 6h).
    /// Informa as horas trabalhadas no dia (soma de todos os registros).
    /// </summary>
    Task<RegistroAcessoResultadoDto> RegistrarAcessoColaboradorAsync(int colaboradorId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Indica se o aluno está atualmente dentro da academia (quantidade ímpar de registros hoje).
    /// </summary>
    Task<bool> AlunoEstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Indica se o colaborador está atualmente dentro da academia (quantidade ímpar de registros hoje).
    /// </summary>
    Task<bool> ColaboradorEstaNaAcademiaAsync(int colaboradorId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lista os acessos registrados hoje (alunos e colaboradores), do mais recente para o mais antigo.
    /// </summary>
    Task<IEnumerable<RegistroAcessoResultadoDto>> ObterAcessosHojeAsync(CancellationToken cancellationToken = default);
}
