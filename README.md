# SQL .BAK Restore Tool

A modern, production-ready WPF desktop application built with .NET 8 that makes it extremely easy for non-technical users to restore SQL Server databases from `.bak` backup files.

## Features

- **Simple, Wizard-Style UI**: Clean, modern interface with step-by-step guidance
- **Automatic Folder Management**: Automatically creates properly structured data and log folders
- **Office/Shop Database Support**: Separate folder structures for Office and Shop databases
- **Connection Testing**: Built-in SQL Server connection validation
- **Real-Time Logging**: Visual feedback throughout the restore process
- **Error Handling**: Comprehensive error handling with clear, user-friendly messages
- **Windows & SQL Authentication**: Supports both authentication methods

## Requirements

- Windows operating system
- .NET 8 Runtime
- SQL Server (any version that supports RESTORE DATABASE)
- E: and F: drives available for data and log files

## Drive Structure

The application automatically organizes database files in a structured manner:

### Office Databases
- **Data Files (MDF)**: `E:\SQLData\Office\<BackupBaseName>\<DatabaseName>.mdf`
- **Log Files (LDF)**: `F:\Office\<BackupBaseName>\<DatabaseName>_log.ldf`

### Shop Databases
- **Data Files (MDF)**: `E:\SQLData\Shop\<BackupBaseName>\<DatabaseName>.mdf`
- **Log Files (LDF)**: `F:\Shop\<BackupBaseName>\<DatabaseName>_log.ldf`

## How to Use

1. **SQL Server Connection**
   - Enter your SQL Server instance name (e.g., `localhost`, `.\SQLEXPRESS`)
   - Choose Windows Authentication or SQL Server Authentication
   - If using SQL auth, provide username and password
   - Click "Test Connection" to verify

2. **Select Backup File**
   - Click "Browse .BAK File..." to select your backup file
   - The application will automatically detect the backup name

3. **Choose Database Type**
   - Select whether this is an **Office** or **Shop** database
   - The folder paths will update automatically based on your selection

4. **Database Details**
   - Review or modify the database name
   - Check the planned folder locations
   - Optionally enable "Replace existing database" if needed

5. **Restore**
   - Click "Restore Database"
   - Monitor progress in the log panel
   - Wait for the success message

## Building the Application

### Prerequisites
- Visual Studio 2022 or later
- .NET 8 SDK

### Build Steps
```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the application
dotnet run
```

### Publishing
To create a self-contained executable:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Architecture

The application follows the MVVM (Model-View-ViewModel) pattern:

- **Views**: `MainWindow.xaml` - Main application window
- **ViewModels**: `MainViewModel.cs` - Application logic and data binding
- **Services**: `SqlRestoreService.cs` - SQL Server operations
- **Models**: Data structures for authentication, database types, and logging
- **Helpers**: RelayCommand implementation for MVVM commanding

## Dependencies

- **Microsoft.Data.SqlClient** (5.2.2) - Modern SQL Server connectivity

## Error Handling

The application handles common scenarios:
- Invalid SQL Server connection
- Missing backup files
- Drive not available (E: or F:)
- Database already exists
- Insufficient permissions
- SQL Server errors during restore

## Security Considerations

- Passwords are handled securely through WPF PasswordBox controls
- SQL connection strings are built using SqlConnectionStringBuilder
- TrustServerCertificate is enabled for local connections
- No credentials are logged or stored persistently

## Future Enhancements

Potential features for future versions:
- Support for custom drive letters
- Batch restore operations
- Backup verification before restore
- Scheduled restores
- Email notifications on completion
- Restore history tracking

## License

This is a production-ready tool for internal use. Modify and distribute as needed for your organization.

## Support

For issues or feature requests, please contact your development team.
