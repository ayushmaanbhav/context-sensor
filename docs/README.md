# ContextSensor Documentation

**Version**: 1.0.0  
**Last Updated**: 2025-10-31  
**Platform**: Windows 10/11

## Overview

ContextSensor is a Windows event monitoring application that captures system events (mouse clicks) and logs detailed contextual information about the foreground application. Built with enterprise-grade architecture using .NET 8.0, it features a configuration-driven design that separates platform-specific code from core business logic.

### Key Features

- ✅ **Mouse Event Capture**: Records left, right, and middle mouse button clicks
- ✅ **Context Awareness**: Captures foreground process information and cursor position
- ✅ **Configuration-Driven**: Define events and attributes declaratively in JSON
- ✅ **Multiple Output Formats**: CSV and JSON with configurable destinations
- ✅ **Platform Abstraction**: OS-agnostic core with Windows-specific implementations
- ✅ **Extensible Design**: Easy to add new events, attributes, and output formats
- ✅ **Production-Ready**: Thread-safe, async processing, graceful error handling

## Quick Start

### Prerequisites
- Windows 10 (1809+) or Windows 11
- .NET 8.0 Desktop Runtime

### Installation
```powershell
# 1. Install .NET 8.0 Runtime
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# 2. Extract application
Expand-Archive -Path ContextSensor.zip -DestinationPath C:\ContextSensor

# 3. Run application
cd C:\ContextSensor
.\ContextSensor.App.exe
```

### Basic Usage
```
# Application starts and displays:
Event monitoring started. Capturing left mouse button clicks...
Press Ctrl+C to stop.

# Click your mouse - events appear:
WM_LBUTTONDOWN, 1234, "notepad.exe", 450, 300

# Stop with Ctrl+C
Shutting down gracefully...
Shutdown complete.
```

## Documentation Structure

### 📋 [REQUIREMENTS.md](REQUIREMENTS.md)
**Project requirements and solution approach**
- Original functional requirements
- Non-functional requirements
- Design goals and constraints
- Solution overview

**Read this first** to understand what ContextSensor does and why.

### 🏗️ [DESIGN.md](DESIGN.md)
**Comprehensive software design and architecture**
- Three-layer architecture overview
- SOLID principles in practice
- Core components deep dive
- Design patterns used (7+ patterns)
- Data flow and processing pipeline
- Extensibility guide (add events, attributes, outputs)
- Design strengths analysis

**Read this** to understand how ContextSensor is architected and why design decisions were made.

### ⚙️ [CONFIGURATION.md](CONFIGURATION.md)
**Complete configuration reference**
- Configuration file structure
- Event schema definition
- Event listeners configuration
- Output writers (Console, File)
- Storage and logging settings
- Configuration examples
- Best practices and troubleshooting

**Read this** to customize ContextSensor for your needs.

### 🪟 [SETUP_WINDOWS.md](SETUP_WINDOWS.md)
**Windows installation and setup guide**
- System requirements
- .NET 8.0 installation
- Building from source
- Running the application
- Verification steps
- Troubleshooting common issues
- Uninstallation

**Read this** to install and run ContextSensor on Windows 10/11.

### 📦 [Installer Documentation](installer/)
**MSI Installer package for professional deployment**
- **[Installer Guide](installer/GUIDE.md)** - Complete installation, configuration, troubleshooting, and deployment guide
- **[Update Manifest Reference](installer/UPDATE_MANIFEST_REFERENCE.md)** - Creating and hosting automatic update manifests
- **[Build Instructions](installer/BUILD.md)** - Building the MSI installer package

**Read this** for enterprise deployment, automatic updates, and Windows service management.

### 🧪 [Testing Documentation](testing/)
**Comprehensive testing guide**
- **[Testing Guide](testing/GUIDE.md)** - Complete testing procedures and scenarios
- Test environment setup
- Manual and automated testing procedures
- Test scenarios and verification

**Read this** for testing installation, services, and auto-updates.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    ContextSensor.App                         │
│  • Entry Point & DI Configuration                            │
│  • Application Lifecycle Management                          │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────────────┐
│                    ContextSensor.Core                        │
│  • Platform-Agnostic Business Logic                          │
│  • Attribute System (Provider Pattern)                       │
│  • Event Processing Pipeline                                 │
│  • Configuration Management                                  │
│  • Output Writers (Base Classes)                             │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────────────┐
│                ContextSensor.Windows                         │
│  • Windows-Specific Implementations                          │
│  • Mouse Hook (Win32 API)                                   │
│  • Process Information (Win32 API)                           │
│  • Event Type Mapping                                        │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Principles

