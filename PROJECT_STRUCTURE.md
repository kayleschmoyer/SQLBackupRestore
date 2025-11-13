# Project Structure

```
SQLBackupRestore/
│
├── SQLBackupRestore.csproj          # Project file (.NET 8, WPF)
├── App.xaml                         # Application resources and styles
├── App.xaml.cs                      # Application startup logic
├── MainWindow.xaml                  # Main window UI layout
├── MainWindow.xaml.cs               # Main window code-behind
│
├── ViewModels/
│   └── MainViewModel.cs             # Main view model (MVVM pattern)
│
├── Services/
│   └── SqlRestoreService.cs         # SQL Server operations service
│
├── Models/
│   ├── AuthenticationType.cs       # Enum for auth types
│   ├── DatabaseType.cs              # Enum for database types (Office/Shop)
│   ├── FileListItem.cs              # Model for backup file list
│   └── LogEntry.cs                  # Model for log entries
│
├── Helpers/
│   └── RelayCommand.cs              # ICommand implementation for MVVM
│
├── Converters/
│   ├── EnumToBooleanConverter.cs    # Enum to boolean converter for radio buttons
│   └── StringToVisibilityConverter.cs # String to visibility converter
│
├── README.md                        # User documentation
├── PROJECT_STRUCTURE.md             # This file
└── .gitignore                       # Git ignore file
```

## Key Components

### Application Layer
- **App.xaml/cs**: Defines application-level resources, styles, and global exception handling

### Presentation Layer
- **MainWindow.xaml**: Complete UI with all controls, bindings, and styling
- **MainViewModel.cs**: Handles all UI logic, data binding, and commands

### Business Logic Layer
- **SqlRestoreService.cs**: Encapsulates all SQL Server operations:
  - Connection testing
  - Backup file analysis (RESTORE FILELISTONLY)
  - Database restoration (RESTORE DATABASE)
  - Instance detection

### Data Layer
- **Models**: Define data structures and enums
- **Converters**: Enable XAML data binding transformations

### Infrastructure
- **Helpers**: Reusable components like RelayCommand for MVVM pattern

## Design Patterns

1. **MVVM (Model-View-ViewModel)**
   - Separation of concerns
   - Data binding for responsive UI
   - Commands for user actions

2. **Service Pattern**
   - SqlRestoreService encapsulates all database operations
   - Loosely coupled from UI layer

3. **Event-Driven Architecture**
   - LogMessageReceived events for real-time logging
   - PropertyChanged for UI updates

## Dependencies

- **Microsoft.Data.SqlClient** (5.2.2): Modern, maintained SQL Server client library
- **.NET 8 WPF**: Windows Presentation Foundation for rich desktop UI

## Build Instructions

### Visual Studio 2022+
1. Open `SQLBackupRestore.csproj` in Visual Studio
2. Build > Build Solution (or press F6)
3. Run with F5

### Command Line (.NET SDK required)
```bash
dotnet restore
dotnet build --configuration Release
dotnet run
```

### Publishing
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Code Quality Features

- **Nullable Reference Types**: Enabled for better null safety
- **Async/Await**: All long-running operations are async
- **Strong Typing**: Full type safety throughout
- **Error Handling**: Comprehensive try-catch with user-friendly messages
- **Logging**: Real-time operation logging visible to user
- **Validation**: Input validation before operations
- **Resource Management**: Proper disposal of SQL connections

## Testing Recommendations

1. **Unit Tests**: Test SqlRestoreService methods
2. **Integration Tests**: Test actual SQL Server connections
3. **UI Tests**: Test ViewModel command logic
4. **Manual Tests**:
   - Test with various backup file sizes
   - Test Office vs Shop folder creation
   - Test Windows vs SQL auth
   - Test error scenarios (missing drives, bad credentials)
