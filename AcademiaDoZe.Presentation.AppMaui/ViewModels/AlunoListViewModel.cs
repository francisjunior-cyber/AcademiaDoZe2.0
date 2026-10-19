using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AlunoListViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;

    public ObservableCollection<string> FilterTypes { get; } = ["Nome", "CPF", "Id", "Email"];

    private ObservableCollection<AlunoDto> _alunos = [];
    public ObservableCollection<AlunoDto> Alunos
    {
        get => _alunos;
        set => SetProperty(ref _alunos, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Nome";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public AlunoListViewModel(IAlunoService alunoService)
    {
        _alunoService = alunoService;
        Title = "Alunos";
    }

    [RelayCommand]
    private async Task AddAlunoAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("aluno");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de cadastro: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task EditAlunoAsync(AlunoDto aluno)
    {
        try
        {
            if (aluno == null) return;
            await Shell.Current.GoToAsync($"aluno?Id={aluno.Id}");
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
        await LoadAlunosAsync();
    }

    [RelayCommand]
    private async Task LoadAlunosAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Alunos.Clear();
                OnPropertyChanged(nameof(Alunos));
            });

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunosList = await _alunoService.ObterTodosAsync(cts.Token);

            if (alunosList != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var aluno in alunosList)
                    {
                        Alunos.Add(aluno);
                    }
                    OnPropertyChanged(nameof(Alunos));
                });
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento dos alunos expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar alunos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAlunoAsync(AlunoDto aluno)
    {
        if (aluno == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Confirmar Exclusão",
            $"Deseja realmente excluir o aluno {aluno.Nome}?",
            "Sim", "Não");

        if (!confirm) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool success = await _alunoService.RemoverAsync(aluno.Id, cts.Token);

            if (success)
            {
                Alunos.Remove(aluno);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Aluno excluído com sucesso!", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir o aluno.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A exclusão do aluno expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao excluir aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}