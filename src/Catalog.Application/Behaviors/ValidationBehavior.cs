using Catalog.Application.Abstractions;
using Catalog.Application.Utils;
using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Catalog.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);

            if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = validationResults
                    .Select(r => r.ErrorMessage ?? "Validation failed")
                    .ToList();
                var message = string.Join("; ", errors);

                var responseType = typeof(TResponse);
                if (responseType == typeof(Result))
                {
                    return (TResponse)(object)Result.Fail(message, errors);
                }

                if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var failMethod = typeof(Result)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m => m.Name == nameof(Result.Fail)
                            && m.IsGenericMethodDefinition
                            && m.GetParameters().Length == 2)
                        .MakeGenericMethod(responseType.GetGenericArguments()[0]);

                    return (TResponse)failMethod.Invoke(null, new object[] { message, errors })!;
                }
            }

            return await next();
        }
    }
}
