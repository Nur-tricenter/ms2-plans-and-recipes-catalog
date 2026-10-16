namespace Shared.Core
{
    public interface IUnitOfWork
    {
        Task Commit();
    }
}