using System;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Thrown when a save-service operation fails.
    /// Inspect <see cref="ErrorCode"/> to determine how the caller should respond.
    /// </summary>
    public sealed class SaveException : Exception
    {
        /// <summary>Machine-readable reason for the failure.</summary>
        public SaveErrorCode ErrorCode { get; }

        /// <summary>
        /// Initialises a new <see cref="SaveException"/>.
        /// </summary>
        /// <param name="errorCode">Category of the failure.</param>
        /// <param name="message">Human-readable description.</param>
        /// <param name="innerException">Original exception, if any.</param>
        public SaveException(SaveErrorCode errorCode, string message, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
