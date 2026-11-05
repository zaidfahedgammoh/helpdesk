using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace HelpDeskLive.MVC.Services;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is HttpRequestException or SocketException)
        {
            _logger.LogWarning(context.Exception, "Network exception handled by ApiExceptionFilter.");

            context.ExceptionHandled = true;
            context.Result = new ViewResult
            {
                ViewName = "Error",
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                    new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                    context.ModelState)
                {
                    Model = new HelpDeskLive.MVC.Models.ErrorViewModel
                    {
                        RequestId = context.HttpContext.TraceIdentifier
                    }
                },
                StatusCode = 503
            };
        }
    }
}
