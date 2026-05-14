using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace VinhKhanhAudioGuide.Web.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.FirstOrDefault(x => x is T) as T;

        if (argument == null)
        {
            return Results.BadRequest("Request body is missing or invalid.");
        }

        var validationResult = await _validator.ValidateAsync(argument);

        if (!validationResult.IsValid)
        {
            return Results.BadRequest(new
            {
                Errors = validationResult.Errors.Select(x => new
                {
                    Property = x.PropertyName,
                    Message = x.ErrorMessage
                })
            });
        }

        return await next(context);
    }
}
