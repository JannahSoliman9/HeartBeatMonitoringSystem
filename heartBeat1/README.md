# Heartbeat Monitoring System

This project implements a heartbeat monitoring system for monitoring WPF applications and Windows services. It includes a `Watcher` service that monitors the heartbeat of applications, logs the information to a SQL Server database, and restarts the applications if they stop sending heartbeats.

## Features

- **Watcher Service**: Monitors multiple WPF applications and Windows services, restarting them if they fail to send heartbeats.
- **Backup Watcher**: A secondary service that monitors the primary watcher, ensuring redundancy.
- **Database Logging**: Logs heartbeat data, received messages, and restart logs into a SQL Server database.
- **Configurable Settings**: Easily configurable through `appsettings.json` and per-app configuration files.

## Requirements

- **.NET 6.0 SDK**
- **SQL Server** (for database logging)
- **Windows OS** (for running services)

### NuGet Packages

This project uses the following NuGet packages:
- `Microsoft.Extensions.Hosting` (v6.0.1)
- `Microsoft.Extensions.Hosting.WindowsServices` (v6.0.1)
- `System.Runtime.InteropServices` (v4.3.0)
- `TaskScheduler` (v2.11.0)

You can install these packages via the NuGet Package Manager in Visual Studio or by running the following command in the Package Manager Console:

```sh
dotnet add package Microsoft.Extensions.Hosting --version 6.0.1
dotnet add package Microsoft.Extensions.Hosting.WindowsServices --version 6.0.1
dotnet add package System.Runtime.InteropServices --version 4.3.0
dotnet add package TaskScheduler --version 2.11.0
```

## Project Structure

### Library (HeartBeat)

- **`App.cs`**: Defines the `App` class with attributes for `AppId`, `AppInterval`, and `AppTimer`.
- **`AppConfig.cs`**: Manages application-specific configuration settings.
- **`DbManager.cs`**: Handles interactions with the database.
- **`ProcessExtensions.cs`**: Provides additional process management functionalities.
- **`Watcher.cs`**: Implements the core watcher functionality for monitoring apps.
- **`WatcherAppsConfig.cs`**: Manages configuration settings for the apps being monitored.
- **`WatcherConfig.cs`**: Manages configuration settings for the watcher itself.
- **`UdpReceiver.cs`**: Receives UDP messages from apps.
- **`UdpSender.cs`**: Sends UDP messages to apps.

### Watcher Service

- **`App.cs`**: Contains the `App` class specific to the watcher service.
- **`appsettings.json`**: Configuration settings for the watcher service.
- **`ProcessExtensions.cs`**: Extensions for process management in the watcher service.
- **`Program.cs`**: Entry point for the watcher service.
- **`Watcher.cs`**: Implements the watcher functionality for the watcher service.
- **`Worker.cs`**: Defines background tasks for the watcher service.
- **`WatcherService.csproj`**: Project file for the watcher service.

### Backup Watcher

- **`App.cs`**: Contains the `App` class for the backup watcher.
- **`appsettings.json`**: Configuration settings for the backup watcher.
- **`Program.cs`**: Entry point for the backup watcher.
- **`Watcher.cs`**: Implements the watcher functionality for the backup watcher.
- **`Worker.cs`**: Defines background tasks for the backup watcher.
- **`BackupWatcher.csproj`**: Project file for the backup watcher.

### Service App

- **`appsettings.json`**: Configuration settings for the service app.
- **`Program.cs`**: Entry point for the service app.
- **`Worker.cs`**: Defines background tasks for the service app.

### WPF App

- **`MainWindow.xaml`**: Defines the UI layout for the WPF app.
- **`appSettings.json`**: Configuration settings for the WPF app.


## Setup Instructions

### 1. Clone the Repository

```sh
git clone https://github.com/JannahSoliman9/HeartBeatMonitoringSystem.git
cd HeartbeatMonitoringSystem/HeartBeat1
```
Open the project solution "heartBeat1.sln"

### 2. Update Database Connection String

In `DatabaseManager.cs`, update the connection string to match your SQL Server instance:

```csharp
string connectionString = "Server=your_server;Database=your_database;User Id=your_user;Password=your_password;";
```

### 3. Configure Application Settings

- **Watcher Configuration (`appsettings.json`)**:
  - **AppId**: Unique identifier for each app or service being monitored.
  - **Interval**: Interval in seconds for checking the heartbeat.
  - **Executable**: Path to the executable if it's a WPF app. Leave empty for services.

Example:

```json
{
  "Apps": [
    {
      "AppId": "App1",
      "IntervalInSeconds": 30,
      "ExecutablePath": "path\\to\\app1.exe"
    },
    {
      "AppId": "Service1",
      "IntervalInSeconds": 60
      "ExecutablePath": " "
    }
  ]
}
```

- **Service App Configuration (`appsettings.json`)**:
  - **AppId**: Unique identifier for the service.
  - **SendingInterval**: Interval in seconds for sending heartbeats.

Example:

```json
{
  "AppId": "Service1",
  "IntervalInSeconds": 30
}
```

### 4. Create Windows Services

For each service app and for the watcher, you need to create a Windows service using the `sc create` command.

- **Watcher Service**:

```sh
sc create WatcherService binPath= "C:\path\to\WatcherService.exe"
```

- **Backup Watcher**:

```sh
sc create BackupWatcherService binPath= "C:\path\to\BackupWatcherService.exe"
```

- **Application/Service Monitoring**:

For each app or service being monitored:

```sh
sc create App1Service binPath= "C:\path\to\App1Service.exe"
```

### 5. Running the Services

After creating the services, start them using:

```sh
sc start WatcherService
sc start BackupWatcherService
sc start App1Service
```

## Troubleshooting

- **Database Connection**: Ensure that the connection string in `DatabaseManager.cs` is correctly configured.
- **Service Not Starting**: Verify the paths in the `sc create` commands and ensure that the executables exist.

```

