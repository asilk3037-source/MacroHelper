using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly MacroService        _macroService;
    private readonly UsuarioService      _usuarioService;
    private readonly MacrosViewModel     _macrosVM;
    private readonly ConfiguracoesViewModel _configVM;
    private readonly CategoriasViewModel _categoriasVM;
    private readonly RelatorioViewModel  _relatorioVM;
    private readonly UsuariosViewModel   _usuariosVM;
    private readonly DashboardViewModel  _dashboardVM;
    private readonly VariaveisGlobaisViewModel _variaveisVM;
    private readonly HistoricoVersoesViewModel _historicoVM;
    private readonly AgendamentosViewModel     _agendamentosVM;
    private readonly GruposViewModel           _gruposVM;
    private readonly AuditoriaViewModel        _auditoriaVM;
    private readonly ComunidadeViewModel       _comunidadeVM;
    private readonly PermissoesViewModel       _permissoesVM;
    private readonly IntegracoesViewModel      _integracoesVM;
    private readonly NotificacoesViewModel     _notificacoesVM;
    private readonly PerfilViewModel           _perfilVM;
    private readonly AjudaViewModel            _ajudaVM;
    private readonly KioskModeService    _kioskService;
    private readonly NotificacaoService  _notificacaoService;

    [ObservableProperty] private ObservableObject? _currentView;
    [ObservableProperty] private string _paginaAtiva    = "macros";
    [ObservableProperty] private string _statusMensagem = "SK MacroHelper ativo";
    [ObservableProperty] private int    _totalMacros    = 0;
    [ObservableProperty] private string _nomeUsuario    = string.Empty;
    [ObservableProperty] private bool   _isAdmin        = false;
    [ObservableProperty] private bool   _modoKiosk      = false;
    [ObservableProperty] private int    _notificacoesNaoLidas = 0;

    public ObservableCollection<NavItem> NavGeral   { get; } = new();
    public ObservableCollection<NavItem> NavEquipe  { get; } = new();
    public ObservableCollection<NavItem> NavSistema { get; } = new();

    public event EventHandler<bool>? ModoKioskAlterado;

    public MainViewModel(MacroService macroService, UsuarioService usuarioService,
        MacrosViewModel macrosVM, ConfiguracoesViewModel configVM,
        CategoriasViewModel categoriasVM, RelatorioViewModel relatorioVM,
        UsuariosViewModel usuariosVM, KioskModeService kioskService,
        DashboardViewModel dashboardVM, VariaveisGlobaisViewModel variaveisVM,
        HistoricoVersoesViewModel historicoVM, AgendamentosViewModel agendamentosVM,
        GruposViewModel gruposVM, AuditoriaViewModel auditoriaVM,
        ComunidadeViewModel comunidadeVM, PermissoesViewModel permissoesVM,
        IntegracoesViewModel integracoesVM, NotificacoesViewModel notificacoesVM,
        PerfilViewModel perfilVM, AjudaViewModel ajudaVM, NotificacaoService notificacaoService,
        AgendamentoService agendamentoService)
    {
        _macroService   = macroService;
        _usuarioService = usuarioService;
        _macrosVM       = macrosVM;
        _configVM       = configVM;
        _categoriasVM   = categoriasVM;
        _relatorioVM    = relatorioVM;
        _usuariosVM     = usuariosVM;
        _kioskService   = kioskService;
        _dashboardVM     = dashboardVM;
        _variaveisVM     = variaveisVM;
        _historicoVM     = historicoVM;
        _agendamentosVM  = agendamentosVM;
        _gruposVM        = gruposVM;
        _auditoriaVM     = auditoriaVM;
        _comunidadeVM    = comunidadeVM;
        _permissoesVM    = permissoesVM;
        _integracoesVM   = integracoesVM;
        _notificacoesVM  = notificacoesVM;
        _perfilVM        = perfilVM;
        _ajudaVM         = ajudaVM;
        _notificacaoService = notificacaoService;

        var u = usuarioService.UsuarioAtual;
        NomeUsuario = u?.Nome ?? "Usuário";
        IsAdmin     = u?.Perfil == "Admin";

        ModoKiosk = _kioskService.Ativo;
        _kioskService.Alterado += (_, ativo) =>
        {
            ModoKiosk = ativo;
            ModoKioskAlterado?.Invoke(this, ativo);
        };

        notificacaoService.NotificacaoRecebida += (_, _) => _ = AtualizarNotificacoesNaoLidasAsync();
        _ = AtualizarNotificacoesNaoLidasAsync();
        agendamentoService.Iniciar();

        ConstruirNav();
        NavigarParaMacros();
        _ = AtualizarTotalAsync();
    }

    private void ConstruirNav()
    {
        NavGeral.Add(new NavItem { Icone = "", Label = "Início",            Chave = "dashboard",    Comando = NavigarParaDashboardCommand });
        NavGeral.Add(new NavItem { Icone = "", Label = "Macros",            Chave = "macros",       Comando = NavigarParaMacrosCommand });
        NavGeral.Add(new NavItem { Icone = "", Label = "Histórico de Versões", Chave = "historico", Comando = NavigarParaHistoricoCommand });
        NavGeral.Add(new NavItem { Icone = "", Label = "Categorias",        Chave = "categorias",   Comando = NavigarParaCategoriasCommand });
        NavGeral.Add(new NavItem { Icone = "", Label = "Variáveis Globais", Chave = "variaveis",    Comando = NavigarParaVariaveisCommand });
        NavGeral.Add(new NavItem { Icone = "", Label = "Agendamentos",      Chave = "agendamentos", Comando = NavigarParaAgendamentosCommand });
        NavGeral.Add(new NavItem { Icone = "", Label = "Comunidade",        Chave = "comunidade",   Comando = NavigarParaComunidadeCommand });
        NavGeral.Add(new NavItem { Icone = "", Label = "Relatório",         Chave = "relatorio",    Comando = NavigarParaRelatorioCommand });
        NavGeral.Add(new NavItem { Icone = "", Label = "Meu Perfil",        Chave = "perfil",       Comando = NavigarParaPerfilCommand });

        NavEquipe.Add(new NavItem { Icone = "", Label = "Usuários",    Chave = "usuarios",    Comando = NavigarParaUsuariosCommand,    Visivel = IsAdmin });
        NavEquipe.Add(new NavItem { Icone = "", Label = "Grupos",      Chave = "grupos",      Comando = NavigarParaGruposCommand,       Visivel = IsAdmin });
        NavEquipe.Add(new NavItem { Icone = "", Label = "Permissões",  Chave = "permissoes",  Comando = NavigarParaPermissoesCommand,   Visivel = IsAdmin });
        NavEquipe.Add(new NavItem { Icone = "", Label = "Auditoria",   Chave = "auditoria",   Comando = NavigarParaAuditoriaCommand,    Visivel = IsAdmin });
        NavEquipe.Add(new NavItem { Icone = "", Label = "Integrações", Chave = "integracoes", Comando = NavigarParaIntegracoesCommand,  Visivel = IsAdmin });

        NavSistema.Add(new NavItem { Icone = "", Label = "Notificações",   Chave = "notificacoes",   Comando = NavigarParaNotificacoesCommand });
        NavSistema.Add(new NavItem { Icone = "", Label = "Configurações", Chave = "configuracoes", Comando = NavigarParaConfiguracoesCommand });
        NavSistema.Add(new NavItem { Icone = "", Label = "Ajuda",         Chave = "ajuda",         Comando = NavigarParaAjudaCommand });
    }

    private void AtualizarNavAtiva()
    {
        foreach (var item in NavGeral.Concat(NavEquipe).Concat(NavSistema))
            item.Ativo = item.Chave == PaginaAtiva;
    }

    private async Task AtualizarNotificacoesNaoLidasAsync()
        => NotificacoesNaoLidas = await _notificacaoService.ContarNaoLidasAsync();

    [RelayCommand]
    public void SairDoModoKiosk() => _kioskService.Definir(false);

    public event EventHandler? LogoutSolicitado;

    [RelayCommand]
    public void Logout() => LogoutSolicitado?.Invoke(this, EventArgs.Empty);

    [RelayCommand] public void NavigarParaMacros()
    {
        PaginaAtiva = "macros"; CurrentView = _macrosVM; AtualizarNavAtiva();
        _ = _macrosVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaCategorias()
    {
        PaginaAtiva = "categorias"; CurrentView = _categoriasVM; AtualizarNavAtiva();
        _ = _categoriasVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaRelatorio()
    {
        PaginaAtiva = "relatorio"; CurrentView = _relatorioVM; AtualizarNavAtiva();
        _ = _relatorioVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaUsuarios()
    {
        PaginaAtiva = "usuarios"; CurrentView = _usuariosVM; AtualizarNavAtiva();
        _ = _usuariosVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaConfiguracoes()
    {
        PaginaAtiva = "configuracoes"; CurrentView = _configVM; AtualizarNavAtiva();
    }
    [RelayCommand] public void NavigarParaDashboard()
    {
        PaginaAtiva = "dashboard"; CurrentView = _dashboardVM; AtualizarNavAtiva();
        _ = _dashboardVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaVariaveis()
    {
        PaginaAtiva = "variaveis"; CurrentView = _variaveisVM; AtualizarNavAtiva();
        _ = _variaveisVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaHistorico()
    {
        PaginaAtiva = "historico"; CurrentView = _historicoVM; AtualizarNavAtiva();
        _ = _historicoVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaAgendamentos()
    {
        PaginaAtiva = "agendamentos"; CurrentView = _agendamentosVM; AtualizarNavAtiva();
        _ = _agendamentosVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaGrupos()
    {
        PaginaAtiva = "grupos"; CurrentView = _gruposVM; AtualizarNavAtiva();
        _ = _gruposVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaAuditoria()
    {
        PaginaAtiva = "auditoria"; CurrentView = _auditoriaVM; AtualizarNavAtiva();
        _ = _auditoriaVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaComunidade()
    {
        PaginaAtiva = "comunidade"; CurrentView = _comunidadeVM; AtualizarNavAtiva();
        _ = _comunidadeVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaPermissoes()
    {
        PaginaAtiva = "permissoes"; CurrentView = _permissoesVM; AtualizarNavAtiva();
        _ = _permissoesVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaIntegracoes()
    {
        PaginaAtiva = "integracoes"; CurrentView = _integracoesVM; AtualizarNavAtiva();
        _ = _integracoesVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaNotificacoes()
    {
        PaginaAtiva = "notificacoes"; CurrentView = _notificacoesVM; AtualizarNavAtiva();
        _ = _notificacoesVM.CarregarAsync();
    }
    [RelayCommand] public void NavigarParaPerfil()
    {
        PaginaAtiva = "perfil"; CurrentView = _perfilVM; AtualizarNavAtiva();
    }
    [RelayCommand] public void NavigarParaAjuda()
    {
        PaginaAtiva = "ajuda"; CurrentView = _ajudaVM; AtualizarNavAtiva();
    }

    private async Task AtualizarTotalAsync()
    {
        var lista = await _macroService.ObterTodosAsync();
        TotalMacros = lista.Count();
    }

    public void AbrirNovaMacroComConteudo(string conteudo)
    {
        NavigarParaMacros();
        _macrosVM.NovaMacro();
        if (_macrosVM.FormularioAtual != null)
            _macrosVM.FormularioAtual.Conteudo = conteudo;
    }
}