1. **SOLID Principles**: Each class has single responsibility, open for extension
2. **Configuration-Driven**: Events and attributes defined in JSON, not code
3. **Platform Abstraction**: Core logic is OS-agnostic
4. **Provider Pattern**: Extensible attribute system
5. **Async Processing**: Channel-based pipeline for performance
6. **Interface-Based**: Loose coupling via dependency injection

## Configuration Files

### appsettings.json - Application Settings
```json
{
  "EventListeners": [
    { "Type": "MouseEventListener", "Enabled": true }
  ],
  "OutputWriters": [
    { "Type": "ConsoleOutputWriter", "Enabled": true, "Format": "csv" },
    { "Type": "FileOutputWriter", "Enabled": true, "Format": "json",
      "Destination": "./data/events.log", "FlushInterval": 10 }
  ],
  "Storage": { "DataDirectory": "./data" },
  "Logging": { "LogFilePath": "./logs/contextsensor.log", "MinimumLevel": "Information" }
}
```

### events.json - Event Schema
```json
{
  "events": [
    {
      "name": "mouse_click_down",
      "description": "Left mouse button click event",
      "output_attributes": [
        { "name": "os_event_type", "datatype": "string" },
        { "name": "foreground_process_id", "datatype": "integer" },
        { "name": "foreground_process_name", "datatype": "string" },
        { "name": "mouse.x", "datatype": "integer" },
        { "name": "mouse.y", "datatype": "integer" }
      ]
    }
  ]
}
```

## Output Examples

### CSV Format (Console)
```csv
WM_LBUTTONDOWN, 1234, "notepad.exe", 450, 300
WM_LBUTTONDOWN, 5678, "chrome.exe", 920, 156
WM_RBUTTONDOWN, 1234, "notepad.exe", 475, 325
```

### JSON Format (File)
```json
{"os_event_type":"WM_LBUTTONDOWN","foreground_process_id":1234,"foreground_process_name":"notepad.exe","mouse.x":450,"mouse.y":300}
{"os_event_type":"WM_LBUTTONDOWN","foreground_process_id":5678,"foreground_process_name":"chrome.exe","mouse.x":920,"mouse.y":156}
```

## Extensibility

### Adding New Event Types

ContextSensor is designed for easy extensibility:

**1. Define in events.json**:
```json
{
  "name": "key_press",
  "output_attributes": [
    { "name": "key_code", "datatype": "integer" },
    { "name": "foreground_process_id", "datatype": "integer" }
  ]
}
```

