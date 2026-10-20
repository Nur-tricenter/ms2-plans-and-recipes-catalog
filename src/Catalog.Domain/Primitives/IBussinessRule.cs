namespace Catalog.Domain.Primitives
{
    public interface IBussinessRule
    {
        bool IsValid();

        string Message { get; }
    }
}
