// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class DashboardListPage : ContentPage
{
    public DashboardListPage(DashboardListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var animacao = AnimationHelper.AnimarEntradaPaginaAsync(Content);

        if (BindingContext is DashboardListViewModel viewModel)
        {
            await viewModel.LoadDashboardDataCommand.ExecuteAsync(null);
        }

        await animacao;
    }
}
