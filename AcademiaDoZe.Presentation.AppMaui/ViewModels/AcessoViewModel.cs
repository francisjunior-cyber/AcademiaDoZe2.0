// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AcessoViewModel : BaseViewModel
{
    private readonly IAcessoService _acessoService;
    private readonly IAlunoService _alunoService;
    private readonly IColaboradorService _colaboradorService;

    // Evita que a seleção feita por código (busca por CPF) dispare novamente a identificação
    private bool _suspendSelecao;

    // Notifica a página para animar o banner (true = autorizado, false = negado)
    public event Action<bool>? FeedbackExibido;

    #region Modo (Aluno / Colaborador)

    private bool _isModoAluno = true;
    public bool IsModoAluno
    {
        get => _isModoAluno;
        set
        {
            if (SetProperty(ref _isModoAluno, value))
            {
                OnPropertyChanged(nameof(IsModoColaborador));
                OnPropertyChanged(nameof(TextoModo));
                OnPropertyChanged(nameof(TextoSelecao));
                OnPropertyChanged(nameof(PlaceholderCpf));
            }
        }
    }

    public bool IsModoColaborador => !IsModoAluno;
    public string TextoModo => IsModoAluno ? "Modo atual selecionado: Aluno" : "Modo atual selecionado: Colaborador";
    public string TextoSelecao => IsModoAluno ? "Selecione o Aluno:" : "Selecione o Colaborador:";
    public string PlaceholderCpf => IsModoAluno ? "CPF do aluno (somente números)" : "CPF do colaborador (somente números)";

    #endregion

    #region Localizar pessoa

    private string _cpfBusca = string.Empty;
    public string CpfBusca
    {
        get => _cpfBusca;
        set => SetProperty(ref _cpfBusca, value);
    }

    public ObservableCollection<AlunoDto> Alunos { get; } = [];
    public ObservableCollection<ColaboradorDto> Colaboradores { get; } = [];

    private AlunoDto? _alunoSelecionado;
    public AlunoDto? AlunoSelecionado
    {
        get => _alunoSelecionado;
        set
        {
            if (SetProperty(ref _alunoSelecionado, value) && value != null && !_suspendSelecao)
            {
                _ = IdentificarPessoaAsync(value.Id, value.Nome, value.Cpf, value.Foto);
            }
        }
    }

    private ColaboradorDto? _colaboradorSelecionado;
    public ColaboradorDto? ColaboradorSelecionado
    {
        get => _colaboradorSelecionado;
        set
        {
            if (SetProperty(ref _colaboradorSelecionado, value) && value != null && !_suspendSelecao)
            {
                _ = IdentificarPessoaAsync(value.Id, value.Nome, value.Cpf, value.Foto);
            }
        }
    }

    #endregion

    #region Identificação e liberação

    private int _pessoaId;
    public int PessoaId
    {
        get => _pessoaId;
        set
        {
            if (SetProperty(ref _pessoaId, value))
            {
                OnPropertyChanged(nameof(TemPessoa));
                OnPropertyChanged(nameof(NaoTemPessoa));
            }
        }
    }

    public bool TemPessoa => PessoaId > 0;
    public bool NaoTemPessoa => !TemPessoa;

    private string _pessoaNome = string.Empty;
    public string PessoaNome
    {
        get => _pessoaNome;
        set => SetProperty(ref _pessoaNome, value);
    }

    private string _pessoaCpf = string.Empty;
    public string PessoaCpf
    {
        get => _pessoaCpf;
        set => SetProperty(ref _pessoaCpf, value);
    }

    private ArquivoDto? _pessoaFoto;
    public ArquivoDto? PessoaFoto
    {
        get => _pessoaFoto;
        set => SetProperty(ref _pessoaFoto, value);
    }

    private bool _dentroDaAcademia;
    public bool DentroDaAcademia
    {
        get => _dentroDaAcademia;
        set
        {
            if (SetProperty(ref _dentroDaAcademia, value))
            {
                OnPropertyChanged(nameof(StatusTexto));
                OnPropertyChanged(nameof(TextoBotaoRegistro));
            }
        }
    }

    public string StatusTexto => DentroDaAcademia ? "DENTRO DA ACADEMIA" : "FORA DA ACADEMIA";
    public string TextoBotaoRegistro => DentroDaAcademia ? "Registrar Saída" : "Registrar Entrada";

    #endregion

    #region Feedback (banner)

    private bool _feedbackVisivel;
    public bool FeedbackVisivel
    {
        get => _feedbackVisivel;
        set => SetProperty(ref _feedbackVisivel, value);
    }

    private bool _feedbackAutorizado;
    public bool FeedbackAutorizado
    {
        get => _feedbackAutorizado;
        set => SetProperty(ref _feedbackAutorizado, value);
    }

    private string _feedbackTitulo = string.Empty;
    public string FeedbackTitulo
    {
        get => _feedbackTitulo;
        set => SetProperty(ref _feedbackTitulo, value);
    }

    private string _feedbackMensagem = string.Empty;
    public string FeedbackMensagem
    {
        get => _feedbackMensagem;
        set => SetProperty(ref _feedbackMensagem, value);
    }

    #endregion

    public ObservableCollection<RegistroAcessoResultadoDto> AcessosHoje { get; } = [];

    public AcessoViewModel(IAcessoService acessoService, IAlunoService alunoService, IColaboradorService colaboradorService)
    {
        _acessoService = acessoService;
        _alunoService = alunoService;
        _colaboradorService = colaboradorService;
        Title = "Controle de Acesso";
    }

    // Carrega as listas de seleção e os acessos de hoje
    public async Task InitializeAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var tAlunos = _alunoService.ObterTodosAsync(cts.Token);
            var tColaboradores = _colaboradorService.ObterTodosAsync(cts.Token);
            var tAcessos = _acessoService.ObterAcessosHojeAsync(cts.Token);
            await Task.WhenAll(tAlunos, tColaboradores, tAcessos);

            var alunos = (tAlunos.Result ?? []).ToList();
            var colaboradores = (tColaboradores.Result ?? []).ToList();
            var acessos = (tAcessos.Result ?? []).ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Alunos.Clear();
                foreach (var aluno in alunos) Alunos.Add(aluno);

                Colaboradores.Clear();
                foreach (var colaborador in colaboradores) Colaboradores.Add(colaborador);

                AcessosHoje.Clear();
                foreach (var acesso in acessos) AcessosHoje.Add(acesso);
            });

            // Atualiza o status da pessoa já identificada (se houver)
            if (TemPessoa)
            {
                await AtualizarStatusAsync();
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento dos dados do controle de acesso expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar o controle de acesso: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelecionarModoAluno()
    {
        AlterarModo(true);
    }

    [RelayCommand]
    private void SelecionarModoColaborador()
    {
        AlterarModo(false);
    }

    private void AlterarModo(bool modoAluno)
    {
        if (IsModoAluno == modoAluno) return;

        IsModoAluno = modoAluno;
        LimparIdentificacao();
        CpfBusca = string.Empty;
        FeedbackVisivel = false;
    }

    private void LimparIdentificacao()
    {
        _suspendSelecao = true;
        try
        {
            AlunoSelecionado = null;
            ColaboradorSelecionado = null;
        }
        finally
        {
            _suspendSelecao = false;
        }

        PessoaId = 0;
        PessoaNome = string.Empty;
        PessoaCpf = string.Empty;
        PessoaFoto = null;
        DentroDaAcademia = false;
    }

    [RelayCommand]
    private async Task BuscarPorCpfAsync()
    {
        var cpfLimpo = new string([.. (CpfBusca ?? string.Empty).Where(char.IsDigit)]);
        if (string.IsNullOrEmpty(cpfLimpo))
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Informe o CPF para realizar a busca.", "OK");
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            if (IsModoAluno)
            {
                var aluno = await _alunoService.ObterPorCpfAsync(cpfLimpo, cts.Token);
                if (aluno == null)
                {
                    LimparIdentificacao();
                    await Shell.Current.DisplayAlertAsync("Aviso", "Aluno não encontrado.", "OK");
                    return;
                }

                _suspendSelecao = true;
                try
                {
                    AlunoSelecionado = Alunos.FirstOrDefault(a => a.Id == aluno.Id);
                }
                finally
                {
                    _suspendSelecao = false;
                }
                await IdentificarPessoaAsync(aluno.Id, aluno.Nome, aluno.Cpf, aluno.Foto);
            }
            else
            {
                var colaborador = await _colaboradorService.ObterPorCpfAsync(cpfLimpo, cts.Token);
                if (colaborador == null)
                {
                    LimparIdentificacao();
                    await Shell.Current.DisplayAlertAsync("Aviso", "Colaborador não encontrado.", "OK");
                    return;
                }

                _suspendSelecao = true;
                try
                {
                    ColaboradorSelecionado = Colaboradores.FirstOrDefault(c => c.Id == colaborador.Id);
                }
                finally
                {
                    _suspendSelecao = false;
                }
                await IdentificarPessoaAsync(colaborador.Id, colaborador.Nome, colaborador.Cpf, colaborador.Foto);
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar por CPF: {ex.Message}", "OK");
        }
    }

    // Preenche o card de identificação e consulta se a pessoa está dentro ou fora da academia
    private async Task IdentificarPessoaAsync(int id, string nome, string cpf, ArquivoDto? foto)
    {
        PessoaId = id;
        PessoaNome = nome;
        PessoaCpf = cpf;
        PessoaFoto = foto;
        FeedbackVisivel = false;
        await AtualizarStatusAsync();
    }

    private async Task AtualizarStatusAsync()
    {
        if (!TemPessoa) return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            bool dentro = IsModoAluno
                ? await _acessoService.AlunoEstaNaAcademiaAsync(PessoaId, cts.Token)
                : await _acessoService.ColaboradorEstaNaAcademiaAsync(PessoaId, cts.Token);
            DentroDaAcademia = dentro;
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A consulta do status expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao consultar o status de acesso: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RegistrarAcessoAsync()
    {
        if (IsBusy) return;

        if (!TemPessoa)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", IsModoAluno ? "Localize ou selecione um aluno antes de registrar o acesso." : "Localize ou selecione um colaborador antes de registrar o acesso.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var resultado = IsModoAluno
                ? await _acessoService.RegistrarAcessoAlunoAsync(PessoaId, cts.Token)
                : await _acessoService.RegistrarAcessoColaboradorAsync(PessoaId, cts.Token);

            FeedbackAutorizado = resultado.Autorizado;
            FeedbackTitulo = !resultado.Autorizado
                ? "Acesso Negado"
                : resultado.TipoMovimento == AppTipoMovimentoAcesso.Saida ? "Saída Registrada" : "Entrada Autorizada";
            FeedbackMensagem = resultado.Mensagem;
            FeedbackVisivel = true;
            FeedbackExibido?.Invoke(resultado.Autorizado);

            // Atualiza status (dentro/fora) e a lista de acessos de hoje
            DentroDaAcademia = IsModoAluno
                ? await _acessoService.AlunoEstaNaAcademiaAsync(PessoaId, cts.Token)
                : await _acessoService.ColaboradorEstaNaAcademiaAsync(PessoaId, cts.Token);

            await CarregarAcessosHojeAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O registro de acesso expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            FeedbackAutorizado = false;
            FeedbackTitulo = "Erro";
            FeedbackMensagem = $"Erro ao registrar o acesso: {ex.Message}";
            FeedbackVisivel = true;
            FeedbackExibido?.Invoke(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AtualizarAcessosAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await CarregarAcessosHojeAsync(cts.Token);
            if (TemPessoa)
            {
                await AtualizarStatusAsync();
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A atualização dos acessos expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao atualizar os acessos: {ex.Message}", "OK");
        }
    }

    private async Task CarregarAcessosHojeAsync(CancellationToken cancellationToken)
    {
        var acessos = (await _acessoService.ObterAcessosHojeAsync(cancellationToken) ?? []).ToList();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            AcessosHoje.Clear();
            foreach (var acesso in acessos) AcessosHoje.Add(acesso);
        });
    }
}
