using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;

namespace F8Translate;

/// <summary>A janela nativa cuida dos atalhos e da bandeja; MainView cuida da apresentação.</summary>
internal sealed class MainForm : Form
{
    private readonly Settings settings = Settings.Load();
    private readonly TranslationService translator = new();
    private readonly MainView view = new();
    private readonly NotifyIcon tray;
    private readonly SuggestionPopup popup = new();
    private SelectionWatcher? watcher;
    private bool busy, exiting, paused, registered;

    public MainForm()
    {
        SuspendLayout();
        Text = "F8 Translate";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 750);
        MinimumSize = new Size(1000, 660);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 248, 252);
        using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("F8Translate.Assets.app.ico"))
            if (stream != null) Icon = new Icon(stream);
        Controls.Add(new ElementHost { Dock = DockStyle.Fill, Child = view });
        // ElementHost encaminha entrada de teclado e acessibilidade para a interface WPF.
        view.Initialize(settings);
        settings.Source = view.SourceCode;
        settings.Target = view.TargetCode;
        settings.Hotkey = view.Hotkey;
        view.MinimizeRequested += () => WindowState = FormWindowState.Minimized;
        view.MaximizeRequested += ToggleMaximize;
        view.CloseRequested += Close;
        view.DragRequested += () =>
        {
            if (WindowState == FormWindowState.Maximized) return;
            Native.ReleaseCapture();
            Native.SendMessage(Handle, 0xA1, 2, 0);
        };
        view.TranslateRequested += () => { if (!busy) _ = TranslateSelectionAsync(manual: true); };
        view.CopyRequested += CopyResult;
        view.SaveRequested += Apply;
        view.RouteChanged += () =>
        {
            if (busy) return;
            settings.Source = view.SourceCode;
            settings.Target = view.TargetCode;
            UpdateObserver();
            SaveSettings();
        };
        view.ConsentWithdrawn += () => { settings.Consent = false; Register(); SaveSettings(); };
        view.SuggestionsWithdrawn += () => { settings.Suggestions = false; UpdateObserver(); SaveSettings(); };
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir F8 Translate", null, (_, _) => Open());
        var pause = new ToolStripMenuItem("Pausar") { CheckOnClick = true };
        pause.CheckedChanged += (_, _) => { paused = pause.Checked; Register(); };
        menu.Items.Add(pause);
        menu.Items.Add("Sair", null, (_, _) =>
        {
            if (busy) { view.SetStatus("Aguarde a tradução terminar antes de sair."); Open(); return; }
            exiting = true; Close();
        });
        tray = new NotifyIcon { Text = "F8 Translate", Icon = Icon, ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => Open();
        popup.TranslateRequested += selected =>
        {
            if (!busy && settings.Consent && !paused) _ = TranslateSelectionAsync(selected);
        };
        Shown += (_, _) =>
        {
            watcher = new SelectionWatcher(selected => PostUi(() => OnSelection(selected)));
            Native.RoundWindow(Handle);
            UpdateNetwork();
            NetworkChange.NetworkAvailabilityChanged += NetworkChanged;
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
        ResumeLayout();
    }

    private void PostUi(Action action)
    {
        if (IsDisposed || Disposing || !IsHandleCreated) return;
        try { BeginInvoke(action); } catch (InvalidOperationException) { }
    }
    private void NetworkChanged(object? sender, NetworkAvailabilityEventArgs e) => PostUi(UpdateNetwork);
    private void UpdateNetwork() => view.SetNetwork(NetworkInterface.GetIsNetworkAvailable());
    private void Open() { Show(); if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal; Activate(); }
    private void ToggleMaximize()
    {
        MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
        WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
    }
    private void CopyResult()
    {
        if (busy || view.ResultText.Length == 0) { view.SetStatus("Traduza um trecho antes de copiar o resultado."); return; }
        try { Clipboard.SetText(view.ResultText); view.SetStatus("Tradução copiada."); }
        catch (ExternalException) { view.SetStatus("Clipboard ocupado. Tente novamente."); }
    }
    private bool SaveSettings()
    {
        try { settings.Save(); return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { view.SetStatus("Não foi possível salvar as configurações."); return false; }
    }
    private void Apply()
    {
        if (busy) { view.SetStatus("Aguarde a tradução terminar."); return; }
        settings.Source = view.SourceCode;
        settings.Target = view.TargetCode;
        settings.Hotkey = view.Hotkey;
        settings.Consent = view.Consent;
        settings.Suggestions = view.Suggestions;
        if (!SaveSettings()) return;
        Register();
        if (settings.Consent) view.ShowTranslation();
    }
    private void UpdateObserver()
    {
        if (watcher != null) watcher.Enabled = settings.Suggestions && settings.Consent && !paused && !busy;
        popup.Hide();
    }
    private string Badge => busy ? "Traduzindo…" : paused ? "Pausado" : !settings.Consent ? "Inativo" : registered ? $"● {settings.Hotkey} ativo" : "Atalho ocupado";
    private void Register()
    {
        Native.UnregisterHotKey(Handle, Native.HotkeyId);
        registered = false;
        UpdateObserver();
        if (paused) { view.SetBadge(Badge); view.SetStatus("Retome pelo ícone na bandeja."); return; }
        if (!settings.Consent) { view.SetBadge(Badge); view.SetStatus("Em Configurações, leia o aviso e permita a tradução online."); return; }
        registered = Native.RegisterHotKey(Handle, Native.HotkeyId, 0x4000, (uint)Enum.Parse<Keys>(settings.Hotkey));
        view.SetBadge(Badge);
        view.SetStatus(registered ? "Selecione texto e use o atalho, ou digite aqui e clique em Traduzir. Ctrl+Enter também traduz." : "Atalho ocupado. Escolha outro em Configurações. A tradução manual continua disponível.");
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
        // Bordas nativas para redimensionar, mesmo com a barra de título personalizada.
        if (m.Msg == 0x84 && m.Result == 1 && WindowState == FormWindowState.Normal)
        {
            long packed = m.LParam.ToInt64();
            var point = PointToClient(new Point((short)(packed & 0xffff), (short)((packed >> 16) & 0xffff)));
            int edge = Math.Max(5, DeviceDpi * 6 / 96);
            bool left = point.X < edge, right = point.X >= ClientSize.Width - edge;
            bool top = point.Y < edge, bottom = point.Y >= ClientSize.Height - edge;
            m.Result = (IntPtr)(top ? (left ? 13 : right ? 14 : 12) : bottom ? (left ? 16 : right ? 17 : 15) : left ? 10 : right ? 11 : 1);
        }
    }
    private async Task TranslateSelectionAsync(SelectedText? selected = null, bool manual = false)
    {
        if (busy) return;
        if (!settings.Consent) { view.ShowSettings(); view.SetStatus("Permita a tradução online e salve antes de enviar texto."); Open(); return; }
        if (paused) { view.SetStatus("O aplicativo está pausado. Retome pelo ícone na bandeja."); return; }
        if (!manual && selected == null && Native.GetForegroundWindow() == Handle)
        { view.SetStatus("Aqui, use o botão Traduzir ou Ctrl+Enter. O atalho global captura texto de outro aplicativo."); return; }
        busy = true;
        UpdateObserver();
        view.SetBusy(true);
        view.SetBadge(Badge);
        var window = selected?.Window ?? Native.GetForegroundWindow();
        var shouldReplace = !manual && selected == null && view.Replace;
        var from = settings.Source;
        var to = settings.Target;
        IDataObject? backup = null;
        uint ownedSequence = 0;
        bool ownsClipboard = false;
        try
        {
            string text;
            if (manual) text = view.InputText;
            else if (selected != null) text = selected.Text;
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
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Digite ou selecione um trecho de texto primeiro.");
            view.SetResult(text, string.Empty);
            view.SetStatus("Consultando o MyMemory…");
            var translated = await translator.TranslateAsync(text, from, to, CancellationToken.None);
            view.SetResult(text, translated);
            view.AddHistory(text, translated, from, to);
            view.ShowTranslation();
            if (shouldReplace)
            {
                if (!settings.Consent || Native.GetForegroundWindow() != window || Native.GetClipboardSequenceNumber() != ownedSequence)
                    throw new InvalidOperationException("Janela, permissão ou clipboard mudou. Nada foi colado; veja o resultado aqui.");
                Clipboard.SetText(translated);
                ownedSequence = Native.GetClipboardSequenceNumber();
                Native.Ctrl(0x56);
                await Task.Delay(300);
                view.SetStatus("Tradução enviada com Ctrl+V. Ctrl+Z depende do aplicativo de destino.");
            }
            else { view.SetStatus("Pronto. Copie a tradução ou continue selecionando textos."); Open(); }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or ExternalException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            view.SetStatus(ex is TaskCanceledException ? "O serviço demorou demais. Tente novamente." :
                ex is HttpRequestException ? "Não foi possível conectar ao serviço. Verifique a internet." :
                ex is System.Text.Json.JsonException ? "Resposta inválida do provedor." : ex.Message);
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
            catch (ExternalException) { view.SetStatus("A tradução terminou, mas não foi possível restaurar o clipboard."); }
            busy = false;
            view.SetBusy(false); view.SetBadge(Badge);
            UpdateObserver();
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NetworkChange.NetworkAvailabilityChanged -= NetworkChanged;
            watcher?.Dispose(); popup.Dispose(); view.ClearSensitiveContent();
            Native.UnregisterHotKey(Handle, Native.HotkeyId);
            tray.Dispose(); translator.Dispose();
        }
        base.Dispose(disposing);
    }
}
