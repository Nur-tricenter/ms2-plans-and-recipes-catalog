namespace Catalog.WebApi.Utils
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse Ok(string? message = null) =>
            new() { Success = true, Message = message };

        public static ApiResponse Fail(string? message = null, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };

        public static ApiResponse<T> Ok<T>(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail<T>(string? message = null, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
    }
}
