namespace Catalog.Infrastructure.Persistence
{
    public interface IDatabase
    {
        void Migrate();
    }
}