// Francis Miguel Kuhl Schweitzer Junior
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

    // Navega para a listagem correspondente ao clicar no card
    [RelayCommand]
    private async Task NavigateAsync(string rota)
    {
        if (string.IsNullOrWhiteSpace(rota))
            return;

        try
        {
            await Shell.Current.GoToAsync(rota);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar: {ex.Message}", "OK");
        }
    }

    // Executa a contagem com fallback seguro: se o serviço falhar ou expirar, retorna 0 sem travar os demais
    private static async Task<int> ContarAsync(Func<CancellationToken, Task<int>> contar, CancellationToken token)
    {
        try
        {
            return await contar(token);
        }
        catch
        {
            return 0;
        }
    }

    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var tLogradouros = ContarAsync(async ct => (await _logradouroService.ObterTodosAsync(ct))?.Count() ?? 0, cts.Token);
            var tAlunos = ContarAsync(async ct => (await _alunoService.ObterTodosAsync(ct))?.Count() ?? 0, cts.Token);
            var tColaboradores = ContarAsync(async ct => (await _colaboradorService.ObterTodosAsync(ct))?.Count() ?? 0, cts.Token);
            var tMatriculas = ContarAsync(async ct => (await _matriculaService.ObterTodasAsync(ct))?.Count() ?? 0, cts.Token);

            var totais = await Task.WhenAll(tLogradouros, tAlunos, tColaboradores, tMatriculas);

            // Atualiza as propriedades na thread principal da interface gráfica
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                TotalLogradouros = totais[0];
                TotalAlunos = totais[1];
                TotalColaboradores = totais[2];
                TotalMatriculas = totais[3];
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
