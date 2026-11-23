// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Application.Enums;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// Converte o enum [Flags] AppMatriculaRestricoes para uma string amigável (lista separada por vírgula)
// Utiliza o atributo [Display(Name = "...")] para obter os nomes amigáveis
public sealed class RestricoesFlagsDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AppMatriculaRestricoes flags) return string.Empty;

        // Nenhuma restrição marcada
        if (flags == AppMatriculaRestricoes.None) return AppMatriculaRestricoes.None.GetDisplayName();

        // Junta todos os valores que estão marcados
        var partes = new List<string>();
        foreach (var item in Enum.GetValues<AppMatriculaRestricoes>())
        {
            if (item == AppMatriculaRestricoes.None) continue;
            if (flags.HasFlag(item)) partes.Add(item.GetDisplayName());
        }

        return partes.Count > 0 ? string.Join(", ", partes) : AppMatriculaRestricoes.None.GetDisplayName();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
