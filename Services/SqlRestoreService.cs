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
                LogMessage("🔌 Testing connection to SQL Server...", LogLevel.Info);
                LogMessage($"   Server: {serverInstance}", LogLevel.Info);
                LogMessage($"   Auth Type: {authType}", LogLevel.Info);

                var connectionString = BuildConnectionString(serverInstance, authType, username, password, "master");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get SQL Server version info
                var versionCommand = new SqlCommand("SELECT @@VERSION", connection);
                var version = await versionCommand.ExecuteScalarAsync();

                LogMessage($"✅ Woohoo! Successfully connected to {serverInstance}!", LogLevel.Success);
                LogMessage($"   Server Version: {version?.ToString()?.Split('\n')[0]}", LogLevel.Info);
                return true;
            }
            catch (SqlException sqlEx)
            {
                LogMessage($"❌ SQL Server connection failed!", LogLevel.Error);
                LogMessage($"   Error Number: {sqlEx.Number}", LogLevel.Error);
                LogMessage($"   Error Message: {sqlEx.Message}", LogLevel.Error);

                // Provide specific troubleshooting guidance based on error number
                switch (sqlEx.Number)
                {
                    case 53:
                    case -1:
                        LogMessage("", LogLevel.Error);
                        LogMessage("🔧 TROUBLESHOOTING TIPS:", LogLevel.Warning);
                        LogMessage("   1. Verify the server name/instance is correct", LogLevel.Warning);
                        LogMessage("      - For named instances use: SERVERNAME\\INSTANCENAME", LogLevel.Warning);
                        LogMessage("      - For default instance use: SERVERNAME", LogLevel.Warning);
                        LogMessage("      - Example: MYSERVER\\SQLEXPRESS or just MYSERVER", LogLevel.Warning);
                        LogMessage("   2. Ensure SQL Server Browser service is running (for named instances)", LogLevel.Warning);
                        LogMessage("   3. Enable TCP/IP protocol in SQL Server Configuration Manager", LogLevel.Warning);
                        LogMessage("   4. Check Windows Firewall settings (allow port 1433 for default, 1434 for Browser)", LogLevel.Warning);
                        LogMessage("   5. Verify 'Allow remote connections' is enabled in SQL Server properties", LogLevel.Warning);
                        break;
                    case 18456:
                        LogMessage("", LogLevel.Error);
                        LogMessage("🔧 TROUBLESHOOTING TIPS:", LogLevel.Warning);
                        LogMessage("   - Login failed - check username and password", LogLevel.Warning);
                        LogMessage("   - For Windows Auth: ensure the current user has access", LogLevel.Warning);
                        LogMessage("   - For SQL Auth: verify SQL Server authentication is enabled", LogLevel.Warning);
                        break;
                    case 4060:
                        LogMessage("", LogLevel.Error);
                        LogMessage("🔧 TROUBLESHOOTING TIPS:", LogLevel.Warning);
                        LogMessage("   - Database not found or access denied", LogLevel.Warning);
                        LogMessage("   - Verify you have permissions to access the database", LogLevel.Warning);
                        break;
                    default:
                        LogMessage("", LogLevel.Error);
                        LogMessage("🔧 TROUBLESHOOTING TIPS:", LogLevel.Warning);
                        LogMessage("   - Check SQL Server error logs for more details", LogLevel.Warning);
                        LogMessage("   - Verify SQL Server service is running", LogLevel.Warning);
                        LogMessage("   - Try connecting with SQL Server Management Studio first", LogLevel.Warning);
                        break;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Unexpected connection error: {ex.Message}", LogLevel.Error);
                LogMessage($"   Exception Type: {ex.GetType().Name}", LogLevel.Error);
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
                LogMessage($"📂 Reading backup file: {Path.GetFileName(backupFilePath)}", LogLevel.Info);

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

                LogMessage($"📋 Found {fileList.Count} file(s) in backup - looking good!", LogLevel.Info);
                foreach (var file in fileList)
                {
                    LogMessage($"  📄 {file.LogicalName} ({file.Type})", LogLevel.Info);
                }

                return fileList;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Failed to read backup file: {ex.Message}", LogLevel.Error);
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
                LogMessage("🔍 Step 1: Analyzing your backup file...", LogLevel.Info);
                var fileList = await GetBackupFileListAsync(serverInstance, authType, backupFilePath, username, password);

                if (fileList.Count == 0)
                {
                    LogMessage("❌ No files found in backup", LogLevel.Error);
                    return false;
                }

                // Find data and log files
                var dataFile = fileList.FirstOrDefault(f => f.Type == "D");
                var logFile = fileList.FirstOrDefault(f => f.Type == "L");

                if (dataFile == null)
                {
                    LogMessage("❌ No data file found in backup", LogLevel.Error);
                    return false;
                }

                // Ensure directories exist
                LogMessage("📁 Step 2: Creating folders for your database...", LogLevel.Info);
                LogMessage($"   Creating: {Path.GetDirectoryName(dataFilePath)}", LogLevel.Info);
                Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath)!);

                if (logFile != null)
                {
                    LogMessage($"   Creating: {Path.GetDirectoryName(logFilePath)}", LogLevel.Info);
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

                LogMessage("🚀 Step 3: Starting the database restore...", LogLevel.Info);
                LogMessage($"   📦 Database: {databaseName}", LogLevel.Info);
                LogMessage($"   💾 Data file: {dataFilePath}", LogLevel.Info);
                LogMessage($"   📝 Log file: {logFilePath}", LogLevel.Info);
                LogMessage("⏳ Please wait while we restore your database (this may take a few minutes)...", LogLevel.Info);

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
                            // Add progress emojis
                            var msg = error.Message;
                            if (msg.Contains("percent"))
                            {
                                LogMessage($"⏳ {msg}", LogLevel.Info);
                            }
                            else
                            {
                                LogMessage($"   {msg}", LogLevel.Info);
                            }
                        }
                    }
                };

                var command = new SqlCommand(restoreCommand.ToString(), connection)
                {
                    CommandTimeout = 3600 // 1 hour timeout
                };

                await command.ExecuteNonQueryAsync();

                LogMessage($"🎉 SUCCESS! Database '{databaseName}' restored successfully!", LogLevel.Success);
                LogMessage($"✅ Data file: {dataFilePath}", LogLevel.Success);
                LogMessage($"✅ Log file: {logFilePath}", LogLevel.Success);
                LogMessage("🎊 All done! Your database is ready to use!", LogLevel.Success);

                return true;
            }
            catch (SqlException sqlEx)
            {
                LogMessage($"❌ SQL Error during restore: {sqlEx.Message}", LogLevel.Error);

                // Log additional SQL error details
                foreach (SqlError error in sqlEx.Errors)
                {
                    LogMessage($"   ⚠️ SQL Error {error.Number}: {error.Message}", LogLevel.Error);
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Restore failed: {ex.Message}", LogLevel.Error);
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

                    // Note: SqlDataSourceEnumerator is not available in modern .NET with Microsoft.Data.SqlClient
                    // Users can manually enter their server instance name if not in the list above
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
            // Normalize server instance name: convert (local) to .
            var normalizedInstance = serverInstance.Replace("(local)", ".", StringComparison.OrdinalIgnoreCase);

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = normalizedInstance,
                InitialCatalog = database,
                Encrypt = false,  // Disable encryption for servers without SSL certificates
                TrustServerCertificate = true,  // Trust server certificate
                ConnectTimeout = 30,  // 30 seconds to establish connection
                ConnectRetryCount = 3,  // Retry connection 3 times
                ConnectRetryInterval = 10,  // Wait 10 seconds between retries
                MultipleActiveResultSets = true,  // Allow multiple result sets
                ApplicationIntent = ApplicationIntent.ReadWrite,  // Specify read/write intent
                Pooling = true,  // Enable connection pooling
                MinPoolSize = 0,
                MaxPoolSize = 100
            };

            if (authType == AuthenticationType.SqlServer)
            {
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
