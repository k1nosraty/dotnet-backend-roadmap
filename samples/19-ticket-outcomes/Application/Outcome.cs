namespace SupportDesk.Application;

public enum ErrorCode { InvalidInput, NotFound, InvalidState, CapacityReached }
public sealed record AppError(ErrorCode Code, string Message);
public sealed class Outcome<T> where T : class
{
    private readonly T? value;
    public AppError? Error { get; }
    public bool IsSuccess => Error is null;
    public T Value => value ?? throw new InvalidOperationException("Failure has no value.");
    private Outcome(T? value, AppError? error) { this.value = value; Error = error; }
    public static Outcome<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, null);
    }
    public static Outcome<T> Failure(ErrorCode code, string message)
    {
        if (!Enum.IsDefined(code)) throw new ArgumentOutOfRangeException(nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("An error message is required.", nameof(message));
        return new(null, new AppError(code, message));
    }
}
