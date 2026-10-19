using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(Aluno), "aluno")]
[QueryProperty(nameof(Aluno), "Aluno")]
[QueryProperty(nameof(AlunoId), "id")]
[QueryProperty(nameof(AlunoId), "Id")]
public partial class AlunoViewModel : ObservableObject, IQueryAttributable
{
    private readonly IAlunoService _alunoService;
    private readonly ILogradouroService _logradouroService;

    [ObservableProperty]
    private string _title = "Novo Aluno";

    [ObservableProperty]
    private AlunoDto _aluno = CriarAlunoPadrao();

    [ObservableProperty]
    private int _alunoId;

    [ObservableProperty]
    private string _confirmarSenha = string.Empty;

    [ObservableProperty]
    private DateTime _selectedDataNascimento = DateTime.Today.AddYears(-18);

    public AlunoViewModel(IAlunoService alunoService, ILogradouroService logradouroService)
    {
        _alunoService = alunoService;
        _logradouroService = logradouroService;
    }

    /// <summary>
    /// Executado automaticamente quando o objeto Aluno é atribuído na navegação
    /// </summary>
    partial void OnAlunoChanged(AlunoDto value)
    {
        if (value != null && value.Id > 0)
        {
            _alunoId = value.Id;
            Title = "Editar Aluno";
            ConfirmarSenha = value.Senha ?? string.Empty;
            if (value.DataNascimento != default)
            {
                SelectedDataNascimento = value.DataNascimento.ToDateTime(TimeOnly.MinValue);
            }
        }
    }

    /// <summary>
    /// Executado automaticamente quando o AlunoId é atribuído na navegação
    /// </summary>
    partial void OnAlunoIdChanged(int value)
    {
        if (value > 0 && (Aluno == null || Aluno.Id != value))
        {
            Title = "Editar Aluno";
            _ = CarregarAlunoPorIdAsync(value);
        }
    }

    /// <summary>
    /// Intercepta dinamicamente qualquer parâmetro enviado na navegação
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query == null || query.Count == 0) return;

        foreach (var kvp in query)
        {
            // Se foi passado o objeto AlunoDto
            if (kvp.Value is AlunoDto alunoDto && alunoDto.Id > 0)
            {
                Aluno = alunoDto;
                return;
            }

            // Se foi passado um ID (inteiro ou string)
            if (kvp.Value != null && int.TryParse(kvp.Value.ToString(), out int id) && id > 0)
            {
                AlunoId = id;
                return;
            }
        }
    }

    public async Task InitializeAsync()
    {
        if (AlunoId > 0 && (Aluno == null || Aluno.Id == 0))
        {
            await CarregarAlunoPorIdAsync(AlunoId);
        }
    }

    private async Task CarregarAlunoPorIdAsync(int id)
    {
        try
        {
            var aluno = await _alunoService.ObterPorIdAsync(id);
            if (aluno != null)
            {
                Aluno = aluno;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar aluno: {ex.Message}", "OK");
        }
    }

    private static AlunoDto CriarAlunoPadrao() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Telefone = string.Empty,
        Email = string.Empty,
        Senha = string.Empty,
        Endereco = new LogradouroDto
        {
            Cep = string.Empty,
            Nome = string.Empty,
            Bairro = string.Empty,
            Cidade = string.Empty,
            Estado = string.Empty,
            Pais = "Brasil"
        },
        Numero = string.Empty,
        Complemento = string.Empty
    };

    [RelayCommand]
    private async Task SalvarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Aluno.Nome))
            {
                await Shell.Current.DisplayAlertAsync("Validação", "O campo Nome é obrigatório.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Aluno.Cpf))
            {
                await Shell.Current.DisplayAlertAsync("Validação", "O campo CPF é obrigatório.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Aluno.Senha))
            {
                await Shell.Current.DisplayAlertAsync("Validação", "A senha é obrigatória.", "OK");
                return;
            }

            if (Aluno.Senha != ConfirmarSenha)
            {
                await Shell.Current.DisplayAlertAsync("Validação", "As senhas não coincidem.", "OK");
                return;
            }

            // Garante que o Logradouro existe no banco de dados para evitar erro de Foreign Key (FK)
            if (Aluno.Endereco != null && Aluno.Endereco.Id == 0)
            {
                if (!string.IsNullOrWhiteSpace(Aluno.Endereco.Cep))
                {
                    var logradouroExistente = await _logradouroService.ObterPorCepAsync(Aluno.Endereco.Cep);
                    if (logradouroExistente != null && logradouroExistente.Id > 0)
                    {
                        Aluno.Endereco = logradouroExistente;
                    }
                    else
                    {
                        var novoLogradouro = await _logradouroService.AdicionarAsync(Aluno.Endereco);
                        if (novoLogradouro != null)
                        {
                            Aluno.Endereco = novoLogradouro;
                        }
                    }
                }
            }

            Aluno.DataNascimento = DateOnly.FromDateTime(SelectedDataNascimento);

            if (Aluno.Id == 0)
            {
                await _alunoService.AdicionarAsync(Aluno);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Aluno cadastrado com sucesso!", "OK");
            }
            else
            {
                await _alunoService.AtualizarAsync(Aluno);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Aluno atualizado com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao salvar aluno: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task CancelarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task TirarFotoAsync()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    using Stream sourceStream = await photo.OpenReadAsync();
                    using MemoryStream ms = new();
                    await sourceStream.CopyToAsync(ms);

                    Aluno.Foto = new ArquivoDto
                    {
                        Conteudo = ms.ToArray()
                    };
                    OnPropertyChanged(nameof(Aluno));
                }
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "A captura de foto não é suportada neste dispositivo.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao tirar foto: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SelecionarFotoAsync()
    {
        try
        {
            FileResult photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo != null)
            {
                using Stream sourceStream = await photo.OpenReadAsync();
                using MemoryStream ms = new();
                await sourceStream.CopyToAsync(ms);

                Aluno.Foto = new ArquivoDto
                {
                    Conteudo = ms.ToArray()
                };
                OnPropertyChanged(nameof(Aluno));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar foto: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task BuscarCepAsync()
    {
        if (string.IsNullOrWhiteSpace(Aluno?.Endereco?.Cep))
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Informe o CEP para buscar.", "OK");
            return;
        }

        try
        {
            var logradouro = await _logradouroService.ObterPorCepAsync(Aluno.Endereco.Cep);
            if (logradouro != null)
            {
                Aluno.Endereco = logradouro;
                OnPropertyChanged(nameof(Aluno));
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "CEP não encontrado.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar CEP: {ex.Message}", "OK");
        }
    }
}