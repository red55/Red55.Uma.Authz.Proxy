namespace Red55.Uma.Authz.Proxy.Middlewares;

using System;
using System.Threading.Tasks;

using EnsureThat;

using Microsoft.AspNetCore.Http;

using Serilog.Events;
/// <summary>
/// Middleware for handling and propagating correlation IDs in HTTP requests and responses.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string _headerName = "X-Correlation-ID";
    private const string _serilogCorrelationIdPropertyName = "CorrelationId";
    /// <summary>
    /// Gets the name of the HTTP header used for the correlation ID.
    /// </summary>
    public static string HeaderName { get; private set; } = _headerName;
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class with the default header name.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = EnsureArg.IsNotNull (next, nameof (next));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class with a custom header name.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    /// <param name="headerName">The custom header name to use for the correlation ID.</param>
    public CorrelationIdMiddleware(RequestDelegate next, string headerName)
    {
        _next = EnsureArg.IsNotNull (next, nameof (next));
        HeaderName = EnsureArg.IsNotNullOrEmpty (headerName);
    }

    /// <summary>
    /// Processes the HTTP request to ensure a correlation ID is present in both request and response headers.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue (HeaderName, out var correlationId))
        {
            if (string.IsNullOrEmpty (context.TraceIdentifier))
            {
                correlationId = context.TraceIdentifier = Guid.NewGuid ().ToString ();
            }
            else
            {
                correlationId = context.TraceIdentifier;
            }
        }
        if (context.Items.TryGetValue (_serilogCorrelationIdPropertyName, out var correlationIdItemProperty)
            && correlationIdItemProperty is LogEventProperty correlationIdProperty)
        {
            correlationId = correlationIdProperty.Value.ToString ();
        }
        else
        {
            context.Items[_serilogCorrelationIdPropertyName] =
                new LogEventProperty (_serilogCorrelationIdPropertyName, new ScalarValue (correlationId.ToString ()));
        }

        context.Request.Headers[HeaderName] = correlationId;
        context.Response.Headers.Append (HeaderName, correlationId);

        return _next (context);
    }
}