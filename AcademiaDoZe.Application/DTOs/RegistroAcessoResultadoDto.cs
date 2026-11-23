// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Application.Enums;
namespace AcademiaDoZe.Application.DTOs;

// Resultado de uma tentativa de registro de acesso (catraca) de aluno ou colaborador
public class RegistroAcessoResultadoDto
{
    public bool Autorizado { get; set; }
    // Nulo quando o acesso é negado (nenhum movimento foi registrado)
    public AppTipoMovimentoAcesso? TipoMovimento { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string NomePessoa { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public ArquivoDto? Foto { get; set; }
    public DateTime DataHora { get; set; }
    // Entrada de aluno: dias restantes do plano. Saída: tempo de permanência (HH:mm:ss). Colaborador: horas trabalhadas no dia (HH:mm:ss).
    public string ResumoTempo { get; set; } = string.Empty;
    // "Aluno" ou "Colaborador"
    public string TipoPessoa { get; set; } = string.Empty;
    public int PessoaId { get; set; }
}
