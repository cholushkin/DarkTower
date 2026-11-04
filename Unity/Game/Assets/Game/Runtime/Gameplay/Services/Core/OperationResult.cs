namespace Game
{
    /// Result of a validated gameplay operation. Invalid requests return Fail and must not have
    /// mutated any state. Reason is a short machine-readable code (e.g. "insufficient_funds"),
    /// suitable for a future UI to localize; this layer never formats player-facing text.
    public readonly struct OperationResult
    {
        public readonly bool Success;
        public readonly string Reason;

        private OperationResult(bool success, string reason)
        {
            Success = success;
            Reason = reason;
        }

        public static readonly OperationResult Ok = new OperationResult(true, null);
        public static OperationResult Fail(string reason) => new OperationResult(false, reason);
    }

    /// Same contract as OperationResult, carrying a created/affected entity id or value on success.
    public readonly struct OperationResult<T>
    {
        public readonly bool Success;
        public readonly string Reason;
        public readonly T Value;

        private OperationResult(bool success, string reason, T value)
        {
            Success = success;
            Reason = reason;
            Value = value;
        }

        public static OperationResult<T> Ok(T value) => new OperationResult<T>(true, null, value);
        public static OperationResult<T> Fail(string reason) => new OperationResult<T>(false, reason, default);
    }
}
