namespace F8Translate;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
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
