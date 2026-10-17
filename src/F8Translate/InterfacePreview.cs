using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace F8Translate;

/// <summary>Prévias de desenvolvimento, sem rede, clipboard, histórico real ou atalhos.</summary>
internal static class InterfacePreview
{
    internal static void Render(string directory)
    {
        Directory.CreateDirectory(directory);
        var view = new MainView();
        view.Initialize(new Settings { Consent = true });
        view.SetBadge("● F8 ativo");
        view.SetNetwork(true);
        view.SetStatus("Selecione texto e use F8, ou digite aqui e clique em Traduzir.");
        Save(view, Path.Combine(directory, "01-traducao.png"), 1120, 750);
        view.ShowSettings();
        Save(view, Path.Combine(directory, "02-configuracoes.png"), 1120, 750);
        view.ShowTranslation();
        Save(view, Path.Combine(directory, "03-janela-menor.png"), 1000, 660);
    }

    private static void Save(MainView view, string file, int width, int height)
    {
        view.Measure(new System.Windows.Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        image.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(file);
        encoder.Save(stream);
    }
}