**2. Implement Event Listener** (see [DESIGN.md](DESIGN.md#adding-a-new-event-type))

**3. Register Attributes** (if new attributes needed)

**4. Enable in appsettings.json**

Future event types could include:
- Keyboard events
- Clipboard changes
- Window focus changes
- USB device connections
- Network activity

## Performance

- **CPU Usage**: <2% on modern hardware
- **Memory**: ~30-50 MB
- **Event Processing**: Async channel-based pipeline
- **Disk I/O**: Buffered writes with configurable flush interval

## Requirements

### Functional
- ✅ Works on Windows 10 and Windows 11
- ✅ Captures left mouse button clicks
- ✅ Logs OS event type (WM_LBUTTONDOWN)
- ✅ Records foreground process ID
- ✅ Records foreground process name
- ✅ Outputs to console (stdout)

### Non-Functional
- ✅ Configuration-driven design
- ✅ Scalable and extensible architecture
- ✅ OS-agnostic core layer
- ✅ Pluggable event listeners
- ✅ Multiple output formats and destinations
- ✅ SOLID principles and design patterns
- ✅ Graceful error handling
- ✅ Thread-safe operations

## Technology Stack

- **Framework**: .NET 8.0 (C# 12)
- **Target**: Windows Desktop (.NET 8.0-windows)
- **Architecture**: Three-layer (App, Core, Platform)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog with file sinks
- **Configuration**: JSON (Newtonsoft.Json)
- **Windows APIs**: User32.dll (mouse hooks, process info)

## Project Structure

```
ContextSensor/
├── docs/                          # Documentation
│   ├── README.md                  # This file
│   ├── REQUIREMENTS.md            # Project requirements
│   ├── DESIGN.md                  # Architecture and design
│   ├── CONFIGURATION.md           # Configuration guide
│   └── SETUP_WINDOWS.md           # Installation guide
├── src/
│   ├── ContextSensor.App/         # Application entry point
│   │   ├── Program.cs             # Main program with DI setup
│   │   ├── appsettings.json       # Application configuration
│   │   └── events.json            # Event schema
│   ├── ContextSensor.Core/        # Platform-agnostic core
│   │   ├── Attributes/            # Attribute system
│   │   ├── Configuration/         # Config management
│   │   ├── Events/                # Event processing
│   │   ├── Output/                # Output writers
│   │   └── OS/                    # OS abstractions
│   └── ContextSensor.Windows/     # Windows implementations
│       ├── Attributes/            # Windows attribute providers
│       ├── Events/                # Windows event listeners
│       └── OS/                    # Windows OS implementations
└── ContextSensor.sln              # Solution file
```

## Common Tasks

### View Captured Events
```powershell
# Console output (real-time)
.\ContextSensor.App.exe

# File output (after stopping)
Get-Content .\data\events.log
```

### Change Output Format
```json
// In appsettings.json
"OutputWriters": [
  { "Type": "ConsoleOutputWriter", "Format": "csv" }  // or "json"
]
```

### Add More Attributes
```json
// In events.json - add to output_attributes
{ "name": "mouse.x", "datatype": "integer" },
{ "name": "mouse.y", "datatype": "integer" }
```

### Enable Debug Logging
```json
// In appsettings.json
"Logging": {
  "MinimumLevel": "Debug"  // was "Information"
}
```

## Troubleshooting

### Application Won't Start
1. Check .NET 8.0 Runtime installed: `dotnet --list-runtimes`
2. Verify configuration files exist
3. Check logs: `.\logs\contextsensor.log`

### No Events Captured
1. Verify event listener enabled in `appsettings.json`
2. Check event defined in `events.json`
3. Ensure output writer enabled
4. Review logs for errors

### Permission Errors
1. Run from user directory (not Program Files)
2. Check folder permissions: `icacls .\data`
3. Use relative paths in configuration

For detailed troubleshooting, see [SETUP_WINDOWS.md](SETUP_WINDOWS.md#troubleshooting).

## Development

### Building from Source
```powershell
# Clone repository
git clone <repository-url>
cd ContextSensor

# Restore dependencies
dotnet restore

# Build solution
dotnet build -c Release

# Run
cd src\ContextSensor.App\bin\Release\net8.0-windows
.\ContextSensor.App.exe
```

### Running Tests
```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Code Standards
- Follow C# coding conventions
- Use async/await for I/O operations
- Implement IDisposable for resource cleanup
- Add XML documentation comments
- Use dependency injection
- Follow SOLID principles

## Contributing

### Adding New Features

**New Event Listener**:
1. Implement `IEventListener` interface
2. Register in DI container
3. Add to configuration
4. See [DESIGN.md](DESIGN.md#adding-a-new-event-type)

**New Attribute Provider**:
1. Extend `AttributeProviderBase`
2. Register in `AttributeRegistry`
3. Register provider in startup
4. See [DESIGN.md](DESIGN.md#adding-a-new-attribute)

**New Output Writer**:
1. Extend `OutputWriterBase`
2. Implement format logic
3. Add to factory in Program.cs
4. See [DESIGN.md](DESIGN.md#adding-a-new-output-format)

## Roadmap

### Planned Features
- [ ] Keyboard event capture
- [ ] Clipboard monitoring
- [ ] Window focus tracking
- [ ] Database output writer
- [ ] REST API output writer
- [ ] Data encryption
- [ ] Event filtering/transformation
- [ ] Linux support
- [ ] macOS support

### Known Limitations
- Windows only (by design for v1.0)
- Mouse clicks only (extensible to other events)
- No GUI (console application)
- Requires .NET 8.0 Runtime

## License

[Specify your license here]

## Support

For questions, issues, or contributions:
- Review documentation in `docs/` folder
- Check troubleshooting sections
- Review application logs
- Check GitHub issues (if applicable)

## Acknowledgments

Built with:
- .NET 8.0
- Serilog for logging
- Newtonsoft.Json for configuration
- Windows Win32 APIs

---

**Documentation Index**:
- 📋 [Requirements](REQUIREMENTS.md) - What and why
- 🏗️ [Design](DESIGN.md) - How it's built
- ⚙️ [Configuration](CONFIGURATION.md) - How to configure
- 🪟 [Setup Guide](SETUP_WINDOWS.md) - How to install and run
- 📦 [Installer Guide](installer/GUIDE.md) - Professional deployment with MSI
- 🔄 [Update Manifest Reference](installer/UPDATE_MANIFEST_REFERENCE.md) - Automatic updates
- 🧪 [Testing Guide](testing/GUIDE.md) - Testing procedures

**Quick Links**:
- [System Requirements](SETUP_WINDOWS.md#system-requirements)
- [Installation](SETUP_WINDOWS.md#installation-methods)
- [MSI Installer Build](installer/BUILD.md) - Building installer package
- [Enterprise Deployment](installer/GUIDE.md#deployment-scenarios)
- [Configuration Reference](CONFIGURATION.md#configuration-files)
- [Architecture Overview](DESIGN.md#architecture)
- [Extensibility Guide](DESIGN.md#extensibility)
- [Troubleshooting](SETUP_WINDOWS.md#troubleshooting)
