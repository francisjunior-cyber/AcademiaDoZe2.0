// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class MatriculaListViewModel : BaseViewModel
{
    private const int DiasPadraoVencimento = 30;

    private readonly IMatriculaService _matriculaService;

    public ObservableCollection<string> FilterTypes { get; } = ["Ativas", "Id", "Id Aluno", "Vencendo em dias"];

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Ativas";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    private ObservableCollection<MatriculaDto> _matriculas = [];
    public ObservableCollection<MatriculaDto> Matriculas
    {
        get => _matriculas;
        set => SetProperty(ref _matriculas, value);
    }

    private MatriculaDto? _selectedMatricula;
    public MatriculaDto? SelectedMatricula
    {
        get => _selectedMatricula;
        set => SetProperty(ref _selectedMatricula, value);
    }

    public MatriculaListViewModel(IMatriculaService matriculaService)
    {
        _matriculaService = matriculaService;
        Title = "Matrículas";
    }

    [RelayCommand]
    private async Task AddMatriculaAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("matricula");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de cadastro: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task EditMatriculaAsync(MatriculaDto matricula)
    {
        try
        {
            if (matricula == null) return;
            await Shell.Current.GoToAsync($"matricula?Id={matricula.Id}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de edição: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadMatriculasAsync();
    }

    [RelayCommand]
    private async Task SearchMatriculasAsync()
    {
        await LoadMatriculasAsync();
    }

    // Carrega a lista conforme o filtro selecionado e o texto digitado
    [RelayCommand]
    private async Task LoadMatriculasAsync()
    {
        if (IsBusy)
        {
            IsRefreshing = false;
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var texto = SearchText?.Trim() ?? string.Empty;
            IEnumerable<MatriculaDto> resultados = [];

            if (string.IsNullOrEmpty(texto))
            {
                if (SelectedFilterType == "Ativas")
                {
                    resultados = await _matriculaService.ObterAtivasAsync(0, cts.Token) ?? [];
                }
                else if (SelectedFilterType == "Vencendo em dias")
                {
                    resultados = await _matriculaService.ObterVencendoEmDiasAsync(DiasPadraoVencimento, cts.Token) ?? [];
                }
                else
                {
                    resultados = await _matriculaService.ObterTodasAsync(cts.Token) ?? [];
                }
            }
            else if (SelectedFilterType == "Id")
            {
                if (int.TryParse(texto, out int id))
                {
                    var matricula = await _matriculaService.ObterPorIdAsync(id, cts.Token);
                    if (matricula != null) resultados = [matricula];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "Para busca por ID, informe um número válido.", "OK");
                    return;
                }
            }
            else if (SelectedFilterType == "Id Aluno")
            {
                if (int.TryParse(texto, out int alunoId))
                {
                    resultados = await _matriculaService.ObterPorAlunoIdAsync(alunoId, cts.Token) ?? [];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "Para busca por ID do Aluno, informe um número válido.", "OK");
                    return;
                }
            }
            else if (SelectedFilterType == "Ativas")
            {
                if (int.TryParse(texto, out int alunoId))
                {
                    resultados = await _matriculaService.ObterAtivasAsync(alunoId, cts.Token) ?? [];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "Para filtrar as ativas por aluno, informe o ID do aluno (número inteiro).", "OK");
                    return;
                }
            }
            else if (SelectedFilterType == "Vencendo em dias")
            {
                if (int.TryParse(texto, out int dias))
                {
                    resultados = await _matriculaService.ObterVencendoEmDiasAsync(dias, cts.Token) ?? [];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "Para busca por vencimento, informe a quantidade de dias (número inteiro).", "OK");
                    return;
                }
            }

            var lista = resultados.ToList();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Matriculas.Clear();
                foreach (var item in lista)
                {
                    Matriculas.Add(item);
                }
                OnPropertyChanged(nameof(Matriculas));
            });
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento das matrículas expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMatriculaAsync(MatriculaDto matricula)
    {
        if (matricula == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Confirmar Exclusão",
            $"Deseja realmente excluir a matrícula {matricula.Id} de {matricula.AlunoMatricula.Nome}?",
            "Sim", "Não");

        if (!confirm) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            bool success = await _matriculaService.RemoverAsync(matricula.Id, cts.Token);

            if (success)
            {
                Matriculas.Remove(matricula);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula excluída com sucesso!", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a matrícula.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A exclusão da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao excluir matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
