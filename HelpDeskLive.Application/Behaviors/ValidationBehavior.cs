using FluentValidation;
using MediatR;
namespace HelpDeskLive.Application.Behaviors;
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(e => e is not null).ToArray();
        if (failures.Length > 0)
            throw new HelpDeskLive.Application.Exceptions.ValidationException(
                failures.GroupBy(e => e.PropertyName).ToDictionary(
                    g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()));
        return await next();
    }
}