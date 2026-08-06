using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Reflection;

namespace MacroHelper.UI.ViewModels;

public partial class AjudaViewModel : ObservableObject
{
    public ObservableCollection<FaqItem> Faqs { get; } = new(
    [
        new("Como crio uma macro?",
            "Em Macros, clique em Nova Macro. Defina um atalho em minúsculas com hífens (ex: chamado-recebido), o título e o conteúdo. Depois de salvar, digite o atalho em qualquer campo do Windows.",
            aberta: true),
        new("O que são variáveis e como uso {nome}?",
            "Insira {nome_da_variavel} no conteúdo da macro. Ao usar a macro, o MacroHelper pede o valor se a variável não existir em Variáveis Globais. Use Variáveis Globais para valores fixos como {empresa} ou {assinatura}."),
        new("Por que o atalho não está funcionando em um programa?",
            "Alguns apps bloqueiam teclas de sistema (ex: navegadores com foco em campo de busca). Tente clicar no campo de texto antes de digitar o atalho, ou use Ctrl+Espaço para abrir o buscador manual."),
        new("Como compartilho uma macro com a equipe?",
            "Abra a macro e marque \"Pública\" — ela aparecerá para todos em Comunidade. A equipe pode copiar para a própria lista e editar localmente sem afetar o original."),
        new("Posso recuperar uma macro excluída?",
            "Sim. Em Macros, clique em Histórico de Versões para ver e restaurar versões anteriores de qualquer macro, incluindo as excluídas nos últimos 30 dias."),
    ]);

    public string VersaoApp =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public string SubtituloComVersao => $"MacroHelper {VersaoApp} · atualizado em {DateTime.Today:dd/MM/yyyy}";
}

public partial class FaqItem : ObservableObject
{
    public string Pergunta { get; }
    public string Resposta { get; }
    [ObservableProperty] private bool _aberta;

    public FaqItem(string pergunta, string resposta, bool aberta = false)
    {
        Pergunta = pergunta;
        Resposta = resposta;
        Aberta   = aberta;
    }

    [RelayCommand]
    private void ToggleAberta() => Aberta = !Aberta;
}
