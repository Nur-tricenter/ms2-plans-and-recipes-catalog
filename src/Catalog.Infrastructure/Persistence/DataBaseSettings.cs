namespace Catalog.Infrastructure.Persistence
{
    public class DataBaseSettings
    {
        public const string SectionName = "DataBaseSettings";

        public string ConnectionString { get; set; } = null!;
    }
}