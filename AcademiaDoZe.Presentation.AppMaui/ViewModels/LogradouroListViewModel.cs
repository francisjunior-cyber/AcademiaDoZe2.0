using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class LogradouroListViewModel : BaseViewModel
{
    private readonly ILogradouroService _logradouroService;

    public ObservableCollection<string> FilterTypes { get; } = ["Cidade", "Id", "Cep"];

    private ObservableCollection<LogradouroDto> _logradouros = [];
    public ObservableCollection<LogradouroDto> Logradouros { get => _logradouros; set => SetProperty(ref _logradouros, value); }

    private string _searchText = string.Empty;
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }

    private string _selectedFilterType = "Cidade";
    public string SelectedFilterType { get => _selectedFilterType; set => SetProperty(ref _selectedFilterType, value); }

    public LogradouroListViewModel(ILogradouroService logradouroService)
    {
        _logradouroService = logradouroService;
        Title = "Logradouros";
    }

    [RelayCommand]
    private async Task AddLogradouroAsync() => await Shell.Current.GoToAsync("logradouro");

    [RelayCommand]
    private async Task EditLogradouroAsync(LogradouroDto logradouro)
    {
        if (logradouro != null)
            await Shell.Current.GoToAsync($"logradouro?Id={logradouro.Id}");
    }

    [RelayCommand]
    private async Task LoadLogradourosAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            Logradouros.Clear();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var list = await _logradouroService.ObterTodosAsync(cts.Token);
            if (list != null)
            {
                foreach (var item in list) Logradouros.Add(item);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
        }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    private async Task DeleteLogradouroAsync(LogradouroDto logradouro)
    {
        if (logradouro == null) return;
        bool confirm = await Shell.Current.DisplayAlertAsync("Confirmar", $"Excluir {logradouro.Nome}?", "Sim", "Não");
        if (!confirm) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (await _logradouroService.RemoverAsync(logradouro.Id, cts.Token))
            {
                Logradouros.Remove(logradouro);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }
}