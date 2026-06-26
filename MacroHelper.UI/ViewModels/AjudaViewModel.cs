using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;

namespace MacroHelper.UI.ViewModels;

public partial class AjudaViewModel : ObservableObject
{
    public ObservableCollection<FaqItem> Faqs { get; } = new(
    [
        new("Como crio uma macro?", "Vá em Macros → Nova Macro, defina um atalho (ex: ;ola) e o conteúdo a ser inserido. Use Ctrl+Espaço em qualquer campo de texto para buscar e inserir uma macro rapidamente."),
        new("Como uso variáveis em uma macro?", "Insira {nome_da_variavel} no conteúdo. Se a variável não existir em Variáveis Globais, o app pergunta o valor no momento do uso."),
        new("Como funcionam as Variáveis Globais?", "São valores fixos (ex: {empresa}) substituídos automaticamente em qualquer macro, sem precisar preencher manualmente toda vez."),
        new("Como agendo uma macro?", "Em Agendamentos, escolha a macro, os dias da semana e o horário. No momento marcado, o conteúdo é copiado para a área de transferência."),
        new("O que é a Comunidade interna?", "Macros marcadas como públicas aparecem para toda a equipe, que pode curtir e copiar para a própria lista."),
        new("Como funcionam permissões granulares?", "Em Permissões, um admin pode conceder ações extras (excluir macros, gerenciar categorias etc.) a usuários comuns, além do padrão do perfil."),
        new("Como vejo o que cada usuário fez?", "A tela Auditoria registra ações de todos os usuários (criação, edição, exclusão) com data e detalhes."),
        new("Como configuro um webhook?", "Em Integrações, cadastre a URL e escolha o evento (macro usada, macro criada, usuário criado, login falhou etc.). Use \"Testar\" para validar."),
        new("Onde vejo notificações do sistema?", "Em Notificações, no menu Sistema — novos usuários e outros eventos aparecem ali e na sininho da barra superior."),
        new("Como faço para não perder minhas macros?", "Em Macros, use \"Exportar JSON\" para salvar uma cópia de todas as macros em um arquivo, e \"Importar\" para restaurá-las depois ou em outro computador."),
    ]);

    public string VersaoApp =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public string SubtituloComVersao => $"Perguntas frequentes · SK MacroHelper v{VersaoApp}";
}

public record FaqItem(string Pergunta, string Resposta);
