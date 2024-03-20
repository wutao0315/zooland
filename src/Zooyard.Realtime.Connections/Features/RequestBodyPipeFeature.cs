using System.IO.Pipelines;

namespace Zooyard.Realtime.Connections.Features;


/// <summary>
/// Default implementation for <see cref="IRequestBodyPipeFeature"/>.
/// </summary>
public class RequestBodyPipeFeature : IRequestBodyPipeFeature
{
    private PipeReader? _internalPipeReader;
    private Stream? _streamInstanceWhenWrapped;
    private readonly HttpContext _context;

    // We want to use zero byte reads for the request body
    private static readonly StreamPipeReaderOptions _defaultReaderOptions = new(useZeroByteReads: true);

    /// <summary>
    /// Initializes a new instance of <see cref="IRequestBodyPipeFeature"/>.
    /// </summary>
    /// <param name="context"></param>
    public RequestBodyPipeFeature(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public PipeReader Reader
    {
        get
        {
            if (_internalPipeReader == null ||
                !ReferenceEquals(_streamInstanceWhenWrapped, _context.Request.Body))
            {
                _streamInstanceWhenWrapped = _context.Request.Body;
                _internalPipeReader = PipeReader.Create(_context.Request.Body, _defaultReaderOptions);

                _context.Response.OnCompleted((self) =>
                {
                    ((PipeReader)self).Complete();
                    return Task.CompletedTask;
                }, _internalPipeReader);
            }

            return _internalPipeReader;
        }
    }
}
