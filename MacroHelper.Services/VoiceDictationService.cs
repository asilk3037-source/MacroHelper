using System.Linq;
#if WINDOWS
using System.Globalization;
using System.Speech.Recognition;
#endif

namespace MacroHelper.Services;

/// <summary>
/// Ditado por voz (MVP): diga "inserir &lt;atalho&gt;" para inserir uma macro sem teclado.
/// No modo contínuo, basta dizer "ei macro, inserir &lt;atalho&gt;" em qualquer momento — sem precisar
/// acionar o atalho de teclado antes. A palavra de ativação evita que conversas normais disparem macros.
/// Requer o reconhecedor de fala pt-BR instalado no Windows.
/// </summary>
public class VoiceDictationService : IDisposable
{
    private static readonly string[] PalavrasDeAtivacao = ["ei macro", "ok macro"];

    public bool Ativo { get; private set; }
    public bool ModoContinuoAtivo { get; private set; }
    public event EventHandler<string>? AtalhoReconhecido;
    public event EventHandler<string>? StatusAlterado;

#if WINDOWS
    private SpeechRecognitionEngine? _engine;
    private Grammar? _grammarManual;
    private Grammar? _grammarContinuo;
#endif

    public void AtualizarVocabulario(IEnumerable<string> atalhos)
    {
#if WINDOWS
        var lista = atalhos.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
        if (lista.Count == 0) return;

        var continuoEstava = ModoContinuoAtivo;

        try
        {
            var engine = new SpeechRecognitionEngine(new CultureInfo("pt-BR"));

            var choices = new Choices(lista.ToArray());

            var gbManual = new GrammarBuilder("inserir");
            gbManual.Append(choices);
            var grammarManual = new Grammar(gbManual) { Name = "manual" };

            var gbContinuo = new GrammarBuilder(new Choices(PalavrasDeAtivacao));
            gbContinuo.Append("inserir");
            gbContinuo.Append(choices);
            var grammarContinuo = new Grammar(gbContinuo) { Name = "continuo" };

            engine.LoadGrammar(grammarManual);
            engine.LoadGrammar(grammarContinuo);
            engine.SetInputToDefaultAudioDevice();
            engine.SpeechRecognized += (_, e) =>
            {
                var atalho = e.Result.Words.Count > 0 ? e.Result.Words[^1].Text : string.Empty;
                if (!string.IsNullOrWhiteSpace(atalho))
                    AtalhoReconhecido?.Invoke(this, atalho);
            };

            _engine?.Dispose();
            _engine          = engine;
            _grammarManual   = grammarManual;
            _grammarContinuo = grammarContinuo;
            Ativo             = false;
            ModoContinuoAtivo = false;
            StatusAlterado?.Invoke(this, $"Vocabulário carregado ({lista.Count} atalhos).");

            if (continuoEstava) IniciarModoContinuo();
        }
        catch (Exception ex)
        {
            StatusAlterado?.Invoke(this, $"Reconhecimento de voz indisponível: {ex.Message}");
        }
#else
        StatusAlterado?.Invoke(this, "Ditado por voz disponível apenas no Windows.");
#endif
    }

    /// <summary>Alterna o modo manual (push-to-talk pelo atalho de teclado): "inserir &lt;atalho&gt;".</summary>
    public void Alternar()
    {
#if WINDOWS
        if (_engine == null)
        {
            StatusAlterado?.Invoke(this, "Carregue o vocabulário antes de ativar o ditado.");
            return;
        }
        if (ModoContinuoAtivo)
        {
            StatusAlterado?.Invoke(this, "Desative o modo contínuo antes de usar o ditado manual.");
            return;
        }

        if (Ativo)
        {
            _engine.RecognizeAsyncStop();
            Ativo = false;
            StatusAlterado?.Invoke(this, "Ditado por voz desativado.");
        }
        else
        {
            _grammarManual!.Enabled = true;
            _engine.RecognizeAsync(RecognizeMode.Multiple);
            Ativo = true;
            StatusAlterado?.Invoke(this, "Ditado por voz ativo — diga \"inserir <atalho>\".");
        }
#else
        StatusAlterado?.Invoke(this, "Ditado por voz disponível apenas no Windows.");
#endif
    }

    /// <summary>
    /// Liga a escuta contínua em segundo plano: a qualquer momento, diga "ei macro, inserir &lt;atalho&gt;".
    /// A palavra de ativação ("ei macro"/"ok macro") evita disparos acidentais durante conversas normais.
    /// </summary>
    public void IniciarModoContinuo()
    {
#if WINDOWS
        if (_engine == null)
        {
            StatusAlterado?.Invoke(this, "Carregue o vocabulário antes de ativar o modo contínuo.");
            return;
        }
        if (Ativo) { _engine.RecognizeAsyncStop(); Ativo = false; }

        _grammarManual!.Enabled   = false;
        _grammarContinuo!.Enabled = true;
        _engine.RecognizeAsync(RecognizeMode.Multiple);
        ModoContinuoAtivo = true;
        StatusAlterado?.Invoke(this, "Modo contínuo ativo — diga \"ei macro, inserir <atalho>\" quando quiser.");
#else
        StatusAlterado?.Invoke(this, "Ditado por voz disponível apenas no Windows.");
#endif
    }

    public void PararModoContinuo()
    {
#if WINDOWS
        if (!ModoContinuoAtivo) return;
        _engine?.RecognizeAsyncStop();
        ModoContinuoAtivo = false;
        StatusAlterado?.Invoke(this, "Modo contínuo desativado.");
#endif
    }

    public void Dispose()
    {
#if WINDOWS
        _engine?.Dispose();
#endif
        GC.SuppressFinalize(this);
    }
}
