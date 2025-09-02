using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

/// <summary>
/// A component for preloading web resources (CSS, JavaScript, fonts, images, JSON, etc.) 
/// to improve performance by loading them before they are needed.
/// </summary>
/// <example>
/// <para>Preload a CSS file:</para>
/// <code>
/// &lt;Preload Href="/css/styles.css" As="style" /&gt;
/// </code>
/// 
/// <para>Preload a JavaScript file:</para>
/// <code>
/// &lt;Preload Href="/js/app.js" As="script" /&gt;
/// </code>
/// 
/// <para>Preload a web font:</para>
/// <code>
/// &lt;Preload Href="/fonts/font.woff2" As="font" Type="font/woff2" CrossOrigin="anonymous" /&gt;
/// </code>
/// 
/// <para>Preload JSON data:</para>
/// <code>
/// &lt;Preload Href="/api/data.json" As="fetch" Type="application/json" /&gt;
/// </code>
/// 
/// <para>Preload an image:</para>
/// <code>
/// &lt;Preload Href="/images/hero.jpg" As="image" /&gt;
/// </code>
/// </example>
public partial class Preload
{
    /// <summary>
    /// Additional attributes to be applied to the link element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] 
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    /// <summary>
    /// The URL of the resource to preload.
    /// </summary>
    [Parameter] public string Href { get; set; } = "";

    /// <summary>
    /// Whether to append a version query string to the URL for cache busting.
    /// </summary>
    [Parameter] public bool AppendVersion { get; set; } = true;

    /// <summary>
    /// The type of resource being preloaded. Common values include:
    /// <list type="bullet">
    /// <item><term>style</term><description>for CSS files</description></item>
    /// <item><term>script</term><description>for JavaScript files</description></item>
    /// <item><term>font</term><description>for web fonts</description></item>
    /// <item><term>image</term><description>for images</description></item>
    /// <item><term>fetch</term><description>for JSON, API responses, etc.</description></item>
    /// <item><term>document</term><description>for HTML documents</description></item>
    /// <item><term>audio</term><description>for audio files</description></item>
    /// <item><term>video</term><description>for video files</description></item>
    /// </list>
    /// </summary>
    [Parameter] public string As { get; set; } = "";

    /// <summary>
    /// The MIME type of the resource. Required for some resource types like fonts.
    /// <para>Examples: "font/woff2", "application/json", "text/css", "text/javascript"</para>
    /// </summary>
    [Parameter] public string? Type { get; set; }

    /// <summary>
    /// For fonts and cross-origin resources, specifies whether the resource supports cross-origin requests.
    /// <para>Common values: "anonymous", "use-credentials"</para>
    /// </summary>
    [Parameter] public string? CrossOrigin { get; set; }



    [Inject] private IWebHostEnvironment webHost { get; set; } = default!;
    [Inject] private IHttpContextAccessor httpContextAccessor { get; set; } = default!;



    private string? href;
    private Dictionary<string, object> combinedAttributes = [];



    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        href = (Href is not null && AppendVersion) 
                ? BitFileVersionProvider.AppendFileVersion(webHost.WebRootFileProvider, httpContextAccessor?.HttpContext?.Request.PathBase ?? PathString.Empty, Href) 
                : Href;

        // Combine all parameters with additional attributes
        combinedAttributes = new Dictionary<string, object>(AdditionalAttributes ?? []);
        
        if (string.IsNullOrEmpty(As) is false)
        {
            combinedAttributes["as"] = As;
        }

        if (string.IsNullOrEmpty(Type) is false)
        {
            combinedAttributes["type"] = Type;
        }

        if (string.IsNullOrEmpty(CrossOrigin) is false)
        {
            combinedAttributes["crossorigin"] = CrossOrigin;
        }
    }
}
