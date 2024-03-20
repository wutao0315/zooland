using System.IO.Pipelines;

namespace Zooyard.WebSocketsImpl.Connections.Internal;

internal static class ClientPipeOptions
{
    public static PipeOptions DefaultOptions = new PipeOptions(writerScheduler: PipeScheduler.ThreadPool, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false, pauseWriterThreshold: 0, resumeWriterThreshold: 0);
}
