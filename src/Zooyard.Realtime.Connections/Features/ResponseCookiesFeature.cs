using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.ObjectPool;
using System.Text;

namespace Zooyard.Realtime.Connections.Features;


/// <summary>
/// Default implementation of <see cref="IResponseCookiesFeature"/>.
/// </summary>
public class ResponseCookiesFeature : IResponseCookiesFeature
{
    private readonly IFeatureCollection _features;
    private IResponseCookies? _cookiesCollection;

    /// <summary>
    /// Initializes a new <see cref="ResponseCookiesFeature"/> instance.
    /// </summary>
    /// <param name="features">
    /// <see cref="IFeatureCollection"/> containing all defined features, including this
    /// <see cref="IResponseCookiesFeature"/> and the <see cref="IHttpResponseFeature"/>.
    /// </param>
    public ResponseCookiesFeature(IFeatureCollection features)
    {
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    /// <summary>
    /// Initializes a new <see cref="ResponseCookiesFeature"/> instance.
    /// </summary>
    /// <param name="features">
    /// <see cref="IFeatureCollection"/> containing all defined features, including this
    /// <see cref="IResponseCookiesFeature"/> and the <see cref="IHttpResponseFeature"/>.
    /// </param>
    /// <param name="builderPool">The <see cref="ObjectPool{T}"/>, if available.</param>
    [Obsolete("This constructor is obsolete and will be removed in a future version.")]
    public ResponseCookiesFeature(IFeatureCollection features, ObjectPool<StringBuilder>? builderPool)
    {
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    /// <inheritdoc />
    public IResponseCookies Cookies
    {
        get
        {
            if (_cookiesCollection == null)
            {
                //_cookiesCollection = new ResponseCookies(_features);
            }

            return _cookiesCollection;
        }
    }
}
