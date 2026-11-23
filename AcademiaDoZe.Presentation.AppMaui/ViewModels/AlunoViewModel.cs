// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(AlunoId), "Id")]
public partial class AlunoViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;
    private readonly ILogradouroService _logradouroService;

    private AlunoDto _aluno = CriarAlunoPadrao();
    public AlunoDto Aluno
    {
        get => _aluno;
        set => SetProperty(ref _aluno, value);
    }

    private int _alunoId;
    public int AlunoId
    {
        get => _alunoId;
        set => SetProperty(ref _alunoId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private string _confirmarSenha = string.Empty;
    public string ConfirmarSenha
    {
        get => _confirmarSenha;
        set => SetProperty(ref _confirmarSenha, value);
    }

    private DateTime _selectedDataNascimento = DateTime.Today.AddYears(-18);
    public DateTime SelectedDataNascimento
    {
        get => _selectedDataNascimento;
        set => SetProperty(ref _selectedDataNascimento, value);
    }

    public AlunoViewModel(IAlunoService alunoService, ILogradouroService logradouroService)
    {
        _alunoService = alunoService;
        _logradouroService = logradouroService;
        Title = "Detalhes do Aluno";
    }

    public async Task InitializeAsync()
    {
        if (AlunoId > 0)
        {
            IsEditMode = true;
            Title = "Editar Aluno";

            // Evita sobrescrever alterações já feitas quando a página reaparece
            if (Aluno.Id != AlunoId)
            {
                ConfirmarSenha = string.Empty;
                await LoadAlunoAsync();
            }
        }
        else
        {
            IsEditMode = false;
            Title = "Novo Aluno";
        }
    }

    [RelayCommand]
    public async Task LoadAlunoAsync()
    {
        if (AlunoId <= 0) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var alunoData = await _alunoService.ObterPorIdAsync(AlunoId, cts.Token);

            if (alunoData != null)
            {
                alunoData.Endereco ??= new LogradouroDto
                {
                    Cep = string.Empty,
                    Nome = string.Empty,
                    Bairro = string.Empty,
                    Cidade = string.Empty,
                    Estado = string.Empty,
                    Pais = "Brasil"
                };

                alunoData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;
                Aluno = alunoData;

                if (alunoData.DataNascimento != default)
                {
                    SelectedDataNascimento = alunoData.DataNascimento.ToDateTime(TimeOnly.MinValue);
                }
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Aluno não encontrado.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento do aluno expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
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
        if (IsBusy) return;

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

            // Senha obrigatória no cadastro; na edição, vazia mantém a senha atual (o serviço preserva o hash)
            bool senhaInformada = !string.IsNullOrWhiteSpace(Aluno.Senha);
            if (!IsEditMode && !senhaInformada)
            {
                await Shell.Current.DisplayAlertAsync("Validação", "A senha é obrigatória.", "OK");
                return;
            }

            if ((senhaInformada || !string.IsNullOrWhiteSpace(ConfirmarSenha)) && Aluno.Senha != ConfirmarSenha)
            {
                await Shell.Current.DisplayAlertAsync("Validação", "As senhas não coincidem.", "OK");
                return;
            }

            IsBusy = true;

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
            Aluno.Senha = senhaInformada ? Aluno.Senha : null;

            if (!IsEditMode)
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
        finally
        {
            IsBusy = false;
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
                FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();
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
            FileResult? photo = await MediaPicker.Default.PickPhotoAsync();
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