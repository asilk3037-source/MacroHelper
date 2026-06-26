namespace MacroHelper.Services;

public record MacroTemplate(string Setor, string Titulo, string Atalho, string Conteudo);

/// <summary>Modelos prontos de macro, organizados por setor, para acelerar a criação de novas macros.</summary>
public static class MacroTemplates
{
    public static readonly MacroTemplate[] Todos =
    [
        new("Atendimento", "Saudação inicial", "ola-atendimento",
            "Olá {nome}, tudo bem? Meu nome é {usuario} e vou te ajudar por aqui. Como posso ajudar hoje?"),
        new("Atendimento", "Encerramento de chamado", "encerrar-chamado",
            "Ficamos felizes em ajudar! Seu chamado foi encerrado em {data}. Se precisar de algo mais, estamos à disposição."),
        new("Atendimento", "Aguardando retorno do cliente", "aguardando-retorno",
            "Olá {nome}, ainda não tivemos seu retorno sobre o assunto tratado. Posso seguir com o atendimento ou prefere remarcar?"),

        new("Vendas", "Apresentação de proposta", "proposta-comercial",
            "Olá {nome}, segue nossa proposta comercial conforme conversamos. Qualquer dúvida, fico à disposição para ajustarmos juntos."),
        new("Vendas", "Follow-up pós-reunião", "followup-reuniao",
            "Olá {nome}, foi um prazer conversar hoje ({data}). Conforme alinhado, os próximos passos são: [...]. Fico no aguardo do seu retorno."),
        new("Vendas", "Agradecimento por fechamento", "fechamento-venda",
            "Parabéns pela escolha, {nome}! Sua compra foi confirmada em {data}. Em breve nossa equipe entrará em contato com os próximos passos."),

        new("RH", "Boas-vindas a novo colaborador", "boas-vindas-rh",
            "Olá {nome}, seja muito bem-vindo(a) à equipe! Seu primeiro dia será {data}. Em breve enviaremos todas as orientações de integração."),
        new("RH", "Confirmação de entrevista", "confirmar-entrevista",
            "Olá {nome}, confirmando sua entrevista agendada para {data} às {hora}. Qualquer imprevisto, nos avise com antecedência."),
        new("RH", "Solicitação de documentos", "solicitar-documentos-rh",
            "Olá {nome}, para darmos sequência ao seu processo, precisamos dos seguintes documentos: [...]. Pode nos enviar até {data}?"),

        new("TI / Suporte técnico", "Chamado recebido", "chamado-recebido",
            "Olá {nome}, recebemos seu chamado em {data} e já estamos analisando. Retornaremos em breve com uma atualização."),
        new("TI / Suporte técnico", "Solicitação de acesso remoto", "solicitar-acesso-remoto",
            "Olá {nome}, para resolvermos mais rápido, podemos acessar sua máquina remotamente? Se sim, me confirme um horário disponível."),
        new("TI / Suporte técnico", "Problema resolvido", "problema-resolvido",
            "Olá {nome}, o problema reportado foi resolvido em {data}. Pode verificar e nos confirmar se tudo ficou certo?"),

        new("Financeiro", "Cobrança amigável", "cobranca-amigavel",
            "Olá {nome}, identificamos uma pendência referente à fatura com vencimento em {data}. Pode nos confirmar a previsão de pagamento?"),
        new("Financeiro", "Confirmação de pagamento", "confirmar-pagamento",
            "Olá {nome}, confirmamos o recebimento do seu pagamento em {data}. Obrigado!"),
    ];

    public static IEnumerable<string> Setores => Todos.Select(t => t.Setor).Distinct();
}
