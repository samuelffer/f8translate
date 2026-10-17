using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace F8Translate;

public partial class MainView : System.Windows.Controls.UserControl
{
    private bool initialized, updating;
    private readonly ObservableCollection<TranslationEntry> history = [];
    private static readonly LanguageOption[] Languages = [
        new("en", "Inglês", "gb"), new("pt", "Português", "pt"), new("es", "Espanhol", "es"),
        new("fr", "Francês", "fr"), new("de", "Alemão", "de"), new("it", "Italiano", "it"), new("ja", "Japonês", "jp")];
    public sealed record LanguageOption(string Code, string Name, string Country)
    {
        public string Flag => $"pack://application:,,,/F8Translate;component/Assets/Flags/{Country}.png";
    }
    public sealed record TranslationEntry(string Original, string Translation, string From, string To, DateTime Time)
    {
        public string Caption => $"{Time:HH:mm}  ·  {From.ToUpperInvariant()} → {To.ToUpperInvariant()}";
    }

    internal event Action? TranslateRequested, CopyRequested, SaveRequested, RouteChanged;
    internal event Action? ConsentWithdrawn, SuggestionsWithdrawn;
    internal event Action? MinimizeRequested, MaximizeRequested, CloseRequested, DragRequested;

    public MainView()
    {
        InitializeComponent();
        HistoryList.ItemsSource = history;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control && TranslationPanel.IsVisible)
            { e.Handled = true; TranslateRequested?.Invoke(); }
        };
    }

    internal void Initialize(Settings settings)
    {
        SourcePicker.ItemsSource = Languages;
        TargetPicker.ItemsSource = Languages;
        SourcePicker.SelectedItem = Languages.FirstOrDefault(x => x.Code == settings.Source) ?? Languages[0];
        TargetPicker.SelectedItem = Languages.FirstOrDefault(x => x.Code == settings.Target) ?? Languages[1];
        HotkeyPicker.ItemsSource = new[] { "F6", "F7", "F8", "F9", "F10", "F11", "F12" };
        HotkeyPicker.SelectedItem = HotkeyPicker.Items.Contains(settings.Hotkey) ? settings.Hotkey : "F8";
        ConsentToggle.IsChecked = settings.Consent;
        SuggestionsToggle.IsChecked = settings.Suggestions;
        ReplaceToggle.IsChecked = false;
        initialized = true;
        if (!settings.Consent) ShowSettings();
    }

    internal string SourceCode => ((LanguageOption)SourcePicker.SelectedItem).Code;
    internal string TargetCode => ((LanguageOption)TargetPicker.SelectedItem).Code;
    internal string Hotkey => (string)HotkeyPicker.SelectedItem;
    internal bool Consent => ConsentToggle.IsChecked == true;
    internal bool Suggestions => SuggestionsToggle.IsChecked == true;
    internal bool Replace => ReplaceToggle.IsChecked == true;
    internal string InputText => InputBox.Text;
    internal string ResultText => OutputBox.Text;

    internal void SetStatus(string text) { StatusText.Text = text; SettingsStatus.Text = text; }
    internal void SetBadge(string text) => HotkeyBadge.Text = text;
    internal void SetNetwork(bool available)
    {
        // Conectividade local não comprova disponibilidade do MyMemory.
        NetworkLabel.Text = available ? "Rede disponível" : "Sem rede";
        NetworkLabel.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(available ? "#19A37F" : "#A28B68"));
        NetworkDot.Fill = NetworkLabel.Foreground;
        NetworkSurface.ToolTip = available ? "Rede detectada pelo Windows. A disponibilidade do tradutor será verificada ao traduzir." : "O Windows não detectou conexão de rede.";
    }
    internal void SetBusy(bool busy)
    {
        TranslateButton.IsEnabled = !busy;
        HistoryList.IsEnabled = !busy;
        CopyButton.IsEnabled = !busy;
        InputBox.IsReadOnly = busy;
        SourcePicker.IsEnabled = TargetPicker.IsEnabled = !busy;
    }
    internal void SetResult(string original, string translated)
    {
        updating = true;
        InputBox.Text = original;
        OutputBox.Text = translated;
        updating = false;
        RefreshTextState();
    }
    internal void AddHistory(string original, string translated, string from, string to)
    {
        history.Insert(0, new(original, translated, from, to, DateTime.Now));
        if (history.Count > 20) history.RemoveAt(history.Count - 1);
        HistoryEmpty.Visibility = Visibility.Collapsed;
    }
    internal void ClearSensitiveContent()
    {
        history.Clear(); InputBox.Clear(); OutputBox.Clear();
    }
    internal void ShowTranslation() => TranslateNav.IsChecked = true;
    internal void ShowSettings() => SettingsNav.IsChecked = true;

    private void RefreshTextState()
    {
        if (InputBox == null || ByteCount == null) return;
        var bytes = Encoding.UTF8.GetByteCount(InputBox.Text);
        ByteCount.Text = $"{bytes}/500 bytes";
        ByteCount.Foreground = bytes > 500 ? System.Windows.Media.Brushes.IndianRed : new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 153, 175));
        InputPlaceholder.Visibility = InputBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        OutputPlaceholder.Visibility = OutputBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void InputChanged(object sender, TextChangedEventArgs e)
    {
        if (!initialized) return;
        if (!updating) OutputBox.Clear();
        RefreshTextState();
    }
    private void LanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!initialized || updating) return;
        OutputBox.Clear(); RefreshTextState(); RouteChanged?.Invoke();
    }
    private void SwapClick(object sender, RoutedEventArgs e)
    {
        if (!SourcePicker.IsEnabled) return;
        updating = true;
        var original = SourcePicker.SelectedItem;
        SourcePicker.SelectedItem = TargetPicker.SelectedItem;
        TargetPicker.SelectedItem = original;
        updating = false;
        OutputBox.Clear(); RefreshTextState(); RouteChanged?.Invoke();
    }
    private void NavigateTranslation(object sender, RoutedEventArgs e) => Navigate(0);
    private void NavigateHistory(object sender, RoutedEventArgs e) => Navigate(1);
    private void NavigateSettings(object sender, RoutedEventArgs e) => Navigate(2);
    private void Navigate(int page)
    {
        if (TranslationPanel == null || HistoryPanel == null || SettingsPanel == null) return;
        TranslationPanel.Visibility = page == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryPanel.Visibility = page == 1 ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = page == 2 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void HistorySelected(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is not TranslationEntry entry) return;
        updating = true;
        SourcePicker.SelectedItem = Languages.First(x => x.Code == entry.From);
        TargetPicker.SelectedItem = Languages.First(x => x.Code == entry.To);
        updating = false;
        RouteChanged?.Invoke(); SetResult(entry.Original, entry.Translation); ShowTranslation();
        HistoryList.SelectedItem = null;
    }
    private void ClearHistoryClick(object sender, RoutedEventArgs e) { history.Clear(); HistoryEmpty.Visibility = Visibility.Visible; }
    private void ConsentRevoked(object sender, RoutedEventArgs e) { if (initialized) ConsentWithdrawn?.Invoke(); }
    private void SuggestionsDisabled(object sender, RoutedEventArgs e) { if (initialized) SuggestionsWithdrawn?.Invoke(); }
    private void TranslateClick(object sender, RoutedEventArgs e) => TranslateRequested?.Invoke();
    private void CopyClick(object sender, RoutedEventArgs e) => CopyRequested?.Invoke();
    private void SaveClick(object sender, RoutedEventArgs e) => SaveRequested?.Invoke();
    private void PrivacyClick(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://mymemory.translated.net/doc/en/privacy.php") { UseShellExecute = true });
    private void TitleDrag(object sender, MouseButtonEventArgs e)
    {
        // Botões da barra não devem iniciar arraste ao receberem o mesmo evento.
        var source = e.OriginalSource as DependencyObject;
        while (source != null)
        {
            if (source is System.Windows.Controls.Button) return;
            source = source is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
        if (e.ClickCount == 2) MaximizeRequested?.Invoke(); else DragRequested?.Invoke();
    }
    private void MinimizeClick(object sender, RoutedEventArgs e) => MinimizeRequested?.Invoke();
    private void MaximizeClick(object sender, RoutedEventArgs e) => MaximizeRequested?.Invoke();
    private void CloseClick(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
}
