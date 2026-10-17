using System.Runtime.InteropServices;

namespace F8Translate;

internal sealed class MainForm : Form
{
    private readonly Settings settings = Settings.Load();
    private readonly TranslationService translator = new();
    private readonly ComboBox source = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly ComboBox target = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly ComboBox hotkey = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly CheckBox consent = new() { Text = "Autorizo enviar o texto selecionado ao MyMemory.", AutoSize = true };
    private readonly CheckBox replace = new() { Text = "Substituir a seleção automaticamente (cuidado)", AutoSize = true };
    private readonly TextBox result = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Width = 510, Height = 120 };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(510, 0) };
    private readonly NotifyIcon tray;
    private bool busy, exiting, paused, registered;
    private static readonly string[] Languages = ["pt", "en", "es", "fr", "de", "it", "ja"];

    public MainForm()
    {
        Text = "F8 Translate";
        ClientSize = new Size(560, 590);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20), AutoScroll = true };
        Controls.Add(layout);
        void Add(Control control) => layout.Controls.Add(control);
        Add(new Label { Text = "Selecione um texto em outro aplicativo e pressione o atalho.", AutoSize = true });
        Add(new Label { Text = "Idioma de origem", AutoSize = true }); Add(source);
        Add(new Label { Text = "Idioma de destino", AutoSize = true }); Add(target);
        Add(new Label { Text = "Atalho global", AutoSize = true }); Add(hotkey);
        source.Items.AddRange(Languages); target.Items.AddRange(Languages);
        hotkey.Items.AddRange(["F6", "F7", "F8", "F9", "F10", "F11", "F12"]);
        source.SelectedItem = Languages.Contains(settings.Source) ? settings.Source : "pt";
        target.SelectedItem = Languages.Contains(settings.Target) ? settings.Target : "en";
        hotkey.SelectedItem = hotkey.Items.Contains(settings.Hotkey) ? settings.Hotkey : "F8";
        settings.Source = (string)source.SelectedItem!;
        settings.Target = (string)target.SelectedItem!;
        settings.Hotkey = (string)hotkey.SelectedItem!;
        consent.Checked = settings.Consent;
        Add(new Label { Text = "Tradução online: MyMemory (terceiro). Limite de 500 bytes por trecho e cotas do serviço. Não envie senhas nem dados sensíveis. O app não registra os textos; o provedor tem sua própria política de dados.", AutoSize = true, MaximumSize = new Size(510, 0) });
        var privacy = new LinkLabel { Text = "Política de privacidade do provedor", AutoSize = true };
        privacy.LinkClicked += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://mymemory.translated.net/doc/en/privacy.php") { UseShellExecute = true });
        Add(privacy); Add(consent); Add(replace);
        var apply = new Button { Text = "Salvar e ativar", AutoSize = true };
        apply.Click += (_, _) => Apply(); Add(apply);
        Add(result);
        var copy = new Button { Text = "Copiar tradução", AutoSize = true };
        copy.Click += (_, _) =>
        {
            if (busy || result.TextLength == 0) return;
            try { Clipboard.SetText(result.Text); status.Text = "Tradução copiada."; }
            catch (ExternalException) { status.Text = "Clipboard ocupado. Tente novamente."; }
        };
        Add(copy); Add(status);
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => Open());
        var pause = new ToolStripMenuItem("Pausar") { CheckOnClick = true };
        pause.CheckedChanged += (_, _) => { paused = pause.Checked; Register(); };
        menu.Items.Add(pause);
        menu.Items.Add("Sair", null, (_, _) =>
        {
            if (busy) { status.Text = "Aguarde a tradução terminar antes de sair."; Open(); return; }
            exiting = true; Close();
        });
        tray = new NotifyIcon { Text = "F8 Translate", Icon = SystemIcons.Application, ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => Open();
        Shown += (_, _) => Register();
        FormClosing += (_, e) => { if (!exiting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } };
    }

    private void Open() { Show(); WindowState = FormWindowState.Normal; Activate(); }

    private void Apply()
    {
        if (busy) { status.Text = "Aguarde a tradução terminar."; return; }
        settings.Source = (string)source.SelectedItem!;
        settings.Target = (string)target.SelectedItem!;
        settings.Hotkey = (string)hotkey.SelectedItem!;
        settings.Consent = consent.Checked;
        try { settings.Save(); Register(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { status.Text = "Não foi possível salvar as configurações."; }
    }

    private void Register()
    {
        Native.UnregisterHotKey(Handle, Native.HotkeyId);
        registered = false;
        if (paused) { status.Text = "Pausado pela bandeja."; return; }
        if (!settings.Consent) { status.Text = "Leia o aviso de privacidade e autorize antes de ativar."; return; }
        registered = Native.RegisterHotKey(Handle, Native.HotkeyId, 0x4000, (uint)Enum.Parse<Keys>(settings.Hotkey));
        status.Text = registered ? $"Ativo: {settings.Hotkey}. Fechar esta janela mantém o app na bandeja." : "Atalho ocupado por outro programa. Escolha outro e salve.";
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == Native.HotkeyMessage && m.WParam == Native.HotkeyId && registered && !busy)
            _ = TranslateSelectionAsync();
    }

    private async Task TranslateSelectionAsync()
    {
        if (Native.GetForegroundWindow() == Handle) { status.Text = "Selecione o texto em outro aplicativo primeiro."; return; }
        busy = true;
        var window = Native.GetForegroundWindow();
        var shouldReplace = replace.Checked;
        IDataObject? backup = null;
        uint ownedSequence = 0;
        bool ownsClipboard = false;
        try
        {
            // Materialize all available formats before another app replaces the clipboard.
            var original = Clipboard.GetDataObject();
            var snapshot = new DataObject();
            if (original != null)
                foreach (var format in original.GetFormats(false))
                {
                    var data = original.GetData(format, false);
                    if (data != null) snapshot.SetData(format, data);
                }
            backup = snapshot;
            var previous = Native.GetClipboardSequenceNumber();
            // Let the user release F8 before injecting Ctrl+C.
            await Task.Delay(180);
            if (Native.GetForegroundWindow() != window) throw new InvalidOperationException("A janela mudou. Tente novamente.");
            Native.Ctrl(0x43);
            for (var i = 0; i < 20 && Native.GetClipboardSequenceNumber() == previous; i++)
                await Task.Delay(50);
            ownedSequence = Native.GetClipboardSequenceNumber();
            if (ownedSequence == previous) throw new InvalidOperationException("Não foi possível copiar a seleção. Verifique se o aplicativo aceita Ctrl+C.");
            ownsClipboard = true;
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Selecione um trecho de texto primeiro.");
            status.Text = "Traduzindo…";
            var translated = await translator.TranslateAsync(text, settings.Source, settings.Target, CancellationToken.None);
            result.Text = translated;
            if (shouldReplace)
            {
                if (Native.GetForegroundWindow() != window || Native.GetClipboardSequenceNumber() != ownedSequence)
                    throw new InvalidOperationException("Janela ou clipboard mudou. Nada foi colado; a tradução está disponível na janela do F8 Translate.");
                Clipboard.SetText(translated);
                ownedSequence = Native.GetClipboardSequenceNumber();
                Native.Ctrl(0x56);
                await Task.Delay(300);
                status.Text = "Tradução enviada com Ctrl+V. Para desfazer, use Ctrl+Z no aplicativo de destino.";
            }
            else { status.Text = "Tradução pronta. Use Copiar tradução para levar o resultado."; Open(); }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or ExternalException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            status.Text = ex is TaskCanceledException ? "O serviço demorou demais. Tente novamente." :
                ex is HttpRequestException ? "Falha de conexão com o provedor de tradução." :
                ex is System.Text.Json.JsonException ? "Resposta inválida do provedor." : ex.Message;
            Open();
        }
        finally
        {
            try
            {
                // Never overwrite clipboard changes made by the user during the request.
                if (ownsClipboard && Native.GetClipboardSequenceNumber() == ownedSequence && backup != null)
                    {
                    if (backup.GetFormats(false).Length == 0) Clipboard.Clear();
                    else Clipboard.SetDataObject(backup, true);
                }
            }
            catch (Exception ex) when (ex is ExternalException)
            { status.Text += " Não foi possível restaurar o clipboard."; }
            busy = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Native.UnregisterHotKey(Handle, Native.HotkeyId);
            tray.Dispose(); translator.Dispose();
        }
        base.Dispose(disposing);
    }
}
