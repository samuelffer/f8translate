using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace F8Translate;

internal sealed record SelectedText(IntPtr Window, string Text);

/// <summary>Observação local e limitada: não lê clipboard nem envia texto à rede.</summary>
internal sealed class SelectionWatcher : IDisposable
{
    private readonly Action<SelectedText?> changed;
    private readonly AutoResetEvent wake = new(false);
    private readonly Thread worker;
    private volatile bool stopping;
    internal volatile bool Enabled;
    private readonly int ownProcess = Environment.ProcessId;

    internal SelectionWatcher(Action<SelectedText?> changed)
    {
        this.changed = changed;
        worker = new Thread(Observe) { IsBackground = true, Name = "F8 selection observer" };
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
    }

    private void Observe()
    {
        SelectedText? previous = null;
        try
        {
            while (!stopping)
            {
                wake.WaitOne(1000);
                if (stopping) break;
                SelectedText? current = null;
                if (Enabled)
                {
                    try { current = ReadSelection(); }
                    // Provedores de acessibilidade podem desaparecer ou negar acesso.
                    catch (Exception ex) when (ex is ElementNotAvailableException or ElementNotEnabledException
                        or InvalidOperationException or NotSupportedException or UnauthorizedAccessException
                        or COMException or ArgumentException) { }
                }
                if (current != previous)
                {
                    previous = current;
                    changed(current);
                }
            }
        }
        finally { wake.Dispose(); }
    }

    private SelectedText? ReadSelection()
    {
        var window = Native.GetForegroundWindow();
        if (window == IntPtr.Zero) return null;
        var element = AutomationElement.FocusedElement;
        if (element == null || element.Current.ProcessId == ownProcess || element.Current.IsPassword) return null;
        for (var depth = 0; element != null && depth < 3; depth++)
        {
            if (element.Current.IsPassword) return null;
            if (element.TryGetCurrentPattern(TextPattern.Pattern, out var value))
            {
                var selections = ((TextPattern)value).GetSelection();
                if (selections.Length != 1) return null;
                var text = selections[0].GetText(501);
                if (string.IsNullOrWhiteSpace(text) || Encoding.UTF8.GetByteCount(text) > 500) return null;
                if (Native.GetForegroundWindow() != window) return null;
                return new SelectedText(window, text);
            }
            element = TreeWalker.ControlViewWalker.GetParent(element);
        }
        return null;
    }

    public void Dispose()
    {
        // Não bloquear a UI esperando um provedor externo de acessibilidade.
        stopping = true;
        try { wake.Set(); } catch (ObjectDisposedException) { }
    }
}
