using System.Net;
using System.Text;
using System.Text.Json;

namespace F8Translate;

internal sealed class TranslationService : IDisposable
{
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(20) };

    public async Task<string> TranslateAsync(string text, string source, string target, CancellationToken cancellationToken)
    {
        if (Encoding.UTF8.GetByteCount(text) > 500)
            throw new InvalidOperationException("Este provedor aceita até 500 bytes por tradução. Selecione um trecho menor.");
        if (source == target)
            throw new InvalidOperationException("Escolha idiomas de origem e destino diferentes.");
        var url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(text)
            + "&langpair=" + Uri.EscapeDataString($"{source}|{target}");
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = json.RootElement;
        if (!root.TryGetProperty("responseStatus", out var status) || status.ToString() != "200")
            throw new InvalidOperationException("O provedor recusou a tradução ou atingiu o limite de uso. Tente novamente mais tarde.");
        var translated = root.GetProperty("responseData").GetProperty("translatedText").GetString();
        if (string.IsNullOrWhiteSpace(translated))
            throw new InvalidOperationException("O provedor retornou uma tradução vazia.");
        return WebUtility.HtmlDecode(translated);
    }

    public void Dispose() => client.Dispose();
}
