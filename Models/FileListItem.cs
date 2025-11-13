namespace SQLBackupRestore.Models
{
    /// <summary>
    /// Represents a logical file from RESTORE FILELISTONLY result.
    /// </summary>
    public class FileListItem
    {
        /// <summary>
        /// Logical name of the file in the backup.
        /// </summary>
        public string LogicalName { get; set; } = string.Empty;

        /// <summary>
        /// Physical name of the file in the backup.
        /// </summary>
        public string PhysicalName { get; set; } = string.Empty;

        /// <summary>
        /// Type of file (D = Data, L = Log).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// File group name.
        /// </summary>
        public string FileGroupName { get; set; } = string.Empty;

        /// <summary>
        /// Size of the file.
        /// </summary>
        public long Size { get; set; }
    }
}
