// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(MatriculaId), "Id")]
public partial class MatriculaViewModel : BaseViewModel
{
    public IEnumerable<AppMatriculaPlano> PlanoTipos { get; } = Enum.GetValues<AppMatriculaPlano>();

    // Itens para múltipla seleção (CheckBoxes) - restrições médicas - flags
    public ObservableCollection<RestricaoSelectable> RestricoesMulti { get; } = [];

    // Suspende a escrita de volta no DTO enquanto a seleção visual é sincronizada a partir dele
    private bool _suspendSelectionSync = false;

    private readonly IMatriculaService _matriculaService;
    private readonly IAlunoService _alunoService;

    private MatriculaDto _matricula = CriarMatriculaPadrao();
    public MatriculaDto Matricula
    {
        get => _matricula;
        set
        {
            if (SetProperty(ref _matricula, value))
            {
                // quando trocar a matrícula, atualiza seleção e propriedades visuais
                SyncSelectionFromFlags();
            }
        }
    }

    private int _matriculaId;
    public int MatriculaId
    {
        get => _matriculaId;
        set => SetProperty(ref _matriculaId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                OnPropertyChanged(nameof(PodeBuscarAluno));
                OnPropertyChanged(nameof(PodeSalvar));
            }
        }
    }

    private string _cpfBusca = string.Empty;
    public string CpfBusca
    {
        get => _cpfBusca;
        set => SetProperty(ref _cpfBusca, value);
    }

    private bool _alunoComMatriculaAtiva;
    public bool AlunoComMatriculaAtiva
    {
        get => _alunoComMatriculaAtiva;
        set
        {
            if (SetProperty(ref _alunoComMatriculaAtiva, value))
            {
                OnPropertyChanged(nameof(PodeSalvar));
            }
        }
    }

    // Plano e data de início passam pelo VM para recalcular a data final automaticamente
    public AppMatriculaPlano? SelectedPlano
    {
        get => Matricula.Plano;
        set
        {
            if (value is null || Matricula.Plano == value.Value) return;
            Matricula.Plano = value.Value;
            OnPropertyChanged();
            RecalcularDataFim();
        }
    }

    public DateTime SelectedDataInicio
    {
        get => Matricula.DataInicio.ToDateTime(TimeOnly.MinValue);
        set
        {
            var novaData = DateOnly.FromDateTime(value);
            if (Matricula.DataInicio == novaData) return;
            Matricula.DataInicio = novaData;
            OnPropertyChanged();
            RecalcularDataFim();
        }
    }

    public string DataFimTexto => Matricula.DataFim == default ? "-" : Matricula.DataFim.ToString("dd/MM/yyyy");

    public bool PodeBuscarAluno => !IsEditMode;
    public bool PodeSalvar => HasAlunoVinculado && (IsEditMode || !AlunoComMatriculaAtiva);

    public bool HasAlunoVinculado => Matricula?.AlunoMatricula != null && Matricula.AlunoMatricula.Id > 0;
    public bool HasNoAlunoVinculado => !HasAlunoVinculado;
    // Aluno entre 12 e 16 anos (idade mínima de 12 anos é garantida pelo cadastro do aluno)
    public bool IsMenor16 => Matricula?.AlunoMatricula != null && Matricula.AlunoMatricula.DataNascimento != default && Matricula.AlunoMatricula.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-16));
    public bool HasRestricoes => Matricula != null && Matricula.RestricoesMedicas != AppMatriculaRestricoes.None;
    public bool ExigeLaudo => IsMenor16 || HasRestricoes;
    public bool HasLaudo => Matricula?.LaudoMedico?.Conteudo != null && Matricula.LaudoMedico.Conteudo.Length > 0;
    public bool LaudoPendente => ExigeLaudo && !HasLaudo;
    public bool LaudoOpcional => !ExigeLaudo && !HasLaudo;

    public MatriculaViewModel(IMatriculaService matriculaService, IAlunoService alunoService)
    {
        _matriculaService = matriculaService;
        _alunoService = alunoService;
        Title = "Detalhes da Matrícula";
        BuildRestricoesMulti();
    }

    private static MatriculaDto CriarMatriculaPadrao()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        return new MatriculaDto
        {
            AlunoMatricula = new AlunoDto
            {
                Nome = string.Empty,
                Cpf = string.Empty,
                DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
                Telefone = string.Empty,
                Endereco = new LogradouroDto
                {
                    Cep = string.Empty,
                    Nome = string.Empty,
                    Bairro = string.Empty,
                    Cidade = string.Empty,
                    Estado = string.Empty,
                    Pais = string.Empty
                },
                Numero = string.Empty
            },
            Plano = AppMatriculaPlano.Anual,
            DataInicio = hoje,
            DataFim = CalcularDataFim(AppMatriculaPlano.Anual, hoje),
            Objetivo = string.Empty,
            RestricoesMedicas = AppMatriculaRestricoes.None,
            ObservacoesRestricoes = string.Empty,
            LaudoMedico = null
        };
    }

    // Mensal +1 mês, Trimestral +3 meses, Semestral +6 meses, Anual +1 ano (mesma regra do domínio)
    private static DateOnly CalcularDataFim(AppMatriculaPlano plano, DateOnly dataInicio)
    {
        return plano switch
        {
            AppMatriculaPlano.Mensal => dataInicio.AddMonths(1),
            AppMatriculaPlano.Trimestral => dataInicio.AddMonths(3),
            AppMatriculaPlano.Semestral => dataInicio.AddMonths(6),
            AppMatriculaPlano.Anual => dataInicio.AddYears(1),
            _ => dataInicio
        };
    }

    private void RecalcularDataFim()
    {
        Matricula.DataFim = CalcularDataFim(Matricula.Plano, Matricula.DataInicio);
        OnPropertyChanged(nameof(DataFimTexto));
    }

    public void NotificarPropriedadesVisuais()
    {
        OnPropertyChanged(nameof(HasAlunoVinculado));
        OnPropertyChanged(nameof(HasNoAlunoVinculado));
        OnPropertyChanged(nameof(IsMenor16));
        OnPropertyChanged(nameof(HasRestricoes));
        OnPropertyChanged(nameof(ExigeLaudo));
        OnPropertyChanged(nameof(HasLaudo));
        OnPropertyChanged(nameof(LaudoPendente));
        OnPropertyChanged(nameof(LaudoOpcional));
        OnPropertyChanged(nameof(PodeSalvar));
        OnPropertyChanged(nameof(SelectedPlano));
        OnPropertyChanged(nameof(SelectedDataInicio));
        OnPropertyChanged(nameof(DataFimTexto));
    }

    public async Task InitializeAsync()
    {
        if (MatriculaId > 0)
        {
            IsEditMode = true;
            Title = "Editar Matrícula";

            // Evita sobrescrever alterações já feitas quando a página reaparece
            if (Matricula.Id != MatriculaId)
            {
                await LoadMatriculaAsync();
            }
        }
        else
        {
            IsEditMode = false;
            Title = "Nova Matrícula";
            SyncSelectionFromFlags();
        }
        NotificarPropriedadesVisuais();
    }

    #region enum Flags <-> seleção múltipla (CheckBoxes)

    // Monta a lista de checkboxes a partir do enum (ignora None)
    private void BuildRestricoesMulti()
    {
        RestricoesMulti.Clear();
        foreach (var value in Enum.GetValues<AppMatriculaRestricoes>())
        {
            if (value == AppMatriculaRestricoes.None) continue;

            var item = new RestricaoSelectable
            {
                Value = value,
                IsSelected = Matricula.RestricoesMedicas.HasFlag(value)
            };
            item.PropertyChanged += RestricaoItem_PropertyChanged;
            RestricoesMulti.Add(item);
        }
    }

    // Atualiza o DTO quando o usuário marca/desmarca
    private void RestricaoItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RestricaoSelectable.IsSelected))
        {
            if (_suspendSelectionSync) return; // evita escrita de volta durante o Sync
            RecomputeRestricoesFlags();
        }
    }

    // Recalcula o bitmask a partir das seleções
    private void RecomputeRestricoesFlags()
    {
        var flags = AppMatriculaRestricoes.None;
        foreach (var item in RestricoesMulti)
        {
            if (item.IsSelected) flags |= item.Value;
        }
        Matricula.RestricoesMedicas = flags;
        OnPropertyChanged(nameof(Matricula));
        NotificarPropriedadesVisuais();
    }

    // Sincroniza a seleção visual a partir do DTO (ao carregar ou substituir a matrícula)
    private void SyncSelectionFromFlags()
    {
        _suspendSelectionSync = true;
        try
        {
            foreach (var item in RestricoesMulti)
            {
                item.IsSelected = Matricula.RestricoesMedicas.HasFlag(item.Value);
            }
        }
        finally
        {
            _suspendSelectionSync = false;
        }
        NotificarPropriedadesVisuais();
    }

    #endregion

    [RelayCommand]
    public void RemoverLaudo()
    {
        Matricula.LaudoMedico = null;
        OnPropertyChanged(nameof(Matricula));
        NotificarPropriedadesVisuais();
    }

    [RelayCommand]
    public async Task LoadMatriculaAsync()
    {
        if (MatriculaId <= 0) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var matriculaData = await _matriculaService.ObterPorIdAsync(MatriculaId, cts.Token);

            if (matriculaData != null)
            {
                Matricula = matriculaData;
                CpfBusca = matriculaData.AlunoMatricula.Cpf;
                AlunoComMatriculaAtiva = false;
                SyncSelectionFromFlags();
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Matrícula não encontrada.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task SaveMatriculaAsync()
    {
        if (IsBusy) return;

        // garante que o DTO está com os flags corretos
        RecomputeRestricoesFlags();

        if (!await ValidateMatriculaAsync(Matricula)) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var alunoData = await _alunoService.ObterPorIdAsync(Matricula.AlunoMatricula.Id, cts.Token);
            if (alunoData == null)
            {
                await Shell.Current.DisplayAlertAsync("Erro", "O Aluno informado não foi encontrado. Busque um aluno válido pelo CPF.", "OK");
                return;
            }
            Matricula.AlunoMatricula = alunoData;
            NotificarPropriedadesVisuais();

            // Revalida com os dados completos do aluno (a idade pode exigir laudo)
            if (!await ValidateMatriculaAsync(Matricula)) return;

            // Bloqueio de nova matrícula quando já existe uma ativa
            if (!IsEditMode && await _matriculaService.PossuiMatriculaAtivaAsync(alunoData.Id, cts.Token))
            {
                AlunoComMatriculaAtiva = true;
                await Shell.Current.DisplayAlertAsync("Matrícula Ativa", "Este aluno já possui uma matrícula ativa. Não é possível criar uma nova matrícula.", "OK");
                return;
            }

            RecalcularDataFim();

            if (IsEditMode)
            {
                await _matriculaService.AtualizarAsync(Matricula, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula atualizada com sucesso!", "OK");
            }
            else
            {
                await _matriculaService.AdicionarAsync(Matricula, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula criada com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O salvamento da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao salvar matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchByCpfAsync()
    {
        var cpfLimpo = new string([.. (CpfBusca ?? string.Empty).Where(char.IsDigit)]);
        if (string.IsNullOrWhiteSpace(cpfLimpo))
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Informe o CPF para realizar a busca.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var alunoData = await _alunoService.ObterPorCpfAsync(cpfLimpo, cts.Token);

            if (alunoData != null)
            {
                Matricula.AlunoMatricula = alunoData;
                CpfBusca = alunoData.Cpf;
                OnPropertyChanged(nameof(Matricula));

                // Bloqueio: não permite nova matrícula se o aluno já possui uma ativa
                AlunoComMatriculaAtiva = !IsEditMode && await _matriculaService.PossuiMatriculaAtivaAsync(alunoData.Id, cts.Token);
                NotificarPropriedadesVisuais();

                if (AlunoComMatriculaAtiva)
                {
                    await Shell.Current.DisplayAlertAsync("Matrícula Ativa", "Este aluno já possui uma matrícula ativa. Não é possível criar uma nova matrícula.", "OK");
                }
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Aluno não encontrado.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca do aluno expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar CPF: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TirarFotoLaudoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "A captura de foto não é suportada neste dispositivo.", "OK");
                return;
            }

            FileResult? foto = await MediaPicker.Default.CapturePhotoAsync();
            await AplicarLaudoAsync(foto);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao capturar o laudo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SelecionarLaudoGaleriaAsync()
    {
        try
        {
            FileResult? foto = await MediaPicker.Default.PickPhotoAsync();
            await AplicarLaudoAsync(foto);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar o laudo: {ex.Message}", "OK");
        }
    }

    private async Task AplicarLaudoAsync(FileResult? arquivo)
    {
        if (arquivo == null) return;

        using var stream = await arquivo.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        Matricula.LaudoMedico = new ArquivoDto { Conteudo = ms.ToArray() };
        OnPropertyChanged(nameof(Matricula));
        NotificarPropriedadesVisuais();
    }

    /*
     Validação utilizando o padrão Notification Pattern, retornando uma lista de erros para o usuário, em vez de fail-fast exceptions.
     Isso permite que o usuário veja todos os problemas de uma vez e corrija-os antes de tentar salvar novamente.
     */
    private static async Task<bool> ValidateMatriculaAsync(MatriculaDto matricula)
    {
        var errors = new List<string>();

        // Aluno
        if (matricula.AlunoMatricula == null || matricula.AlunoMatricula.Id <= 0)
        {
            errors.Add("• Aluno é obrigatório. Informe o CPF e realize a busca.");
        }

        // Data de Início
        if (matricula.DataInicio == default)
        {
            errors.Add("• Data de início é obrigatória.");
        }

        // Objetivo
        if (string.IsNullOrWhiteSpace(matricula.Objetivo))
        {
            errors.Add("• Objetivo da matrícula é obrigatório.");
        }

        // Plano
        if (!Enum.IsDefined(matricula.Plano))
        {
            errors.Add("• Plano da matrícula é inválido.");
        }

        bool possuiLaudo = matricula.LaudoMedico?.Conteudo != null && matricula.LaudoMedico.Conteudo.Length > 0;

        // Laudo médico obrigatório para alunos de 12 a 16 anos
        bool menor16 = matricula.AlunoMatricula != null
            && matricula.AlunoMatricula.DataNascimento != default
            && matricula.AlunoMatricula.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-16));
        if (menor16 && !possuiLaudo)
        {
            errors.Add("• Alunos entre 12 e 16 anos devem obrigatoriamente apresentar laudo médico.");
        }

        // Laudo médico obrigatório para restrições médicas
        if (matricula.RestricoesMedicas != AppMatriculaRestricoes.None && !possuiLaudo)
        {
            errors.Add("• Alunos com restrições médicas cadastradas devem obrigatoriamente apresentar laudo médico.");
        }

        if (errors.Count > 0)
        {
            var mensagem = "Por favor, corrija os seguintes campos:\n\n" + string.Join("\n", errors);
            await Shell.Current.DisplayAlertAsync("Erros de Validação", mensagem, "OK");
            return false;
        }

        return true;
    }
}
