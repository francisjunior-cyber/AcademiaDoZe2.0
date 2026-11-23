// Francis Miguel Kuhl Schweitzer Junior
using System.ComponentModel.DataAnnotations;
namespace AcademiaDoZe.Application.Enums;

public enum AppTipoMovimentoAcesso
{
    [Display(Name = "Entrada")]
    Entrada = 0,
    [Display(Name = "Saída")]
    Saida = 1
}
