# ContextSensor - Configuration Guide

## Table of Contents
1. [Overview](#overview)
2. [Configuration Files](#configuration-files)
3. [Application Settings](#application-settings)
4. [Event Schema](#event-schema)
5. [Event Listeners](#event-listeners)
6. [Output Writers](#output-writers)
7. [Storage Configuration](#storage-configuration)
8. [Logging Configuration](#logging-configuration)
9. [Configuration Examples](#configuration-examples)
10. [Best Practices](#best-practices)

## Overview

ContextSensor uses a **two-file configuration system** that separates application behavior from event schema definitions:

- **[`appsettings.json`](../src/ContextSensor.App/appsettings.json)** - Application-level settings (listeners, writers, storage, logging)
- **[`events.json`](../src/ContextSensor.App/events.json)** - Event schema definitions (what events to capture and what data to record)

### Configuration Philosophy

**Why Two Files?**

1. **Separation of Concerns**: Application behavior vs. data schema
2. **Independent Versioning**: Update event definitions without touching app config
3. **Clarity**: Users can focus on schema changes without app settings clutter
4. **Upgradability**: Event schema can be versioned and migrated independently

### Configuration Loading

Configuration is loaded at application startup by [`JsonConfigurationLoader`](../src/ContextSensor.Core/Configuration/JsonConfigurationLoader.cs):

```csharp
// Loads appsettings.json
var config = configLoader.LoadConfiguration("appsettings.json");

// Automatically loads events.json from same directory
config.EventSchema = LoadEventSchema("events.json");
```

**Validation**: Both files are validated on load. Application will not start if configuration is invalid.

## Configuration Files

### File Locations

Both configuration files must be in the **application directory** (where the executable is located):

```
ContextSensor.App.exe
├── appsettings.json    ← Application settings
├── events.json         ← Event schema
├── logs/               ← Created automatically
└── data/               ← Created automatically
```

### File Formats

Both files use **JSON** with UTF-8 encoding:
- Comments are **not supported** in standard JSON
- Use proper JSON syntax (quoted keys, no trailing commas)
- Case-sensitive property names

## Application Settings

### File: appsettings.json

Complete structure:

```json
{
  "EventListeners": [
    {
      "Type": "MouseEventListener",
      "Enabled": true
    }
  ],
  "OutputWriters": [
    {
      "Type": "ConsoleOutputWriter",
      "Enabled": true,
      "Format": "csv",
      "Destination": "console"
    },
    {
      "Type": "FileOutputWriter",
      "Enabled": true,
      "Format": "json",
      "Destination": "./data/events.log",
      "FlushInterval": 10
    }
  ],
  "Storage": {
    "DataDirectory": "./data"
  },
  "Logging": {
    "LogFilePath": "./logs/contextsensor.log",
    "MinimumLevel": "Information"
  },
  "AppSettings": {
    "ApplicationName": "ContextSensor",
    "Version": "1.0.0"
  }
}
```

## Event Schema

### File: events.json

Complete structure:

```json
{
  "events": [
    {
      "name": "mouse_click_down",
      "description": "Left mouse button click event",
      "output_attributes": [
        {
          "name": "os_event_type",
          "datatype": "string",
          "description": "The OS-level event type identifier"
        },
        {
          "name": "foreground_process_id",
          "datatype": "integer",
          "description": "Process ID of the foreground application"
        },
        {
          "name": "foreground_process_name",
          "datatype": "string",
          "description": "Executable name of the foreground process"
        },
        {
          "name": "mouse.x",
          "datatype": "integer",
          "description": "X coordinate of mouse cursor"
        },
        {
          "name": "mouse.y",
          "datatype": "integer",
          "description": "Y coordinate of mouse cursor"
        }
      ]
    }
  ]
}
```

### Event Definition

Each event definition consists of:

#### name (string, required)
**Purpose**: Unique identifier for the event type.

**Value**: Lowercase with underscores (e.g., `mouse_click_down`, `key_press`)

**Impact**: 
- Used by event listeners to determine which events to capture
- Matched by event type mapper (e.g., `WM_LBUTTONDOWN` → `mouse_click_down`)
- Referenced in logs for debugging

**Example**:
```json
"name": "mouse_click_down"
```

#### description (string, optional)
**Purpose**: Human-readable description of what the event represents.

**Value**: Any descriptive text

**Impact**: 
- Documentation only
- Appears in startup logs
- No effect on functionality

**Example**:
```json
"description": "Left mouse button click event"
```

#### output_attributes (array, required)
**Purpose**: List of attributes to include in output for this event.

**Value**: Array of attribute definition objects

**Impact**: 
- Determines exactly what data is captured and outputted
- Order defines output order in CSV format
- Missing attributes are outputted as empty values
- Adding/removing attributes changes output schema

**Example**:
```json
"output_attributes": [
  { "name": "os_event_type", "datatype": "string", "description": "..." }
]
```

### Attribute Definition

Each attribute in `output_attributes` consists of:

#### name (string, required)
**Purpose**: Identifies which attribute to capture.

**Value**: Qualified attribute name (e.g., `os_event_type`, `mouse.x`, `foreground_process_id`)

**Must Match**: Registered attributes in [`AttributeRegistry`](../src/ContextSensor.Core/Attributes/AttributeDefinition.cs)

**Available Attributes**:

**Global Attributes** (no component prefix):
- `os_event_type` - Windows event type (e.g., "WM_LBUTTONDOWN")
- `foreground_process_id` - Process ID of active window
- `foreground_process_name` - Executable name of active process

**Component Attributes** (component.attribute format):
- `mouse.x` - Mouse cursor X coordinate
- `mouse.y` - Mouse cursor Y coordinate
- `process.id` - Generic process ID (context-dependent)
- `process.name` - Generic process name (context-dependent)

**Impact**:
- Unknown attributes are logged as warnings
- Output includes empty value if provider unavailable
- Case-sensitive matching

**Example**:
```json
"name": "foreground_process_id"
```

#### datatype (string, required)
**Purpose**: Specifies the data type for proper formatting.

**Supported Values**:
- `string` - Text data (quoted in output)
- `integer` - 32-bit integer
- `long` - 64-bit integer
- `double` - Floating point number
- `boolean` - true/false
- `datetimeutc` - UTC timestamp (ISO 8601)
- `datetimewithtz` - Timestamp with timezone
- `timespan` - Duration
- `json` - JSON object or array

**Impact**:
- **CSV Format**: Affects quoting and formatting
  - `string`: Quoted (e.g., `"notepad.exe"`)
  - `integer`: Unquoted number (e.g., `1234`)
  - `boolean`: Lowercase (e.g., `true`)
- **JSON Format**: Affects JSON type
  - `string`: JSON string
  - `integer`: JSON number
  - `boolean`: JSON boolean
- **Validation**: Type mismatches are logged but don't fail
- **Future**: May enable type validation and conversion

**Example**:
```json
"datatype": "integer"
```

#### description (string, optional)
**Purpose**: Documents what this attribute represents.

**Value**: Any descriptive text

**Impact**: Documentation only, no functional effect

**Example**:
```json
"description": "Process ID of the foreground application"
```

### Adding Custom Events

**To add a new event type** (e.g., keyboard events):

1. **Define event in schema**:
```json
{
  "name": "key_press",
  "description": "Keyboard key press event",
  "output_attributes": [
    { "name": "os_event_type", "datatype": "string", "description": "Key event type" },
    { "name": "key_code", "datatype": "integer", "description": "Virtual key code" },
    { "name": "foreground_process_id", "datatype": "integer", "description": "Target process" }
  ]
}
```

2. **Add event listener** (requires code changes - see [DESIGN.md](DESIGN.md#adding-a-new-event-type))

3. **Register attributes** (if new attributes needed)

4. **Enable listener** in [`appsettings.json`](../src/ContextSensor.App/appsettings.json)

## Event Listeners

### Configuration Section

```json
"EventListeners": [
  {
    "Type": "MouseEventListener",
    "Enabled": true
  }
]
```

### Properties

#### Type (string, required)
**Purpose**: Specifies which event listener implementation to use.

**Available Values**:
- `MouseEventListener` - Captures mouse button events

**Future Values** (require implementation):
- `KeyboardEventListener` - Keyboard events
- `ClipboardEventListener` - Clipboard changes
- `USBEventListener` - USB device events
- `NetworkEventListener` - Network activity

**Impact**:
- Application will fail to start if Type is unknown
- Type must exactly match class name
- Case-sensitive

**Example**:
```json
"Type": "MouseEventListener"
```

#### Enabled (boolean, required)
**Purpose**: Controls whether this listener is active.

**Values**:
- `true` - Listener will be started
- `false` - Listener will be ignored (no hooks installed)

**Impact**:
- **Performance**: Disabled listeners have zero overhead
- **Events**: No events captured when disabled
- **Dynamic**: Cannot change without restart
- **Multiple**: Can have multiple listeners of same type (if needed)

**Example**:
```json
"Enabled": true
```

### MouseEventListener Details

**What it captures**:
- Left mouse button down (WM_LBUTTONDOWN)
- Right mouse button down (WM_RBUTTONDOWN)
- Middle mouse button down (WM_MBUTTONDOWN)

**How it works**:
- Installs Windows low-level mouse hook
- Runs on hook thread (synchronous)
- Minimal processing in hook callback
- Queues events to async channel for processing

**Requirements**:
- Windows 10 or Windows 11
- Appropriate permissions (usually User level sufficient)
- Message pump must be running (handled by HiddenForm)

**Performance**:
- Low overhead (~1-2% CPU on modern hardware)
- Async processing prevents UI lag
- Buffered output reduces I/O impact

## Output Writers

### Configuration Section

```json
"OutputWriters": [
  {
    "Type": "ConsoleOutputWriter",
    "Enabled": true,
    "Format": "csv",
    "Destination": "console"
  },
  {
    "Type": "FileOutputWriter",
    "Enabled": true,
    "Format": "json",
    "Destination": "./data/events.log",
    "FlushInterval": 10
  }
]
```

### Properties

#### Type (string, required)
**Purpose**: Specifies which output writer implementation to use.

**Available Values**:
- `ConsoleOutputWriter` - Writes to stdout (console)
- `FileOutputWriter` - Writes to file

**Future Values** (require implementation):
- `DatabaseOutputWriter` - Write to SQL database
- `RestApiOutputWriter` - POST to REST endpoint
- `MessageQueueOutputWriter` - Publish to queue (RabbitMQ, etc.)

**Impact**:
- Application will fail to start if Type is unknown
- Can have multiple writers enabled simultaneously
- Each writer processes every event

**Example**:
```json
"Type": "ConsoleOutputWriter"
```

#### Enabled (boolean, required)
**Purpose**: Controls whether this writer is active.

**Values**:
- `true` - Writer will process events
- `false` - Writer will be ignored

**Impact**:
- Disabled writers are not instantiated (zero overhead)
- At least one writer must be enabled or app fails to start
- Can be used for temporary debugging (enable ConsoleOutputWriter)

**Example**:
```json
"Enabled": true
```

#### Format (string, required)
**Purpose**: Specifies output format.

**Supported Values**:
- `csv` - Comma-separated values
- `json` - JSON objects

**Impact**:

**CSV Format**:
```
WM_LBUTTONDOWN, 1234, "notepad.exe", 450, 300
```
- Attributes in order from `output_attributes`
- Strings quoted, numbers unquoted
- Efficient for parsing and storage
- Good for log analysis tools

**JSON Format**:
```json
{"os_event_type":"WM_LBUTTONDOWN","foreground_process_id":1234,"foreground_process_name":"notepad.exe","mouse.x":450,"mouse.y":300}
```
- One JSON object per line
- Attribute names included
- Easy to parse programmatically
- Better for structured processing

**Performance**:
- CSV is slightly faster (less overhead)
- JSON uses more disk space (attribute names)

**Example**:
```json
"Format": "csv"
```

#### Destination (string, required)
**Purpose**: Specifies where output goes.

**Values**:
- For `ConsoleOutputWriter`: `"console"` (value ignored)
- For `FileOutputWriter`: File path (absolute or relative)

**Path Rules**:
- **Relative paths**: Relative to executable directory
  - Example: `"./data/events.log"` → `<exe-dir>/data/events.log`
- **Absolute paths**: Full path
  - Example: `"C:/Logs/ContextSensor/events.log"`
- **Directory creation**: Directories are created automatically
- **File mode**: Append mode (existing file preserved)

**Impact**:
- Invalid paths cause application startup failure
- Insufficient permissions cause startup failure
- Large files can slow performance (use log rotation)

**Example**:
```json
"Destination": "./data/events.log"
```

#### FlushInterval (integer, optional, FileOutputWriter only)
**Purpose**: Controls how often file buffer is flushed to disk.

**Value**: Number of events before flushing (default: 100)

**Impact**:

**Low values** (e.g., 1-10):
- **Pros**: Less data loss on crash, near real-time file updates
- **Cons**: Higher I/O overhead, slower performance

**High values** (e.g., 100-1000):
- **Pros**: Better performance, less I/O overhead
- **Cons**: More data loss potential, delayed file updates

**Recommended**:
- **Development**: 1-10 (immediate visibility)
- **Production**: 100-500 (balance performance and safety)
- **High-volume**: 1000+ (prioritize performance)

**Automatic flush**:
- Application shutdown (graceful)
- At end of each file rotation

**Example**:
```json
"FlushInterval": 10
```

### Multiple Writers Example

You can enable multiple writers simultaneously:

```json
"OutputWriters": [
  {
    "Type": "ConsoleOutputWriter",
    "Enabled": true,
    "Format": "csv"
  },
  {
    "Type": "FileOutputWriter",
    "Enabled": true,
    "Format": "json",
    "Destination": "./data/events.json",
    "FlushInterval": 100
  },
  {
    "Type": "FileOutputWriter",
    "Enabled": true,
    "Format": "csv",
    "Destination": "./data/events.csv",
    "FlushInterval": 100
  }
]
```

**Effect**: Each event is written to console (CSV), JSON file, and CSV file.

## Storage Configuration

### Configuration Section

```json
"Storage": {
  "DataDirectory": "./data"
}
```

### Properties

#### DataDirectory (string, required)
**Purpose**: Base directory for all stateful data (event logs, persistent state).

**Value**: Directory path (absolute or relative)

**Path Rules**:
- **Relative paths**: Relative to executable directory
- **Directory creation**: Created automatically if doesn't exist
- **Permissions**: Must have write permissions

**Impact**:
- All `FileOutputWriter` relative paths are relative to executable, NOT this directory
- Used by application to organize data
- Future: May store state files, checkpoints, etc.

**Best Practice**:
- Use dedicated directory (e.g., `./data`)
- Exclude from version control
- Include in backup strategy
- Monitor disk space usage

**Example**:
```json
"DataDirectory": "./data"
```

## Logging Configuration

### Configuration Section

```json
"Logging": {
  "LogFilePath": "./logs/contextsensor.log",
  "MinimumLevel": "Information"
}
```

**Note**: Application uses **Serilog** for logging. Logs are separate from event data.

### Properties

#### LogFilePath (string, required)
**Purpose**: Path to application log file (not event data).

**Value**: File path (absolute or relative)

**Log Contents**:
- Application lifecycle (startup, shutdown)
- Configuration loading
- Error messages and exceptions
- Provider registration
- Performance warnings

**Log Rotation**:
- **Automatic**: Daily rotation
- **Retention**: Last 7 days kept
- **Format**: `contextsensor-YYYYMMDD.log`

**Example**:
```json
"LogFilePath": "./logs/contextsensor.log"
```

#### MinimumLevel (string, required)
**Purpose**: Minimum log level to record.

**Supported Values** (in order of verbosity):
- `Debug` - Detailed debugging information
- `Information` - General informational messages (default)
- `Warning` - Warning messages
- `Error` - Error messages only
- `Fatal` - Critical failures only

**Impact**:

**Debug**:
- Logs everything including attribute lookups, provider calls
- Very verbose, useful for troubleshooting
- Performance impact: Moderate
- **Use**: Development and debugging

**Information**:
- Logs startup, shutdown, configuration, event counts
- Balanced verbosity
- Performance impact: Low
- **Use**: Production (default)

**Warning**:
- Logs only warnings and errors
- Minimal logging
- Performance impact: Very low
- **Use**: Production with minimal logging

**Error**:
- Logs only errors and critical issues
- Almost silent
- **Use**: Production with issue-only logging

**Example**:
```json
"MinimumLevel": "Information"
```

## App Settings

### Configuration Section

```json
"AppSettings": {
  "ApplicationName": "ContextSensor",
  "Version": "1.0.0"
}
```

### Properties

#### ApplicationName (string, optional)
**Purpose**: Display name of the application.

**Impact**: 
- Appears in startup logs
- May be used in future telemetry
- No functional impact on operation

**Example**:
```json
"ApplicationName": "ContextSensor"
```

#### Version (string, optional)
**Purpose**: Application version identifier.

**Impact**: 
- Appears in startup logs
- May be used in compatibility checks (future)
- No current functional impact

**Example**:
```json
"Version": "1.0.0"
```

## Configuration Examples

### Minimal Configuration

**Capture mouse clicks, output to console only:**

**appsettings.json:**
```json
{
  "EventListeners": [
    { "Type": "MouseEventListener", "Enabled": true }
  ],
  "OutputWriters": [
    { "Type": "ConsoleOutputWriter", "Enabled": true, "Format": "csv", "Destination": "console" }
  ],
  "Storage": { "DataDirectory": "./data" },
  "Logging": { "LogFilePath": "./logs/app.log", "MinimumLevel": "Warning" },
  "AppSettings": { "ApplicationName": "ContextSensor", "Version": "1.0.0" }
}
```

**events.json:**
```json
{
  "events": [
    {
      "name": "mouse_click_down",
      "output_attributes": [
        { "name": "os_event_type", "datatype": "string" },
        { "name": "foreground_process_id", "datatype": "integer" },
        { "name": "foreground_process_name", "datatype": "string" }
      ]
    }
  ]
}
```

### Development Configuration

**Verbose logging, immediate file flush, console output:**

**appsettings.json:**
```json
{
  "EventListeners": [
    { "Type": "MouseEventListener", "Enabled": true }
  ],
  "OutputWriters": [
    { "Type": "ConsoleOutputWriter", "Enabled": true, "Format": "csv", "Destination": "console" },
    { "Type": "FileOutputWriter", "Enabled": true, "Format": "json", "Destination": "./data/dev-events.json", "FlushInterval": 1 }
  ],
  "Storage": { "DataDirectory": "./data" },
  "Logging": { "LogFilePath": "./logs/dev.log", "MinimumLevel": "Debug" },
  "AppSettings": { "ApplicationName": "ContextSensor-Dev", "Version": "1.0.0-dev" }
}
```

### Production Configuration

**File output only, optimized performance:**

**appsettings.json:**
```json
{
  "EventListeners": [
    { "Type": "MouseEventListener", "Enabled": true }
  ],
  "OutputWriters": [
    { "Type": "FileOutputWriter", "Enabled": true, "Format": "json", "Destination": "C:/ProgramData/ContextSensor/events.log", "FlushInterval": 500 }
  ],
  "Storage": { "DataDirectory": "C:/ProgramData/ContextSensor/data" },
  "Logging": { "LogFilePath": "C:/ProgramData/ContextSensor/logs/app.log", "MinimumLevel": "Warning" },
  "AppSettings": { "ApplicationName": "ContextSensor", "Version": "1.0.0" }
}
```

### Extended Attributes Configuration

**Capture additional mouse position data:**

**events.json:**
```json
{
  "events": [
    {
      "name": "mouse_click_down",
      "description": "Mouse click with full context",
      "output_attributes": [
        { "name": "os_event_type", "datatype": "string", "description": "Windows event type" },
        { "name": "foreground_process_id", "datatype": "integer", "description": "Active process PID" },
        { "name": "foreground_process_name", "datatype": "string", "description": "Active process name" },
        { "name": "mouse.x", "datatype": "integer", "description": "Mouse X coordinate" },
        { "name": "mouse.y", "datatype": "integer", "description": "Mouse Y coordinate" }
      ]
    }
  ]
}
```

## Best Practices

### Configuration Management

1. **Version Control**
   - ✅ Commit default configurations
   - ✅ Document configuration changes
   - ❌ Don't commit environment-specific settings
   - ✅ Use configuration templates

2. **Environment-Specific Configs**
   ```
   appsettings.json              ← Default
   appsettings.development.json  ← Development overrides
   appsettings.production.json   ← Production overrides
   ```

3. **Backup Strategy**
   - Back up configuration files before changes
   - Test configuration in development first
   - Keep versioned history of event schema changes

### Performance Tuning

1. **High-Volume Scenarios**
   ```json
   {
     "OutputWriters": [
       {
         "Type": "FileOutputWriter",
         "Format": "csv",              // CSV is faster than JSON
         "FlushInterval": 1000,        // Higher flush interval
         "Destination": "./data/events.csv"
       }
     ],
     "Logging": {
       "MinimumLevel": "Warning"       // Reduce logging overhead
     }
   }
   ```

2. **Low-Latency Requirements**
   ```json
   {
     "OutputWriters": [
       {
         "Type": "FileOutputWriter",
         "FlushInterval": 1            // Immediate flush
       }
     ]
   }
   ```

3. **Minimize Attributes**
   - Only capture attributes you need
   - Fewer attributes = faster processing
   - Consider separate event types for different use cases

### Security Considerations

1. **File Permissions**
   - Set appropriate permissions on data directory
   - Log files may contain sensitive information
   - Consider encrypting event data files

2. **Path Security**
   - Avoid user-controlled paths in configuration
   - Use absolute paths in production
   - Validate file paths before use

3. **Sensitive Data**
   - Be aware of captured process names
   - Mouse coordinates could reveal sensitive information
   - Consider data masking for compliance

### Monitoring and Maintenance

1. **Disk Space**
   - Monitor event log file sizes
   - Implement log rotation strategy
   - Alert on disk space thresholds

2. **Performance Monitoring**
   - Watch CPU usage in logs
   - Monitor event processing rate
   - Track flush performance

3. **Log Review**
   - Regularly review application logs
   - Check for warnings and errors
   - Monitor attribute provider performance

### Troubleshooting Common Issues

#### Application Won't Start

**Check**:
1. Configuration files exist in executable directory
2. JSON syntax is valid (no trailing commas)
3. Required fields are present
4. Event listener types are correct
5. Output writer types are correct

**Fix**:
```bash
# Validate JSON syntax
Get-Content appsettings.json | ConvertFrom-Json
Get-Content events.json | ConvertFrom-Json
```

#### No Events Captured

**Check**:
1. Event listener is `Enabled: true`
2. Event is defined in `events.json`
3. Event name matches in mapper
4. Application has required permissions
5. Check logs for errors

#### Output Not Appearing

**Check**:
1. At least one output writer is enabled
2. File paths are valid and writable
3. Check flush interval (may be buffered)
4. Look for errors in logs

#### High CPU Usage

**Solutions**:
1. Reduce logging level to `Warning` or `Error`
2. Increase flush interval
3. Use CSV format instead of JSON
4. Reduce number of output attributes
5. Disable unnecessary event listeners

#### Large Log Files

**Solutions**:
1. Implement external log rotation
2. Reduce flush interval (process more frequently)
3. Use separate files for different event types
4. Compress old log files
5. Archive to external storage

---

## Quick Reference

### Configuration File Checklist

**appsettings.json**:
- [ ] At least one event listener enabled
- [ ] At least one output writer enabled
- [ ] Valid file paths
- [ ] Appropriate flush intervals
- [ ] Correct log level

**events.json**:
- [ ] All events have unique names
- [ ] Attribute names match registry
- [ ] Datatypes are valid
- [ ] Output attributes are in desired order

### Common Configuration Tasks

**Add new attribute to output**:
1. Add to `output_attributes` in `events.json`
2. Restart application

**Change output format**:
1. Update `Format` field in `appsettings.json`
2. Restart application

**Enable/disable event capture**:
1. Update `Enabled` field in `EventListeners`
2. Restart application

**Change log verbosity**:
1. Update `MinimumLevel` in `Logging`
2. Restart application

---

For setup instructions, see [`SETUP_WINDOWS.md`](SETUP_WINDOWS.md).

For architectural details, see [`DESIGN.md`](DESIGN.md).

For requirements, see [`REQUIREMENTS.md`](REQUIREMENTS.md).
