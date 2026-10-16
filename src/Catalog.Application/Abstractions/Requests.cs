using Catalog.Application.Utils;
using MediatR;

namespace Catalog.Application.Abstractions
{
    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
    }

    public interface ICommand : ICommand<Result>
    {
    }

    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
    }
}
