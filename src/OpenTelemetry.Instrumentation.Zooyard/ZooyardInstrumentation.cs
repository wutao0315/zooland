namespace OpenTelemetry.Instrumentation.Zooyard;

/// <summary>
/// instrumentation.
/// </summary>
internal class ZooyardInstrumentation : IDisposable
{
    private readonly DiagnosticSourceSubscriber? _diagnosticSourceSubscriber;

    public ZooyardInstrumentation(DiagnosticListener diagnosticListener)
    {
        _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(diagnosticListener, null);
        _diagnosticSourceSubscriber.Subscribe();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _diagnosticSourceSubscriber?.Dispose();
    }
}
