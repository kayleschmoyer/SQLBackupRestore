# 🎯 VastOffice Database Restore Tool

A **fun**, **colorful**, and **extremely user-friendly** WPF desktop application built with .NET 8 that makes restoring SQL Server databases as easy as 1-2-3! Designed for users of **all ages and technical levels** - even if you've never used a computer before!

## ✨ Features

- **🎨 Beautiful, Fun Interface**: Colorful, emoji-rich UI with large buttons and clear instructions
- **👤 Automatic Windows Authentication**: Detects who you are automatically - no login needed!
- **🏢 Smart Instance Selection**: Automatically switches between VastOffice (Office) and VastPOS (Shop)
- **📁 Hardcoded File Names**: Always saves as VastOffice.mdf and VastOffice_log.ldf
- **⏳ Animated Loading Spinner**: See real-time progress with a fun spinning animation
- **📋 Step-by-Step Messages**: Every action is explained with friendly emojis
- **🎉 Easy as 1-2-3**: Simple wizard that guides you through each step
- **🚀 Production-Ready**: Comprehensive error handling with clear, helpful messages

## 📋 Requirements

- ✅ Windows operating system (Windows 10 or later recommended)
- ✅ .NET 8 Runtime (automatically included when you build the exe)
- ✅ SQL Server with either:
  - **VastOffice** instance (for Office databases)
  - **VastPOS** instance (for Shop databases)
- ✅ E: and F: drives available for data and log files
- ✅ Windows user account with SQL Server access

## 📂 Drive Structure

The application automatically organizes database files with **hardcoded names**:

### 🏢 Office Databases (VastOffice Instance)
- **Data File**: `E:\SQLData\Office\<BackupBaseName>\VastOffice.mdf`
- **Log File**: `F:\Office\<BackupBaseName>\VastOffice_log.ldf`

### 🛒 Shop Databases (VastPOS Instance)
- **Data File**: `E:\SQLData\Shop\<BackupBaseName>\VastOffice.mdf`
- **Log File**: `F:\Shop\<BackupBaseName>\VastOffice_log.ldf`

**Note**: The file names are **always** VastOffice.mdf and VastOffice_log.ldf regardless of the backup file name!

## 🎮 How to Use (Easy as 1-2-3!)

### When you open the application:
- 👋 You'll be greeted with a welcome message showing your Windows username
- 🔐 The app automatically logs you in using Windows Authentication
- ✅ The connection to VastOffice is tested automatically

### Step 1: Choose Your Database Type
- 🏢 Click **"Office"** if this is for an office (uses VastOffice instance)
- 🛒 Click **"Shop"** if this is for a shop (uses VastPOS instance)
- The app automatically switches to the correct SQL Server instance!

### Step 2: Choose Your Backup File
- 📁 Click the big blue **"Browse for .BAK File..."** button
- Find your backup file and click Open
- ✅ The app shows you which file you selected

### Step 3: Review Your Settings
- 📝 Check the database name
- 📂 See where the files will be saved
- 🔄 Optionally check "Replace existing database" if needed

### Step 4: Let's Do This!
- 🎉 Click the big green **"RESTORE DATABASE NOW!"** button
- ⏳ Watch the animated spinner and see what's happening
- 📋 Read the friendly step-by-step messages
- 🎊 Celebrate when you see "SUCCESS!" in the log!

## 🛠️ Building the Application from GitHub

### 📥 Step 1: Get the Code from GitHub

#### Option A: Using Git (Recommended)
```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/SQLBackupRestore.git

# Navigate into the folder
cd SQLBackupRestore
```

#### Option B: Download as ZIP
1. Go to the GitHub repository page
2. Click the green **"Code"** button
3. Click **"Download ZIP"**
4. Extract the ZIP file to a folder on your computer
5. Open a command prompt and navigate to that folder

