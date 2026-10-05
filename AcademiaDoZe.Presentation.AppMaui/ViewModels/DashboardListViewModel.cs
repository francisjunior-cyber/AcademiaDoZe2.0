using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class DashboardListViewModel : BaseViewModel
{
    private readonly ILogradouroService _logradouroService;
    private readonly IAlunoService _alunoService;
    private readonly IColaboradorService _colaboradorService;
    private readonly IMatriculaService _matriculaService;

    private int _totalLogradouros;
    public int TotalLogradouros { get => _totalLogradouros; set => SetProperty(ref _totalLogradouros, value); }

    private int _totalAlunos;
    public int TotalAlunos { get => _totalAlunos; set => SetProperty(ref _totalAlunos, value); }

    private int _totalColaboradores;
    public int TotalColaboradores { get => _totalColaboradores; set => SetProperty(ref _totalColaboradores, value); }

    private int _totalMatriculas;
    public int TotalMatriculas { get => _totalMatriculas; set => SetProperty(ref _totalMatriculas, value); }

    public DashboardListViewModel(ILogradouroService logradouroService, IAlunoService alunoService, IColaboradorService colaboradorService, IMatriculaService matriculaService)
    {
        _logradouroService = logradouroService;
        _alunoService = alunoService;
        _colaboradorService = colaboradorService;
        _matriculaService = matriculaService;
        Title = "Dashboard";
    }

    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            // Busca apenas os Logradouros para evitar timeout em tabelas inexistentes/serviços não implementados
            var logradouros = await _logradouroService.ObterTodosAsync();
            var totalLog = logradouros?.Count() ?? 0;

            // Atualiza as propriedades na thread principal da interface gráfica
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                TotalLogradouros = totalLog;
                TotalAlunos = 0;
                TotalColaboradores = 0;
                TotalMatriculas = 0;
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar dados do painel: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

