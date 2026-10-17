using System.Runtime.InteropServices;

namespace F8Translate;

internal sealed class MainForm : Form
{
    private static readonly Color Ink = Color.FromArgb(30, 41, 59);
    private static readonly Color Muted = Color.FromArgb(100, 116, 139);
    private static readonly Color Accent = Color.FromArgb(49, 92, 222);
    private readonly Settings settings = Settings.Load();
    private readonly TranslationService translator = new();
    private readonly ComboBox source = Picker(), target = Picker(), hotkey = Picker();
    private readonly CheckBox consent = Toggle("Permitir tradução online pelo MyMemory");
    private readonly CheckBox suggestions = Toggle("Sugerir tradução ao selecionar texto");
    private readonly CheckBox replace = Toggle("No atalho, substituir o texto em vez de pré-visualizar");
    private readonly TextBox result = new()
    {
        Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
        BorderStyle = BorderStyle.None, BackColor = Color.White, ForeColor = Ink, Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 12), PlaceholderText = "Sua tradução aparece aqui."
    };
    private readonly Label status = new() { AutoSize = true, ForeColor = Muted, MaximumSize = new Size(530, 0) };
    private readonly Label badge = new() { AutoSize = true, ForeColor = Accent, Padding = new Padding(0, 6, 0, 0) };
    private readonly NotifyIcon tray;
    private readonly SuggestionPopup popup = new();
    private SelectionWatcher? watcher;
    private bool busy, exiting, paused, registered;
    private static readonly Language[] Languages = [new("pt", "Português"), new("en", "Inglês"), new("es", "Espanhol"), new("fr", "Francês"), new("de", "Alemão"), new("it", "Italiano"), new("ja", "Japonês")];
    private sealed record Language(string Code, string Name) { public override string ToString() => Name; }
    private static ComboBox Picker() => new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Width = 155 };
    private static CheckBox Toggle(string text) => new() { Text = text, AutoSize = true, ForeColor = Ink, Margin = new Padding(0, 5, 0, 5) };
    private static Label Caption(string text) => new() { Text = text, AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 0, 0, 7) };
    private static Button Button(string text, bool primary = false)
    {
        var button = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = primary ? Accent : Color.White, ForeColor = primary ? Color.White : Ink,
            Padding = new Padding(14, 7, 14, 7), Margin = new Padding(0, 0, 10, 0) };
        button.FlatAppearance.BorderColor = Color.FromArgb(222, 228, 237);
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        return button;
    }

    public MainForm()
    {
        Text = "F8 Translate";
        ClientSize = new Size(600, 730);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(246, 248, 252);
        ForeColor = Ink;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(30, 24, 22, 20), AutoScroll = true };
        Controls.Add(layout);
        void Add(Control control, int bottom = 12) { control.Margin = new Padding(0, 0, 0, bottom); layout.Controls.Add(control); }
        Add(new Label { Text = "F8 Translate", Font = new Font("Segoe UI", 23, FontStyle.Bold), AutoSize = true }, 2);
        Add(Caption("Menos interrupções. Mais entendimento."), 18);
        source.Items.AddRange(Languages); target.Items.AddRange(Languages);
        hotkey.Items.AddRange(["F6", "F7", "F8", "F9", "F10", "F11", "F12"]);
        source.SelectedItem = Languages.FirstOrDefault(x => x.Code == settings.Source) ?? Languages[1];
        target.SelectedItem = Languages.FirstOrDefault(x => x.Code == settings.Target) ?? Languages[0];
        hotkey.SelectedItem = hotkey.Items.Contains(settings.Hotkey) ? settings.Hotkey : "F8";
        settings.Source = ((Language)source.SelectedItem!).Code;
        settings.Target = ((Language)target.SelectedItem!).Code;
        settings.Hotkey = (string)hotkey.SelectedItem!;
        consent.Checked = settings.Consent;
        suggestions.Checked = settings.Suggestions;
        var languages = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        foreach (var item in new[] { ("DE", source), ("PARA", target), ("ATALHO", hotkey) })
        {
            var column = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 20, 0) };
            column.Controls.Add(Caption(item.Item1)); column.Controls.Add(item.Item2); languages.Controls.Add(column);
        }
        Add(languages, 18);
        var output = new Panel { Width = 530, Height = 175, BackColor = Color.White, Padding = new Padding(16) };
        output.Controls.Add(result); Add(output);
        var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        var copy = Button("Copiar tradução");
        copy.Click += (_, _) =>
        {
            if (busy || result.TextLength == 0) return;
            try { Clipboard.SetText(result.Text); status.Text = "Tradução copiada."; }
            catch (ExternalException) { status.Text = "Clipboard ocupado. Tente novamente."; }
        };
        actions.Controls.Add(copy); actions.Controls.Add(badge); Add(actions, 18);
        Add(consent, 3); Add(suggestions, 3); Add(replace, 12);
        Add(new Label { Text = "A sugestão é local: só enviamos o trecho quando você clica ou usa o atalho. Até 500 bytes por tradução. Não envie dados sensíveis.", ForeColor = Muted, AutoSize = true, MaximumSize = new Size(530, 0) }, 5);
        var privacy = new LinkLabel { Text = "Privacidade do MyMemory ↗", AutoSize = true, LinkColor = Accent };
        privacy.LinkClicked += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://mymemory.translated.net/doc/en/privacy.php") { UseShellExecute = true });
        Add(privacy, 16);
        var apply = Button("Salvar e ativar", true);
        apply.Click += (_, _) => Apply(); Add(apply, 12); Add(status);
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir F8 Translate", null, (_, _) => Open());
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
        popup.TranslateRequested += selected =>
        {
            if (!busy && settings.Consent && !paused) _ = TranslateSelectionAsync(selected);
        };
        Shown += (_, _) =>
        {
            watcher = new SelectionWatcher(selected =>
            {
                if (IsDisposed || Disposing) return;
                try { BeginInvoke((Action)(() => OnSelection(selected))); }
                catch (InvalidOperationException) { }
            });
            Register();
        };
        FormClosing += (_, e) =>
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; Hide();
                tray.ShowBalloonTip(2500, "F8 Translate continua ativo", "Use o atalho ou selecione texto. Para encerrar, escolha Sair na bandeja.", ToolTipIcon.Info);
            }
        };
        // Revogar uma permissão tem efeito imediato, mesmo antes de salvar.
        consent.CheckedChanged += (_, _) => { if (!consent.Checked) { settings.Consent = false; Register(); } };
        suggestions.CheckedChanged += (_, _) => { if (!suggestions.Checked) { settings.Suggestions = false; UpdateObserver(); } };
    }

    private void Open() { Show(); WindowState = FormWindowState.Normal; Activate(); }

    private void Apply()
    {
        if (busy) { status.Text = "Aguarde a tradução terminar."; return; }
        settings.Source = ((Language)source.SelectedItem!).Code;
        settings.Target = ((Language)target.SelectedItem!).Code;
        settings.Hotkey = (string)hotkey.SelectedItem!;
        settings.Consent = consent.Checked;
        settings.Suggestions = suggestions.Checked;
        try { settings.Save(); Register(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { status.Text = "Não foi possível salvar as configurações."; }
    }

    private void UpdateObserver()
    {
        if (watcher != null) watcher.Enabled = settings.Suggestions && settings.Consent && !paused && !busy;
        popup.Hide();
    }

    private void Register()
    {
        Native.UnregisterHotKey(Handle, Native.HotkeyId);
        registered = false;
        UpdateObserver();
        if (paused) { badge.Text = "Pausado"; status.Text = "Retome pelo ícone na bandeja."; return; }
        if (!settings.Consent) { badge.Text = "Inativo"; status.Text = "Leia o aviso, permita a tradução online e salve para começar."; return; }
        registered = Native.RegisterHotKey(Handle, Native.HotkeyId, 0x4000, (uint)Enum.Parse<Keys>(settings.Hotkey));
        badge.Text = registered ? $"●  {settings.Hotkey} ativo" : "Atalho ocupado";
        status.Text = registered ? "Fechar no X mantém o app ativo na bandeja." : "Escolha outro atalho e salve. As sugestões continuam disponíveis se ativadas.";
    }

    private void OnSelection(SelectedText? selected)
    {
        if (selected == null || busy || paused || !settings.Consent || !settings.Suggestions
            || Native.GetForegroundWindow() != selected.Window) { popup.Hide(); return; }
        popup.Offer(selected, $"{settings.Source.ToUpperInvariant()} → {settings.Target.ToUpperInvariant()}");
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == Native.HotkeyMessage && m.WParam == Native.HotkeyId && registered && !busy)
            _ = TranslateSelectionAsync();
    }

    private async Task TranslateSelectionAsync(SelectedText? selected = null)
    {
        if (selected == null && Native.GetForegroundWindow() == Handle) { status.Text = "Selecione texto em outro aplicativo primeiro."; return; }
        busy = true;
        UpdateObserver();
        var window = selected?.Window ?? Native.GetForegroundWindow();
        // Uma sugestão nunca substitui o documento: a seleção pode já ter mudado.
        var shouldReplace = selected == null && replace.Checked;
        var from = settings.Source;
        var to = settings.Target;
        IDataObject? backup = null;
        uint ownedSequence = 0;
        bool ownsClipboard = false;
        result.Clear();
        badge.Text = "Traduzindo…";
        try
        {
            string text;
            if (selected != null) text = selected.Text;
            else
            {
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
                await Task.Delay(180);
                if (Native.GetForegroundWindow() != window) throw new InvalidOperationException("A janela mudou. Tente novamente.");
                Native.Ctrl(0x43);
                for (var i = 0; i < 20 && Native.GetClipboardSequenceNumber() == previous; i++) await Task.Delay(50);
                ownedSequence = Native.GetClipboardSequenceNumber();
                if (ownedSequence == previous) throw new InvalidOperationException("Não foi possível copiar. Selecione texto em um aplicativo que aceite Ctrl+C.");
                ownsClipboard = true;
                text = Clipboard.GetText();
            }
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Selecione um trecho de texto primeiro.");
            status.Text = "Consultando o MyMemory…";
            var translated = await translator.TranslateAsync(text, from, to, CancellationToken.None);
            result.Text = translated;
            if (shouldReplace)
            {
                if (!settings.Consent || Native.GetForegroundWindow() != window || Native.GetClipboardSequenceNumber() != ownedSequence)
                    throw new InvalidOperationException("Janela, permissão ou clipboard mudou. Nada foi colado; veja o resultado aqui.");
                Clipboard.SetText(translated);
                ownedSequence = Native.GetClipboardSequenceNumber();
                Native.Ctrl(0x56);
                await Task.Delay(300);
                status.Text = "Tradução enviada com Ctrl+V. Ctrl+Z depende do aplicativo de destino.";
            }
            else { status.Text = "Pronto. Copie a tradução ou continue selecionando textos."; Open(); }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or ExternalException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            status.Text = ex is TaskCanceledException ? "O serviço demorou demais. Tente novamente." :
                ex is HttpRequestException ? "Não foi possível conectar ao serviço. Verifique a internet." :
                ex is System.Text.Json.JsonException ? "Resposta inválida do provedor." : ex.Message;
            Open();
        }
        finally
        {
            try
            {
                if (ownsClipboard && Native.GetClipboardSequenceNumber() == ownedSequence && backup != null)
                {
                    if (backup.GetFormats(false).Length == 0) Clipboard.Clear();
                    else Clipboard.SetDataObject(backup, true);
                }
            }
            catch (ExternalException) { status.Text += " Não foi possível restaurar o clipboard."; }
            busy = false;
            badge.Text = paused ? "Pausado" : !settings.Consent ? "Inativo" : registered ? $"●  {settings.Hotkey} ativo" : "Atalho ocupado";
            UpdateObserver();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            watcher?.Dispose(); popup.Dispose();
            Native.UnregisterHotKey(Handle, Native.HotkeyId);
            tray.Dispose(); translator.Dispose();
        }
        base.Dispose(disposing);
    }
}
