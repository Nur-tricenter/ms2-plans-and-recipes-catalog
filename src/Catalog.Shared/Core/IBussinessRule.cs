namespace Shared.Core
{
    public interface IBussinessRule
    {
        bool IsValid();

        string Message { get; }
    }
}