### 🔧 Step 2: Prerequisites
Before building, make sure you have:
- **Visual Studio 2022** (Community Edition is free!) or later
- **.NET 8 SDK** (download from https://dot.net)

### 🏗️ Step 3: Build the Application

#### Using Visual Studio (Easiest for Testing):
1. Double-click the `SQLBackupRestore.csproj` file
2. Visual Studio will open
3. Press **F5** or click the green **"Start"** button to run and test
4. The application will launch and you can test it!

#### Using Command Line:
```bash
# Restore NuGet packages (downloads dependencies)
dotnet restore

# Build the solution in Debug mode (for testing)
dotnet build --configuration Debug

# Run the application for testing
dotnet run
```

### 📦 Step 4: Create an EXE to Give to People

To create a **single .exe file** that you can give to users (includes everything they need):

```bash
# This creates a single EXE file with .NET runtime included
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish
```

**What this does:**
- `-c Release`: Builds an optimized version
- `-r win-x64`: For 64-bit Windows computers
- `--self-contained true`: Includes .NET runtime (users don't need to install .NET)
- `-p:PublishSingleFile=true`: Creates one single .exe file
- `-p:PublishTrimmed=false`: Keeps all necessary files (safer)
- `-o ./publish`: Outputs to a "publish" folder

### 📍 Where to Find Your EXE

After running the publish command, your EXE will be at:
```
./publish/SQLBackupRestore.exe
```

**This single file** is everything you need! You can:
- ✅ Copy it to any Windows computer
- ✅ Put it on a USB drive
- ✅ Email it (if your email allows .exe files)
- ✅ Put it on a network share
- ✅ Give it to users

### 🚀 Distributing to Users

#### For One Computer:
1. Copy `SQLBackupRestore.exe` to the user's computer
2. Double-click to run!

#### For Multiple Computers:
1. Put the .exe on a network share
2. Users can run it directly from the network share
3. Or copy it to their local computers

#### Creating a Shortcut on Desktop:
1. Right-click the .exe file
2. Select "Create shortcut"
3. Move the shortcut to the user's Desktop
4. Optionally: Right-click the shortcut → Properties → Change Icon

### 💡 Pro Tips

**For smaller file size** (advanced users only):
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o ./publish
```
⚠️ **Warning**: Trimming might cause issues. Only use if you test thoroughly!

**To build for 32-bit Windows:**
```bash
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o ./publish32
```

## 🏗️ Architecture

The application follows the **MVVM** (Model-View-ViewModel) pattern for clean, maintainable code:

- **Views**: `MainWindow.xaml` - Beautiful, colorful UI with animations
- **ViewModels**: `MainViewModel.cs` - Application logic, auto-detection, and data binding
- **Services**: `SqlRestoreService.cs` - SQL Server operations with friendly logging
- **Models**: Data structures for authentication, database types, and logging
- **Helpers**: RelayCommand implementation for MVVM commanding

### Key Features in Code:
- 🔐 **Auto Windows Auth**: Detects `Environment.UserName` on startup
- 🔄 **Instance Switching**: Changes SQL instance based on Office/Shop selection
- 📁 **Hardcoded Names**: Always uses VastOffice.mdf and VastOffice_log.ldf
- ⏳ **Animated Spinner**: WPF Storyboard with rotating animation
- 🎨 **Material Design Colors**: Green (#4CAF50), Blue (#2196F3), Red (#F44336)

## 📚 Dependencies

- **Microsoft.Data.SqlClient** (5.2.2) - Modern SQL Server connectivity
- **.NET 8** - Latest .NET framework
- **WPF** - Windows Presentation Foundation for beautiful UI

## ⚠️ Error Handling

The application handles common scenarios with **friendly, helpful messages**:
- ❌ Invalid SQL Server connection → Shows clear error message
- 📂 Missing backup files → Asks you to select a valid .bak file
- 💾 Drive not available (E: or F:) → Warns you before starting
- 🔄 Database already exists → Asks if you want to replace it
- 🔒 Insufficient permissions → Shows permission error clearly
- ⚡ SQL Server errors during restore → Displays SQL error details with emojis

## 🔒 Security Considerations

- 🔐 **Windows Authentication Only**: Uses your Windows login automatically
- 🛡️ **No Password Storage**: Never stores or logs any passwords
- 🔗 **Secure Connections**: SQL connection strings built using SqlConnectionStringBuilder
- 🎯 **TrustServerCertificate**: Enabled for local connections
- 📝 **Safe Logging**: Only logs operations, never credentials

## 🎯 What Makes This Special

This isn't just another database tool - it's designed with **real people** in mind:

- 👴 **For Everyone**: So easy, even someone who's 90 and never used a computer can do it!
- 🎨 **Fun to Use**: Colorful, with emojis and encouraging messages
- 📖 **Educational**: Every step is explained clearly
- ⏳ **Transparent**: You can see exactly what's happening at all times
- 🎉 **Rewarding**: Celebrates your success with fun messages!

## 🐛 Troubleshooting

### "Connection failed" error
- ✅ Make sure VastOffice or VastPOS SQL instance is running
- ✅ Check that your Windows user has permission to access SQL Server
- ✅ Try selecting Office or Shop to switch instances

### "Drive not found" error
- ✅ Make sure E: and F: drives exist on your computer
- ✅ Check that you have write permissions to these drives

### "Database already exists" error
- ✅ Check the "Replace existing database" checkbox in Step 3
- ✅ Or choose a different database name

## 💻 For Developers

### Project Structure:
```
SQLBackupRestore/
├── Models/              # Data structures
├── ViewModels/          # MVVM view models
├── Views/               # XAML UI files
├── Services/            # SQL Server operations
├── Helpers/             # Utility classes
└── Converters/          # XAML data converters
```

### Testing Locally:
1. Clone from GitHub (see instructions above)
2. Open in Visual Studio
3. Press F5 to run
4. Test with a real .bak file

### Making Changes:
1. Make your changes in Visual Studio
2. Test by pressing F5
3. When ready, publish a new EXE
4. Distribute the new EXE to users

## 📜 License

This is a production-ready tool for internal use. Modify and distribute as needed for your organization.

## 💬 Support

For issues, questions, or feature requests:
- Check this README first
- Test the application locally using the instructions above
- Review error messages - they're designed to be helpful!
- Contact your development team for assistance

---

**Made with ❤️ and lots of emojis to make database restoration fun!**
