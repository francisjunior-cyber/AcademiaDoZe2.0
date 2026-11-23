// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class AcessoPage : ContentPage
{
    public AcessoPage(AcessoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is AcessoViewModel viewModel)
        {
            // evita assinar duas vezes caso a página reapareça
            viewModel.FeedbackExibido -= OnFeedbackExibido;
            viewModel.FeedbackExibido += OnFeedbackExibido;
        }

        var animacao = AnimationHelper.AnimarEntradaPaginaAsync(Content);

        if (BindingContext is AcessoViewModel vm)
        {
            await vm.InitializeAsync();
        }

        await animacao;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is AcessoViewModel viewModel)
        {
            viewModel.FeedbackExibido -= OnFeedbackExibido;
        }
    }

    // Anima o banner da catraca: pulso para autorizado, sacudida para acesso negado
    private async void OnFeedbackExibido(bool autorizado)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (autorizado)
                {
                    await AnimationHelper.AnimarPulsoSucessoAsync(BannerFeedback);
                }
                else
                {
                    await AnimationHelper.AnimarErroShakeAsync(BannerFeedback);
                }
            });
        }
        catch (Exception)
        {
            // animação é apenas visual
        }
    }
}
