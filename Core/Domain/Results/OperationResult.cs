namespace Core.Domain.Results
{
    /// <summary>
    /// Encapsula o resultado de uma operação, seguindo o padrão Result.
    /// </summary>
    public sealed class OperationResult
    {
        public bool IsSuccess { get; }
        public string Message  { get; }
        public string? Output  { get; }

        private OperationResult(bool isSuccess, string message, string? output = null)
        {
            IsSuccess = isSuccess;
            Message   = message;
            Output    = output;
        }

        public static OperationResult Success(string message, string? output = null)
            => new(true, message, output);

        public static OperationResult Failure(string message, string? output = null)
            => new(false, message, output);
    }

    /// <summary>
    /// Resultado tipado que carrega dados em caso de sucesso.
    /// </summary>
    public sealed class OperationResult<T>
    {
        public bool   IsSuccess { get; }
        public string Message   { get; }
        public T?     Data      { get; }

        private OperationResult(bool isSuccess, string message, T? data = default)
        {
            IsSuccess = isSuccess;
            Message   = message;
            Data      = data;
        }

        public static OperationResult<T> Success(T data, string message = "")
            => new(true, message, data);

        public static OperationResult<T> Failure(string message)
            => new(false, message);
    }
}
