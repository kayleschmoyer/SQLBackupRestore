using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SQLBackupRestore.Helpers;
using SQLBackupRestore.Models;
using SQLBackupRestore.Services;

namespace SQLBackupRestore.ViewModels
{
    /// <summary>
    /// Main view model for the application.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SqlRestoreService _sqlRestoreService;

        // SQL Connection fields
        private string _sqlServerInstance = "VastOffice"; // Default to VastOffice instance
        private AuthenticationType _authenticationType = AuthenticationType.Windows;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _connectionStatus = string.Empty;
        private bool _isConnectionSuccessful;
        private string _currentWindowsUser = string.Empty;

        // Backup file fields
        private string _backupFilePath = string.Empty;
        private string _backupBaseName = string.Empty;

        // Database type fields
        private DatabaseType _databaseType = DatabaseType.Office;

        // Database details fields
        private string _databaseName = string.Empty;
        private string _customFolderName = string.Empty;
        private string _dataFileFolder = string.Empty;
        private string _logFileFolder = string.Empty;

        // Operation state fields
        private bool _isRestoring;
        private bool _isRestoreComplete;
        private double _progressValue;
        private bool _isProgressIndeterminate = true;
        private bool _replaceExistingDatabase;

        // Log entries
        private ObservableCollection<LogEntry> _logEntries = new();

        public MainViewModel()
        {
            _sqlRestoreService = new SqlRestoreService();
            _sqlRestoreService.LogMessageReceived += OnLogMessageReceived;

            // Initialize commands
            TestConnectionCommand = new RelayCommand(async _ => await TestConnectionAsync(), _ => CanTestConnection());
            BrowseBackupFileCommand = new RelayCommand(_ => BrowseBackupFile());
            RestoreDatabaseCommand = new RelayCommand(async _ => await RestoreDatabaseAsync(), _ => CanRestoreDatabase());

            // Detect current Windows user
            _currentWindowsUser = Environment.UserName;

            // Initialize - detect SQL instances and auto-connect
            _ = InitializeAsync();
        }

        #region Properties

        /// <summary>
        /// SQL Server instance name.
        /// </summary>
        public string SqlServerInstance
        {
            get => _sqlServerInstance;
            set
            {
                if (SetProperty(ref _sqlServerInstance, value))
                {
                    ConnectionStatus = string.Empty;
                    IsConnectionSuccessful = false;
                }
            }
        }

        /// <summary>
        /// Authentication type (Windows or SQL Server).
        /// </summary>
        public AuthenticationType AuthenticationType
        {
            get => _authenticationType;
            set
            {
                if (SetProperty(ref _authenticationType, value))
                {
                    OnPropertyChanged(nameof(IsSqlAuthentication));
                    ConnectionStatus = string.Empty;
                    IsConnectionSuccessful = false;
                }
            }
        }

        /// <summary>
        /// Gets whether SQL authentication is selected.
        /// </summary>
        public bool IsSqlAuthentication => AuthenticationType == AuthenticationType.SqlServer;

