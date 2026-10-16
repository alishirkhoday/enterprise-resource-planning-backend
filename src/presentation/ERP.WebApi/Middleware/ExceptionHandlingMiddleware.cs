using System.Text.Json;
using ERP.Application.Common;
using ERP.WebApi.Controllers;

namespace ERP.WebApi.Middleware;

/// <summary>
/// The last line of the request pipeline. Everything that escapes a handler ends here and leaves
/// as an <see cref="Envelope{T}"/>, so a caller parsing responses never has to handle a second
/// shape that only appears when something went wrong.
/// <para>
/// It replaces a handler that wrote the literal string "Error" with a 200, which told a client
/// nothing and told a monitoring system less.
/// </para>
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, Serilog.ILogger logger, IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException validationException)
        {
            // Thrown by the validation behaviour, so a malformed request is answered the same way
            // whichever endpoint it arrived at.
            await WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                new Error("ValidationFailed", "One or more fields are not valid."),
                validationException.Errors
                    .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray(), StringComparer.Ordinal));
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller hung up. Nothing went wrong and there is nobody left to tell.
            logger.Information("Request {Path} was abandoned by the caller.", context.Request.Path);
        }
        catch (Exception exception)
        {
            // The identifier is logged with the exception and returned to the caller, so a report
            // of "it failed" can be traced to the one entry that explains why.
            var incidentId = context.TraceIdentifier;

            logger.Error(
                exception,
                "Unhandled exception on {Method} {Path}. Incident {IncidentId}",
                context.Request.Method,
                context.Request.Path,
                incidentId);

            // The message is deliberately uninformative outside development: an exception text can
            // name tables, paths and connection details.
            var message = environment.IsDevelopment()
                ? exception.Message
                : $"An unexpected error occurred. Quote incident {incidentId} when reporting it.";

            await WriteAsync(context, StatusCodes.Status500InternalServerError, new Error("UnhandledException", message));
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        Error error,
        IDictionary<string, string[]>? fieldErrors = null)
    {
        // Something already started writing, so the headers are gone and the body is half sent.
        // Anything written now would corrupt what the client is reading.
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var envelope = Envelope<object>.Error(error);

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                new
                {
                    envelope.Data,
                    envelope.ErrorCode,
                    envelope.ErrorMessage,
                    envelope.InvalidField,
                    envelope.TimeGenerated,
                    Errors = fieldErrors
                },
                SerializerOptions));
    }
}
