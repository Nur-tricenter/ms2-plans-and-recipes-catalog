namespace Catalog.Application.Utils
{
    public class Result
    {
        public bool IsSuccess { get; protected set; }
        public bool IsFailed => !IsSuccess;
        public string? Message { get; protected set; }
        public List<string> Errors { get; protected set; } = new();

        protected Result() { }

        public static Result Ok()
        {
            return new Result { IsSuccess = true };
        }

        public static Result Ok(string message)
        {
            return new Result { IsSuccess = true, Message = message };
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(value) { IsSuccess = true };
        }

        public static Result<T> Ok<T>(T value, string message)
        {
            return new Result<T>(value) { IsSuccess = true, Message = message };
        }

        public static Result Fail(string message)
        {
            return new Result { IsSuccess = false, Message = message, Errors = new List<string> { message } };
        }

        public static Result Fail(string message, IEnumerable<string> errors)
        {
            return new Result { IsSuccess = false, Message = message, Errors = errors.ToList() };
        }

        public static Result<T> Fail<T>(string message)
        {
            return new Result<T>(default) { IsSuccess = false, Message = message, Errors = new List<string> { message } };
        }

        public static Result<T> Fail<T>(string message, IEnumerable<string> errors)
        {
            return new Result<T>(default) { IsSuccess = false, Message = message, Errors = errors.ToList() };
        }
    }

    public class Result<T> : Result
    {
        public T? Value { get; protected set; }

        public Result() { }

        public Result(T? value)
        {
            Value = value;
        }

        public static implicit operator Result<T>(T value)
        {
            return Ok(value);
        }
    }
}