        /// <summary>
        /// Username for SQL authentication.
        /// </summary>
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ConnectionStatus = string.Empty;
                    IsConnectionSuccessful = false;
                }
            }
        }

        /// <summary>
        /// Password for SQL authentication.
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ConnectionStatus = string.Empty;
                    IsConnectionSuccessful = false;
                }
            }
        }

        /// <summary>
        /// Connection test status message.
        /// </summary>
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        /// <summary>
        /// Whether the connection test was successful.
        /// </summary>
        public bool IsConnectionSuccessful
        {
            get => _isConnectionSuccessful;
            set => SetProperty(ref _isConnectionSuccessful, value);
        }

        /// <summary>
        /// Selected backup file path.
        /// </summary>
        public string BackupFilePath
        {
            get => _backupFilePath;
            set
            {
                if (SetProperty(ref _backupFilePath, value))
                {
                    UpdateBackupBaseName();
                    UpdatePlannedFolders();
                }
            }
        }

        /// <summary>
        /// Backup base name (file name without extension).
        /// </summary>
        public string BackupBaseName
        {
            get => _backupBaseName;
            private set => SetProperty(ref _backupBaseName, value);
        }

        /// <summary>
        /// Selected database type (Office or Shop).
        /// </summary>
        public DatabaseType DatabaseType
        {
            get => _databaseType;
            set
            {
                if (SetProperty(ref _databaseType, value))
                {
                    UpdateInstanceBasedOnType();
                    UpdatePlannedFolders();
                }
            }
        }

        /// <summary>
        /// Current Windows user logged in.
        /// </summary>
        public string CurrentWindowsUser
        {
            get => _currentWindowsUser;
            private set => SetProperty(ref _currentWindowsUser, value);
        }

        /// <summary>
        /// Target database name.
        /// </summary>
        public string DatabaseName
        {
            get => _databaseName;
            set => SetProperty(ref _databaseName, value);
        }

        /// <summary>
        /// Custom folder name for the database files.
        /// </summary>
        public string CustomFolderName
        {
            get => _customFolderName;
            set
            {
                if (SetProperty(ref _customFolderName, value))
                {
                    UpdatePlannedFolders();
                }
            }
        }

        /// <summary>
        /// Data file folder.
        /// </summary>
        public string DataFileFolder
        {
            get => _dataFileFolder;
            set => SetProperty(ref _dataFileFolder, value);
        }

        /// <summary>
        /// Log file folder.
        /// </summary>
        public string LogFileFolder
        {
            get => _logFileFolder;
            set => SetProperty(ref _logFileFolder, value);
        }

        /// <summary>
        /// Whether a restore operation is in progress.
        /// </summary>
        public bool IsRestoring
        {
            get => _isRestoring;
            set
            {
                if (SetProperty(ref _isRestoring, value))
                {
                    OnPropertyChanged(nameof(CanModifySettings));
                }
            }
        }

        /// <summary>
        /// Whether the restore is complete.
        /// </summary>
        public bool IsRestoreComplete
        {
            get => _isRestoreComplete;
            set => SetProperty(ref _isRestoreComplete, value);
        }

        /// <summary>
        /// Whether settings can be modified (not during restore).
        /// </summary>
        public bool CanModifySettings => !IsRestoring;

        /// <summary>
        /// Progress value (0-100).
        /// </summary>
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        /// <summary>
        /// Whether progress is indeterminate.
        /// </summary>
        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            set => SetProperty(ref _isProgressIndeterminate, value);
        }

        /// <summary>
        /// Whether to replace an existing database.
        /// </summary>
        public bool ReplaceExistingDatabase
        {
            get => _replaceExistingDatabase;
            set => SetProperty(ref _replaceExistingDatabase, value);
        }

        /// <summary>
        /// Log entries for display.
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries
        {
            get => _logEntries;
            set => SetProperty(ref _logEntries, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to test SQL connection.
        /// </summary>
        public ICommand TestConnectionCommand { get; }

        /// <summary>
        /// Command to browse for backup file.
        /// </summary>
        public ICommand BrowseBackupFileCommand { get; }

        /// <summary>
        /// Command to restore database.
        /// </summary>
        public ICommand RestoreDatabaseCommand { get; }

        #endregion

        #region Command Methods

        private bool CanTestConnection()
        {
            if (string.IsNullOrWhiteSpace(SqlServerInstance))
                return false;

            if (AuthenticationType == AuthenticationType.SqlServer)
            {
                return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
            }

            return true;
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                ConnectionStatus = "Testing...";
                IsConnectionSuccessful = false;

                var success = await _sqlRestoreService.TestConnectionAsync(
                    SqlServerInstance,
                    AuthenticationType,
                    Username,
                    Password);

                if (success)
                {
                    ConnectionStatus = "✓ Connection successful";
                    IsConnectionSuccessful = true;
                }
                else
                {
                    ConnectionStatus = "✗ Connection failed";
                    IsConnectionSuccessful = false;
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"✗ Error: {ex.Message}";
                IsConnectionSuccessful = false;
            }
        }

        private void BrowseBackupFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SQL Backup Files (*.bak)|*.bak|All Files (*.*)|*.*",
                Title = "Select SQL Server Backup File",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                BackupFilePath = dialog.FileName;
            }
        }

        private bool CanRestoreDatabase()
        {
            if (IsRestoring)
                return false;

            if (!IsConnectionSuccessful)
                return false;

            if (string.IsNullOrWhiteSpace(BackupFilePath) || !File.Exists(BackupFilePath))
                return false;

            if (string.IsNullOrWhiteSpace(DatabaseName))
                return false;

            if (string.IsNullOrWhiteSpace(DataFileFolder))
                return false;

            if (string.IsNullOrWhiteSpace(LogFileFolder))
                return false;

            return true;
        }

        private async Task RestoreDatabaseAsync()
        {
            try
            {
                IsRestoring = true;
                IsRestoreComplete = false;
                ProgressValue = 0;
                LogEntries.Clear();

                AddLogEntry("Starting database restore process...", LogLevel.Info);

                // Ensure directories exist
                try
                {
                    Directory.CreateDirectory(DataFileFolder);
                    Directory.CreateDirectory(LogFileFolder);
                }
                catch (Exception ex)
                {
                    AddLogEntry($"Failed to create directories: {ex.Message}", LogLevel.Error);
                    MessageBox.Show(
                        $"Failed to create directories:\n{ex.Message}",
                        "Directory Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Check if database already exists
                var dbExists = await _sqlRestoreService.DatabaseExistsAsync(
                    SqlServerInstance,
                    AuthenticationType,
                    DatabaseName,
                    Username,
                    Password);

                if (dbExists && !ReplaceExistingDatabase)
                {
                    var result = MessageBox.Show(
                        $"Database '{DatabaseName}' already exists. Do you want to replace it?",
                        "Database Exists",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        AddLogEntry("Restore cancelled by user.", LogLevel.Warning);
                        return;
                    }

                    ReplaceExistingDatabase = true;
                }

                // Build file paths with hardcoded names
                var dataFilePath = Path.Combine(DataFileFolder, "VastOffice.mdf");
                var logFilePath = Path.Combine(LogFileFolder, "VastOffice_log.ldf");

                // Perform restore
                var success = await _sqlRestoreService.RestoreDatabaseAsync(
                    SqlServerInstance,
                    AuthenticationType,
                    BackupFilePath,
                    DatabaseName,
                    dataFilePath,
                    logFilePath,
                    ReplaceExistingDatabase,
                    Username,
                    Password);

                if (success)
                {
                    IsRestoreComplete = true;
                    ProgressValue = 100;
                    IsProgressIndeterminate = false;

                    MessageBox.Show(
                        $"Database '{DatabaseName}' has been restored successfully!\n\n" +
                        $"Data file: {dataFilePath}\n" +
                        $"Log file: {logFilePath}",
                        "Restore Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Database restore failed. Please check the log for details.",
                        "Restore Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"Unexpected error: {ex.Message}", LogLevel.Error);
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsRestoring = false;
            }
        }

        #endregion

        #region Helper Methods

        private async Task InitializeAsync()
        {
            try
            {
                AddLogEntry($"Welcome {_currentWindowsUser}", LogLevel.Info);
                AddLogEntry("Connecting to SQL Server...", LogLevel.Info);

                // Default to VastOffice instance
                SqlServerInstance = "VastOffice";

                // Auto-test connection
                await TestConnectionAsync();
            }
            catch
            {
                // Ignore errors during initialization
            }
        }

        private void UpdateInstanceBasedOnType()
        {
            // Update SQL instance based on database type
            SqlServerInstance = DatabaseType == DatabaseType.Office ? "VastOffice" : "VastPOS";

            // Reset connection status when changing instance
            ConnectionStatus = string.Empty;
            IsConnectionSuccessful = false;

            AddLogEntry($"Switched to {SqlServerInstance} instance", LogLevel.Info);
        }

        private void UpdateBackupBaseName()
        {
            if (!string.IsNullOrWhiteSpace(BackupFilePath) && File.Exists(BackupFilePath))
            {
                var fileName = Path.GetFileNameWithoutExtension(BackupFilePath);
                BackupBaseName = fileName;
                DatabaseName = fileName; // Auto-populate database name
                CustomFolderName = fileName; // Auto-populate custom folder name
            }
            else
            {
                BackupBaseName = string.Empty;
            }
        }

        private void UpdatePlannedFolders()
        {
            if (string.IsNullOrWhiteSpace(CustomFolderName))
            {
                DataFileFolder = string.Empty;
                LogFileFolder = string.Empty;
                return;
            }

            var typeFolder = DatabaseType == DatabaseType.Office ? "Office" : "Shop";

            // Try E: and F: drives first, but allow user to change
            if (Directory.Exists("E:\\"))
            {
                DataFileFolder = $@"E:\SQLData\{typeFolder}\{CustomFolderName}\";
            }
            else
            {
                DataFileFolder = $@"C:\SQLData\{typeFolder}\{CustomFolderName}\";
            }

            if (Directory.Exists("F:\\"))
            {
                LogFileFolder = $@"F:\{typeFolder}\{CustomFolderName}\";
            }
            else
            {
                LogFileFolder = $@"C:\SQLLogs\{typeFolder}\{CustomFolderName}\";
            }
        }

        private void OnLogMessageReceived(object? sender, LogEntry e)
        {
            // Ensure we're on the UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(e);
            });
        }

        private void AddLogEntry(string message, LogLevel level)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                Level = level
            };

            LogEntries.Add(entry);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
