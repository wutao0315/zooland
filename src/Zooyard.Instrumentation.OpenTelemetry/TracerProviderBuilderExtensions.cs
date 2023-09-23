using Zooyard.Instrumentation.OpenTelemetry;

namespace OpenTelemetry.Trace
{
    public static class TracerProviderBuilderExtensions
    {
        /// <summary>
        /// Enables the message eventing data collection for CAP.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        public static TracerProviderBuilder AddZooyardInstrumentation(this TracerProviderBuilder builder
            , Action<ZooyardInstrumentationOptions>? configure = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var options = new ZooyardInstrumentationOptions();
            configure?.Invoke(options);

            builder.AddSource(DiagnosticListener.SourceName);

            var instrumentation = new ZooyardInstrumentation(new DiagnosticListener());

            return builder.AddInstrumentation(() => instrumentation);
        }
    }
}