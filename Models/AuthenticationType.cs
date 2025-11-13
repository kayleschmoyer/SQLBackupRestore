namespace SQLBackupRestore.Models
{
    /// <summary>
    /// Specifies the type of authentication to use for SQL Server connection.
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// Use Windows Authentication (integrated security).
        /// </summary>
        Windows,

        /// <summary>
        /// Use SQL Server Authentication (username and password).
        /// </summary>
        SqlServer
    }
}
