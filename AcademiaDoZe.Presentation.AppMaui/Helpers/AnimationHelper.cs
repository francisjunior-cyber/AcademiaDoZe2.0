// Francis Miguel Kuhl Schweitzer Junior
namespace AcademiaDoZe.Presentation.AppMaui.Helpers;

// Animações leves e reutilizáveis para as páginas do aplicativo
public static class AnimationHelper
{
    // Entrada suave da página: surge com fade e leve deslocamento vertical
    public static async Task AnimarEntradaPaginaAsync(VisualElement? elemento, uint duracao = 350)
    {
        if (elemento == null) return;
        try
        {
            elemento.Opacity = 0;
            elemento.TranslationY = 24;
            await Task.WhenAll(
                elemento.FadeToAsync(1, duracao, Easing.CubicOut),
                elemento.TranslateToAsync(0, 0, duracao, Easing.CubicOut));
        }
        catch (Exception)
        {
            // animação é apenas visual: em caso de falha garante o estado final visível
            elemento.Opacity = 1;
            elemento.TranslationY = 0;
        }
    }

    // Pulso de sucesso: aumenta levemente e retorna ao tamanho normal
    public static async Task AnimarPulsoSucessoAsync(VisualElement? elemento)
    {
        if (elemento == null) return;
        try
        {
            elemento.Opacity = 1;
            elemento.TranslationX = 0;
            await elemento.ScaleToAsync(1.04, 140, Easing.CubicOut);
            await elemento.ScaleToAsync(1.0, 140, Easing.CubicIn);
        }
        catch (Exception)
        {
            elemento.Scale = 1;
        }
    }

    // Erro: sacudida horizontal curta
    public static async Task AnimarErroShakeAsync(VisualElement? elemento)
    {
        if (elemento == null) return;
        try
        {
            elemento.Opacity = 1;
            elemento.Scale = 1;
            foreach (double deslocamento in new double[] { -12, 12, -9, 9, -5, 5, 0 })
            {
                await elemento.TranslateToAsync(deslocamento, 0, 45, Easing.Linear);
            }
        }
        catch (Exception)
        {
            elemento.TranslationX = 0;
        }
    }
}
