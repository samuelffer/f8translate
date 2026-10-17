namespace F8Translate;

/// <summary>Janela não ativável: aparecer não rouba o foco nem desfaz a seleção.</summary>
internal sealed class SuggestionPopup : Form
{
    private readonly Button translate = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 8000 };
    private SelectedText? selection;
    internal event Action<SelectedText>? TranslateRequested;
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var p = base.CreateParams; p.ExStyle |= 0x08000000 | 0x00000080; return p; }
    }

    internal SuggestionPopup()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10);
        ClientSize = new Size(286, 48);
        BackColor = Color.FromArgb(35, 48, 72);
        Padding = new Padding(4);
        translate.Dock = DockStyle.Fill;
        translate.FlatStyle = FlatStyle.Flat;
        translate.FlatAppearance.BorderSize = 0;
        translate.BackColor = BackColor;
        translate.ForeColor = Color.White;
        translate.Cursor = Cursors.Hand;
        translate.Click += (_, _) =>
        {
            var captured = selection;
            Hide();
            if (captured != null) TranslateRequested?.Invoke(captured);
        };
        var dismiss = new Button { Text = "×", Dock = DockStyle.Right, Width = 32, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, TabStop = false };
        dismiss.FlatAppearance.BorderSize = 0;
        dismiss.Click += (_, _) => Hide();
        Controls.Add(translate); Controls.Add(dismiss);
        timer.Tick += (_, _) => Hide();
        VisibleChanged += (_, _) => { if (!Visible) { timer.Stop(); selection = null; } };
    }

    internal void Offer(SelectedText selected, string route)
    {
        selection = selected;
        translate.Text = $"Traduzir seleção · {route}";
        var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(Math.Clamp(Cursor.Position.X + 12, screen.Left, Math.Max(screen.Left, screen.Right - Width)),
            Math.Clamp(Cursor.Position.Y + 22, screen.Top, Math.Max(screen.Top, screen.Bottom - Height)));
        Show();
        timer.Stop(); timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
