using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Server;

public class JsonifyErrorsAttribute : ExceptionFilterAttribute
{
    public bool RewriteErrors { get; set; }

    public override Task OnExceptionAsync(ExceptionContext context)
    {
        var exception = context.Exception;
        var httpContext = context.HttpContext;
        var services = httpContext.RequestServices;

        if (RewriteErrors) {
            var rewriter = services.GetRequiredService<IErrorRewriter>();
            exception = rewriter.Rewrite(context, exception, true);
        }

        var log = services.GetRequiredService<ILogger<JsonifyErrorsAttribute>>();
        log.LogError(exception, "Error message: {Message}", exception.Message);

        var serializer = TypeDecoratingSerializer.Default;
        var content = serializer.Write(exception.ToExceptionInfo());
        var result = new ContentResult() {
            Content = content,
            ContentType = "application/json",
            StatusCode = (int)HttpStatusCode.InternalServerError,
        };
        context.ExceptionHandled = true;
        return result.ExecuteResultAsync(context);
    }
}
