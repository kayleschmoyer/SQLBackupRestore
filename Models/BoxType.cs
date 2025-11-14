namespace SQLBackupRestore.Models
{
    /// <summary>
    /// Specifies the type of SQL Server box based on server name pattern.
    /// </summary>
    public enum BoxType
    {
        /// <summary>
        /// Unknown or general box type (e.g., USATSVASTSQL02).
        /// </summary>
        General,

        /// <summary>
        /// Office box (contains "ST" pattern, e.g., USATSVASTQAST01).
        /// </summary>
        Office,

        /// <summary>
        /// Shop box (contains "SH" pattern, e.g., USATSVASTQASH01).
        /// </summary>
        Shop
    }
}
