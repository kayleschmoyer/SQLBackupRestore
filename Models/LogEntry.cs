using System;

namespace SQLBackupRestore.Models
{
    /// <summary>
    /// Represents a log entry for display in the log panel.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the log level (Info, Warning, Error, Success).
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Gets the formatted log entry for display.
        /// </summary>
        public string FormattedMessage => $"[{Timestamp:HH:mm:ss}] {Message}";
    }

    /// <summary>
    /// Log severity levels.
    /// </summary>
    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }
}
