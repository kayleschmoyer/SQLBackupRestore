using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SQLBackupRestore.Models;

namespace SQLBackupRestore.Services
{
    /// <summary>
    /// Service for handling SQL Server database restore operations.
    /// </summary>
    public class SqlRestoreService
    {
        /// <summary>
        /// Event raised when a log message is generated.
        /// </summary>
        public event EventHandler<LogEntry>? LogMessageReceived;

        /// <summary>
        /// Tests the connection to the SQL Server.
        /// </summary>
        /// <param name="serverInstance">SQL Server instance name.</param>
        /// <param name="authType">Authentication type.</param>
        /// <param name="username">Username (if SQL auth).</param>
        /// <param name="password">Password (if SQL auth).</param>
        /// <returns>True if connection is successful, false otherwise.</returns>
        public async Task<bool> TestConnectionAsync(
            string serverInstance,
            AuthenticationType authType,
            string? username = null,
            string? password = null)
        {
            try
            {
                LogMessage("Testing connection to SQL Server...", LogLevel.Info);

                var connectionString = BuildConnectionString(serverInstance, authType, username, password, "master");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                LogMessage($"Successfully connected to {serverInstance}", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Connection failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the logical file names from a backup file.
        /// </summary>
        public async Task<List<FileListItem>> GetBackupFileListAsync(
            string serverInstance,
            AuthenticationType authType,
            string backupFilePath,
            string? username = null,
            string? password = null)
        {
            var fileList = new List<FileListItem>();

            try
            {
                LogMessage($"Reading backup file structure from: {Path.GetFileName(backupFilePath)}", LogLevel.Info);

                var connectionString = BuildConnectionString(serverInstance, authType, username, password, "master");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = @BackupPath", connection);
                command.Parameters.AddWithValue("@BackupPath", backupFilePath);
                command.CommandTimeout = 300; // 5 minutes

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var item = new FileListItem
                    {
                        LogicalName = reader["LogicalName"].ToString() ?? string.Empty,
                        PhysicalName = reader["PhysicalName"].ToString() ?? string.Empty,
                        Type = reader["Type"].ToString() ?? string.Empty,
                        FileGroupName = reader.GetOrdinal("FileGroupName") >= 0 && !reader.IsDBNull(reader.GetOrdinal("FileGroupName"))
                            ? reader["FileGroupName"].ToString() ?? string.Empty
                            : string.Empty,
                        Size = reader.GetOrdinal("Size") >= 0 && !reader.IsDBNull(reader.GetOrdinal("Size"))
                            ? Convert.ToInt64(reader["Size"])
                            : 0
                    };
                    fileList.Add(item);
                }

                LogMessage($"Found {fileList.Count} file(s) in backup", LogLevel.Info);
                foreach (var file in fileList)
                {
                    LogMessage($"  - {file.LogicalName} (Type: {file.Type})", LogLevel.Info);
                }

                return fileList;
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to read backup file structure: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Checks if a database exists on the server.
        /// </summary>
        public async Task<bool> DatabaseExistsAsync(
            string serverInstance,
            AuthenticationType authType,
            string databaseName,
            string? username = null,
            string? password = null)
        {
            try
            {
                var connectionString = BuildConnectionString(serverInstance, authType, username, password, "master");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName",
                    connection);
                command.Parameters.AddWithValue("@DatabaseName", databaseName);

                var count = (int?)await command.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking if database exists: {ex.Message}", LogLevel.Warning);
                return false;
            }
        }

        /// <summary>
        /// Restores a database from a backup file.
        /// </summary>
        public async Task<bool> RestoreDatabaseAsync(
            string serverInstance,
            AuthenticationType authType,
            string backupFilePath,
            string databaseName,
            string dataFilePath,
            string logFilePath,
            bool replaceExisting,
            string? username = null,
            string? password = null)
        {
            try
            {
                // Get file list from backup
                var fileList = await GetBackupFileListAsync(serverInstance, authType, backupFilePath, username, password);

                if (fileList.Count == 0)
                {
                    LogMessage("No files found in backup", LogLevel.Error);
                    return false;
                }

                // Find data and log files
                var dataFile = fileList.FirstOrDefault(f => f.Type == "D");
                var logFile = fileList.FirstOrDefault(f => f.Type == "L");

                if (dataFile == null)
                {
                    LogMessage("No data file found in backup", LogLevel.Error);
                    return false;
                }

                // Ensure directories exist
                LogMessage($"Creating directory: {Path.GetDirectoryName(dataFilePath)}", LogLevel.Info);
                Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath)!);

                if (logFile != null)
                {
                    LogMessage($"Creating directory: {Path.GetDirectoryName(logFilePath)}", LogLevel.Info);
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
                }

                // Build RESTORE command
                var restoreCommand = new StringBuilder();
                restoreCommand.AppendLine($"RESTORE DATABASE [{databaseName}]");
                restoreCommand.AppendLine($"FROM DISK = '{backupFilePath}'");
                restoreCommand.AppendLine("WITH");
                restoreCommand.AppendLine($"  MOVE '{dataFile.LogicalName}' TO '{dataFilePath}',");

                if (logFile != null)
                {
                    restoreCommand.AppendLine($"  MOVE '{logFile.LogicalName}' TO '{logFilePath}',");
                }

                if (replaceExisting)
                {
                    restoreCommand.AppendLine("  REPLACE,");
                }

                restoreCommand.AppendLine("  RECOVERY,");
                restoreCommand.AppendLine("  STATS = 10");

                LogMessage("Starting database restore...", LogLevel.Info);
                LogMessage($"Database: {databaseName}", LogLevel.Info);
                LogMessage($"Data file: {dataFilePath}", LogLevel.Info);
                LogMessage($"Log file: {logFilePath}", LogLevel.Info);

                var connectionString = BuildConnectionString(serverInstance, authType, username, password, "master");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Set up InfoMessage handler to capture RESTORE progress
                connection.InfoMessage += (sender, args) =>
                {
                    foreach (SqlError error in args.Errors)
                    {
                        if (error.Class <= 10) // Informational messages
                        {
                            LogMessage(error.Message, LogLevel.Info);
                        }
                    }
                };

                var command = new SqlCommand(restoreCommand.ToString(), connection)
                {
                    CommandTimeout = 3600 // 1 hour timeout
                };

                await command.ExecuteNonQueryAsync();

                LogMessage($"Database '{databaseName}' restored successfully!", LogLevel.Success);
                LogMessage($"Data file location: {dataFilePath}", LogLevel.Success);
                LogMessage($"Log file location: {logFilePath}", LogLevel.Success);

                return true;
            }
            catch (SqlException sqlEx)
            {
                LogMessage($"SQL Error during restore: {sqlEx.Message}", LogLevel.Error);

                // Log additional SQL error details
                foreach (SqlError error in sqlEx.Errors)
                {
                    LogMessage($"  SQL Error {error.Number}: {error.Message}", LogLevel.Error);
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"Restore failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Detects local SQL Server instances.
        /// </summary>
        public async Task<List<string>> DetectSqlInstancesAsync()
        {
            var instances = new List<string>();

            try
            {
                LogMessage("Detecting SQL Server instances...", LogLevel.Info);

                await Task.Run(() =>
                {
                    // Add common local instances
                    instances.Add("(local)");
                    instances.Add("localhost");
                    instances.Add(@".\SQLEXPRESS");
                    instances.Add(@"(local)\SQLEXPRESS");

                    // Try to detect using SQL Server browser
                    try
                    {
                        var dataTable = SqlDataSourceEnumerator.Instance.GetDataSources();
                        foreach (DataRow row in dataTable.Rows)
                        {
                            var serverName = row["ServerName"]?.ToString() ?? string.Empty;
                            var instanceName = row["InstanceName"]?.ToString() ?? string.Empty;

                            if (!string.IsNullOrEmpty(serverName))
                            {
                                var fullInstance = string.IsNullOrEmpty(instanceName)
                                    ? serverName
                                    : $"{serverName}\\{instanceName}";

                                if (!instances.Contains(fullInstance))
                                {
                                    instances.Add(fullInstance);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors from enumeration
                    }
                });

                LogMessage($"Found {instances.Count} potential instance(s)", LogLevel.Info);
            }
            catch (Exception ex)
            {
                LogMessage($"Error detecting instances: {ex.Message}", LogLevel.Warning);
            }

            return instances;
        }

        /// <summary>
        /// Builds a SQL connection string.
        /// </summary>
        private string BuildConnectionString(
            string serverInstance,
            AuthenticationType authType,
            string? username,
            string? password,
            string database)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = serverInstance,
                InitialCatalog = database,
                TrustServerCertificate = true,
                ConnectTimeout = 30
            };

            if (authType == AuthenticationType.Windows)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = username ?? string.Empty;
                builder.Password = password ?? string.Empty;
            }

            return builder.ConnectionString;
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        private void LogMessage(string message, LogLevel level)
        {
            LogMessageReceived?.Invoke(this, new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                Level = level
            });
        }
    }
}
