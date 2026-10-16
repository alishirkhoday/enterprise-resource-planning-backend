namespace ERP.Application.Common
{
    /// <summary>
    /// A business outcome the caller asked for and did not get.
    /// <para>
    /// Compared by <see cref="Code"/>, and that matters. It used to be a plain class, so
    /// <c>result.Error == Errors.General.NotFound()</c> compared references against a freshly
    /// built instance and was always false - which meant nothing was ever answered with a 404.
    /// The message is for a person to read and may be reworded; the code is what other code
    /// branches on.
    /// </para>
    /// </summary>
    public sealed record Error
    {
        public string Code { get; }
        public string Message { get; }

        public Error(string code, string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
            ArgumentException.ThrowIfNullOrEmpty(message, nameof(message));
            Code = code;
            Message = message;
        }

        public bool Equals(Error? other) => other is not null && string.Equals(Code, other.Code, StringComparison.Ordinal);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Code);

        public override string ToString() => $"{Code}: {Message}";
    }
}
