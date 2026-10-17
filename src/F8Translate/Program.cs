namespace F8Translate;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--render-preview")
        {
            ApplicationConfiguration.Initialize();
            InterfacePreview.Render(args[1]);
            return;
        }
        using var instance = new Mutex(true, "Local\\F8Translate.Desktop", out var first);
        if (!first)
        {
            MessageBox.Show("O F8 Translate já está aberto. Procure o ícone na bandeja.");
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
