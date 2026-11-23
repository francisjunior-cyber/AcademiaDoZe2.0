// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Presentation.AppMaui.ViewModels;
using AcademiaDoZe.Presentation.AppMaui.Views;
using Microsoft.Extensions.Logging;
using AcademiaDoZe.Presentation.AppMaui.Configuration;
namespace AcademiaDoZe.Presentation.AppMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

            fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
        });
        // Configurar serviços da aplicação e repositórios
        ConfigurationHelper.ConfigureServices(builder.Services);
        // Registrar ViewModels
        builder.Services.AddTransient<DashboardListViewModel>();
        builder.Services.AddTransient<LogradouroListViewModel>();
        builder.Services.AddTransient<LogradouroViewModel>();
        // Registrar Views
        builder.Services.AddTransient<DashboardListPage>();
        builder.Services.AddTransient<LogradouroListPage>();
        builder.Services.AddTransient<LogradouroPage>();
        builder.Services.AddTransient<ConfigPage>();
        // Registrar ViewModels do Colaborador
        builder.Services.AddTransient<ColaboradorListViewModel>();
        builder.Services.AddTransient<ColaboradorViewModel>();
        // Registrar Views do Colaborador
        builder.Services.AddTransient<ColaboradorListPage>();
        builder.Services.AddTransient<ColaboradorPage>();
        builder.Services.AddTransient<AlunoListViewModel>();
        builder.Services.AddTransient<AlunoViewModel>();
        builder.Services.AddTransient<AlunoListPage>();
        builder.Services.AddTransient<AlunoPage>();
        // Registrar ViewModels e Views de Matrícula
        builder.Services.AddTransient<MatriculaListViewModel>();
        builder.Services.AddTransient<MatriculaViewModel>();
        builder.Services.AddTransient<MatriculaListPage>();
        builder.Services.AddTransient<MatriculaPage>();
        // Registrar ViewModel e View do Controle de Acesso (catraca)
        builder.Services.AddTransient<AcessoViewModel>();
        builder.Services.AddTransient<AcessoPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}