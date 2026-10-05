using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(LogradouroId), "Id")]
public partial class LogradouroViewModel : BaseViewModel
{
    private readonly ILogradouroService _logradouroService;

    private LogradouroDto _logradouro = new() { Cep = "", Nome = "", Bairro = "", Cidade = "", Estado = "", Pais = "Brasil" };
    public LogradouroDto Logradouro { get => _logradouro; set => SetProperty(ref _logradouro, value); }

    private int _logradouroId;
    public int LogradouroId { get => _logradouroId; set => SetProperty(ref _logradouroId, value); }

    private bool _isEditMode;
    public bool IsEditMode { get => _isEditMode; set => SetProperty(ref _isEditMode, value); }

    public LogradouroViewModel(ILogradouroService logradouroService)
    {
        _logradouroService = logradouroService;
        Title = "Logradouro";
    }

    public async Task InitializeAsync()
    {
        if (LogradouroId > 0)
        {
            IsEditMode = true;
            Title = "Editar Logradouro";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var data = await _logradouroService.ObterPorIdAsync(LogradouroId, cts.Token);
            if (data != null) Logradouro = data;
        }
        else
        {
            IsEditMode = false;
            Title = "Novo Logradouro";
        }
    }

    [RelayCommand] private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task SaveLogradouroAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (IsEditMode)
                await _logradouroService.AtualizarAsync(Logradouro, cts.Token);
            else
                await _logradouroService.AdicionarAsync(Logradouro, cts.Token);

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }
}