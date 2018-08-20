using Sweeper.Core.Propagation;
using Sweeper.Noop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Support
{
    public sealed class GlobalTracer : ITracer
    {
        /**
     * Singleton instance.
     * <p>
     * Since we cannot prevent people using {@linkplain #get() GlobalTracer.get()} as a constant,
     * this guarantees that references obtained before, during or after initialization
     * all behave as if obtained <em>after</em> initialization once properly initialized.<br>
     * As a minor additional benefit it makes it harder to circumvent the {@link Tracer} API.
     */
        private static readonly GlobalTracer INSTANCE = new GlobalTracer();

        /**
         * The registered {@link Tracer} delegate or the {@link NoopTracer} if none was registered yet.
         * Never {@code null}.
         */
        private static volatile ITracer tracer = NoopTracerFactory.create();

        private GlobalTracer()
        {
        }

        /**
         * Returns the constant {@linkplain GlobalTracer}.
         * <p>
         * All methods are forwarded to the currently configured tracer.<br>
         * Until a tracer is {@link #register(Tracer) explicitly configured},
         * the {@link io.opentracing.noop.NoopTracer NoopTracer} is used.
         *
         * @return The global tracer constant.
         * @see #register(Tracer)
         */
        public static ITracer get()
        {
            return INSTANCE;
        }

        /**
         * Identify whether a {@link Tracer} has previously been registered.
         * <p>
         * This check is useful in scenarios where more than one component may be responsible
         * for registering a tracer. For example, when using a Java Agent, it will need to determine
         * if the application has already registered a tracer, and if not attempt to resolve and
         * register one itself.
         *
         * @return Whether a tracer has been registered
         */
        public static bool isRegistered()
        {
            return !(GlobalTracer.tracer is NoopTracerImpl);
        }

        /**
         * Register a {@link Tracer} to back the behaviour of the {@link #get() global tracer}.
         * <p>
         * The tracer is provided through a {@linkplain Callable} that will only be called if the global tracer is absent.
         * Registration is a one-time operation. Once a tracer has been registered, all attempts at re-registering
         * will return {@code false}.
         * <p>
         * Every application intending to use the global tracer is responsible for registering it once
         * during its initialization.
         *
         * @param provider Provider for the tracer to use as global tracer.
         * @return {@code true} if the provided tracer was registered as a result of this call,
         * {@code false} otherwise.
         * @throws NullPointerException  if the tracer provider is {@code null} or provides a {@code null} Tracer.
         * @throws RuntimeException      any exception thrown by the provider gets rethrown,
         *                               checked exceptions will be wrapped into appropriate runtime exceptions.
         */
        public static bool registerIfAbsent(Func<ITracer> provider)
        {
            requireNonNull(provider, "Cannot register GlobalTracer from provider <null>.");
            if (!isRegistered())
            {
                try
                {
                    ITracer suppliedTracer = requireNonNull(provider(), "Cannot register GlobalTracer <null>.");
                    if (!(suppliedTracer is GlobalTracer))
                    {
                        GlobalTracer.tracer = suppliedTracer;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception obtaining tracer from provider: " + ex.Message, ex);
                }
            }
            return false;
        }




        public IScopeManager ScopeManager
        {
            get => tracer.ScopeManager;
        }


        public ISpanBuilder buildSpan(String operationName)
        {
            return tracer.buildSpan(operationName);
        }


        public void inject<C>(ISpanContext spanContext, IFormat<C> format, C carrier)
        {
            tracer.inject(spanContext, format, carrier);
        }


        public ISpanContext extract<C>(IFormat<C> format, C carrier)
        {
            return tracer.extract(format, carrier);
        }


        public ISpan activeSpan()
        {
            return tracer.activeSpan();
        }

        public override string ToString()
        {
            return $"{typeof(GlobalTracer).Name}:{tracer}";
        }

        private static T requireNonNull<T>(T value, String message)
        {
            if (value == null)
            {
                throw new Exception(message);
            }
            return value;
        }
    }
}
