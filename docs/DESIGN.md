# ContextSensor - Software Design Document

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Design Principles](#design-principles)
4. [Core Components](#core-components)
5. [Design Patterns](#design-patterns)
6. [Data Flow](#data-flow)
7. [Extensibility](#extensibility)
8. [Design Strengths](#design-strengths)

## Overview

ContextSensor is a Windows event monitoring application built with a mature, enterprise-grade architecture. It captures system events (currently mouse clicks) and logs detailed contextual information about the foreground application and event characteristics.

### Key Architectural Goals
- **Configuration-Driven**: Events and attributes defined declaratively in JSON
- **Platform Abstraction**: OS-specific code isolated from core logic
- **Extensible Design**: Easy to add new events, attributes, and output formats
- **Production-Ready**: Thread-safe, async, with graceful error handling
- **SOLID Principles**: Clean architecture following industry best practices

## Architecture

### Three-Layer Architecture

```
╔═══════════════════════════════════════════════════════════════════════╗
║                        ContextSensor.App Layer                        ║
║                     (Application Entry Point)                         ║
╠═══════════════════════════════════════════════════════════════════════╣
║  Program.cs                                                           ║
║  ├─ DI Container Configuration                                        ║
║  ├─ Service Registration                                              ║
║  ├─ Application Lifecycle                                             ║
║  └─ Startup & Shutdown Logic                                          ║
║                                                                       ║
║  Configuration Files:                                                 ║
║  ├─ appsettings.json  (App behavior)                                  ║
║  └─ events.json       (Event schema)                                  ║
╚═══════════════════════════════════════════════════════════════════════╝
                                  │
                                  │ depends on
                                  ▼
╔═══════════════════════════════════════════════════════════════════════╗
║                        ContextSensor.Core Layer                       ║
║                   (Platform-Agnostic Business Logic)                  ║
╠═══════════════════════════════════════════════════════════════════════╣
║  Attributes/              Events/                Output/              ║
║  ├─ AttributeRegistry     ├─ EventOrchestrator  ├─ IOutputWriter     ║
║  ├─ IAttributeProvider    ├─ IEventListener     ├─ OutputWriterBase  ║
║  ├─ ProviderRegistry      ├─ IEventProcessor    ├─ ConsoleWriter     ║
║  └─ EventAttributeProv.   ├─ IEventData         └─ FileWriter        ║
║                           └─ IEventTypeMapper                         ║
║  Configuration/                                                       ║
║  ├─ SensorConfiguration           OS/                                ║
║  ├─ EventSchema                   └─ IProcessInformationProvider      ║
║  └─ JsonConfigLoader                                                  ║
╚═══════════════════════════════════════════════════════════════════════╝
                                  │
                                  │ implements
                                  ▼
╔═══════════════════════════════════════════════════════════════════════╗
║                     ContextSensor.Windows Layer                       ║
║                  (Windows-Specific Implementations)                   ║
╠═══════════════════════════════════════════════════════════════════════╣
║  Events/                         Attributes/                          ║
║  ├─ MouseEventListener           ├─ MouseAttributeProvider            ║
║  ├─ MouseEventData               └─ ForegroundProcessAttributeProv.   ║
║  └─ WindowsEventTypeMapper                                            ║
║                                  OS/                                  ║
║  Win32 APIs Used:                └─ WindowsProcessInfoProvider        ║
║  ├─ SetWindowsHookEx (mouse)                                          ║
║  ├─ GetForegroundWindow          Win32 APIs:                          ║
║  └─ GetWindowThreadProcessId     ├─ GetForegroundWindow              ║
║                                  └─ GetWindowThreadProcessId          ║
╚═══════════════════════════════════════════════════════════════════════╝
```

### Component Interaction Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Application Startup                            │
└────────────┬─────────────────────────────────────────────────────────┘
             │
             ├─> Load Configuration (appsettings.json + events.json)
             │
             ├─> Initialize DI Container
             │   ├─> Register Core Services
             │   ├─> Register Platform Services (Windows)
             │   └─> Register Logging (Serilog)
             │
             ├─> Initialize Attribute System
             │   ├─> AttributeRegistry.Initialize()
             │   └─> Register Attribute Providers
             │       ├─> EventAttributeProvider
             │       ├─> ForegroundProcessAttributeProvider
             │       └─> MouseAttributeProvider
             │
             ├─> Create Event Listeners (from config)
             │   └─> MouseEventListener
             │       └─> SetWindowsHookEx(WH_MOUSE_LL)
             │
             ├─> Create Output Writers (from config)
             │   ├─> ConsoleOutputWriter
             │   └─> FileOutputWriter
             │
             ├─> Create Event Processor
             │   └─> AttributeBasedEventProcessor
             │
             ├─> Create & Start EventOrchestrator
             │   ├─> Subscribe to Listeners
             │   ├─> Start Background Processing
             │   └─> Start Event Listeners
             │
             └─> Enter Message Pump (Windows)
                 └─> Application.Run(HiddenForm)
```

### Project Structure

#### ContextSensor.Core (Platform-Agnostic)
```
Core/
├── Attributes/           # Attribute definition and provider system
│   ├── AttributeDefinition.cs          # Component & attribute registry
│   ├── IAttributeProvider.cs           # Provider interface & base class
│   ├── AttributeProviderRegistry.cs    # Provider management
│   └── EventAttributeProvider.cs       # Global event attributes
├── Configuration/        # Configuration management
│   ├── SensorConfiguration.cs          # Main config model
│   ├── EventSchema.cs                  # Event definition schema
│   ├── IConfigurationLoader.cs         # Loader interface
│   └── JsonConfigurationLoader.cs      # JSON implementation
├── Events/              # Event processing pipeline
│   ├── IEventData.cs                   # Event data interface
│   ├── IEventListener.cs               # Listener interface
│   ├── IEventProcessor.cs              # Processor interface
│   ├── IEventTypeMapper.cs             # OS event type mapping
│   ├── EventOrchestrator.cs            # Main coordinator
│   └── AttributeBasedEventProcessor.cs # Attribute enrichment
├── Output/              # Output writer system
│   ├── IOutputWriter.cs                # Writer interface
│   ├── OutputWriterBase.cs             # Base with formatting
│   ├── ConsoleOutputWriter.cs          # Console output
│   ├── FileOutputWriter.cs             # File output
│   └── ValueFormatter.cs               # Type-safe formatting
└── OS/                  # OS abstraction
    └── IProcessInformationProvider.cs  # Process info interface
```

#### ContextSensor.Windows (Windows-Specific)
```
Windows/
├── Attributes/          # Windows attribute providers
│   ├── MouseAttributeProvider.cs       # Mouse position data
│   └── ForegroundProcessAttributeProvider.cs  # Active process
├── Events/              # Windows event listeners
│   ├── MouseEventData.cs               # Mouse event model
│   ├── MouseEventListener.cs           # Low-level mouse hook
│   └── WindowsEventTypeMapper.cs       # WM_* to schema mapping
└── OS/                  # Windows OS implementations
    └── WindowsProcessInformationProvider.cs  # Win32 process APIs
```

## Design Principles

### 1. SOLID Principles

```
┌────────────────────────────────────────────────────────────────────────┐
│                      SOLID Principles in ContextSensor                  │
└────────────────────────────────────────────────────────────────────────┘

S - Single Responsibility Principle
┌─────────────────────────────────────────────────────────────────────┐
│  Each class has ONE reason to change                                 │
├─────────────────────────────────────────────────────────────────────┤
│  EventOrchestrator          → Coordinates event flow only           │
│  AttributeBasedEventProc.   → Enriches events with attributes       │
│  MouseEventListener         → Captures mouse events only            │
│  ConsoleOutputWriter        → Writes to console only                │
│  JsonConfigurationLoader    → Loads configuration only              │
└─────────────────────────────────────────────────────────────────────┘

O - Open/Closed Principle
┌─────────────────────────────────────────────────────────────────────┐
│  Open for extension, closed for modification                         │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐         ┌──────────────────────────┐             │
│  │IEventListener│◄────────┤ Just implement interface │             │
│  └──────────────┘         │ No core code changes     │             │
│        △                  └──────────────────────────┘             │
│        │                                                            │
│    ┌───┴────┐         Add new listener types:                      │
│    │        │         • KeyboardEventListener                       │
│  Mouse   Keyboard    • ClipboardEventListener                      │
│  Listener Listener   • USBEventListener                            │
│                                                                     │
│  Configuration-driven registration:                                 │
│  { "Type": "KeyboardEventListener", "Enabled": true }              │
└─────────────────────────────────────────────────────────────────────┘

L - Liskov Substitution Principle
┌─────────────────────────────────────────────────────────────────────┐
│  Subtypes must be substitutable for their base types                │
├─────────────────────────────────────────────────────────────────────┤
│  IEventListener listener;                                           │
│                                                                     │
│  listener = new MouseEventListener(...);      ✓ Works              │
│  listener = new KeyboardEventListener(...);   ✓ Works              │
│  listener = new ClipboardEventListener(...);  ✓ Works              │
│                                                                     │
│  EventOrchestrator doesn't care which implementation!               │
│  All implementations work identically in the pipeline.              │
└─────────────────────────────────────────────────────────────────────┘

I - Interface Segregation Principle
┌─────────────────────────────────────────────────────────────────────┐
│  Many client-specific interfaces better than one general-purpose    │
├─────────────────────────────────────────────────────────────────────┤
│  Small, Focused Interfaces:                                         │
│                                                                     │
│  IEventListener      → StartAsync(), StopAsync(), EventCaptured     │
│  IOutputWriter       → WriteAsync(), FlushAsync()                   │
│  IEventProcessor     → ProcessAsync()                               │
│  IAttributeProvider  → GetAttributeValue(), GetAttributeValues()    │
│  IProcessInfo...     → GetProcessName(), GetForegroundProcessId()   │
│                                                                     │
│  Each interface has ONLY methods relevant to its purpose            │
│  No "god interface" forcing implementations to stub unused methods  │
└─────────────────────────────────────────────────────────────────────┘

D - Dependency Inversion Principle
┌─────────────────────────────────────────────────────────────────────┐
│  Depend on abstractions, not concretions                            │
├─────────────────────────────────────────────────────────────────────┤
│  High-Level Module (EventOrchestrator)                              │
│            │                                                        │
│            │ depends on                                             │
│            ▼                                                        │
│  ┌──────────────────┐                                               │
│  │   Abstractions   │                                               │
│  │  (Interfaces)    │                                               │
│  ├──────────────────┤                                               │
│  │ IEventListener   │                                               │
│  │ IOutputWriter    │                                               │
│  │ IEventProcessor  │                                               │
│  └────────┬─────────┘                                               │
│           │ implemented by                                          │
│           ▼                                                         │
│  ┌──────────────────┐                                               │
│  │ Low-Level Modules│                                               │
│  ├──────────────────┤                                               │
│  │ MouseEventList.  │                                               │
│  │ FileOutputWriter │                                               │
│  │ AttributeBasedP..│                                               │
│  └──────────────────┘                                               │
│                                                                     │
│  Benefits:                                                          │
│  • High-level code doesn't change when low-level code changes       │
│  • Easy to swap implementations (testing, platforms)                │
│  • Loose coupling enables independent development                   │
└─────────────────────────────────────────────────────────────────────┘
```

#### Single Responsibility Principle (SRP)
Each class has ONE clearly defined responsibility:
- [`EventOrchestrator`](../src/ContextSensor.Core/Events/EventOrchestrator.cs) - Coordinates event flow
- [`AttributeBasedEventProcessor`](../src/ContextSensor.Core/Events/AttributeBasedEventProcessor.cs) - Enriches events with attributes
- [`MouseEventListener`](../src/ContextSensor.Windows/Events/MouseEventListener.cs) - Captures mouse events
- [`ConsoleOutputWriter`](../src/ContextSensor.Core/Output/ConsoleOutputWriter.cs) - Writes to console
- [`JsonConfigurationLoader`](../src/ContextSensor.Core/Configuration/JsonConfigurationLoader.cs) - Loads configuration

#### Open/Closed Principle (OCP)
**Open for extension, closed for modification:**

**Adding new event listeners** - No core code changes needed:
```csharp
// Just implement the interface
public class KeyboardEventListener : IEventListener { }

// Register in configuration
{ "Type": "KeyboardEventListener", "Enabled": true }
```

**Adding new attribute providers** - Register without modifying core:
```csharp
registry.RegisterProvider(new NetworkAttributeProvider());
```

**Adding new output formats** - Extend base class:
```csharp
public class XmlOutputWriter : OutputWriterBase { }
```

#### Liskov Substitution Principle (LSP)
All interface implementations are fully substitutable:
```csharp
IEventListener listener = new MouseEventListener(...);
IEventListener listener = new KeyboardEventListener(...); // Future
// Both work identically in EventOrchestrator
```

#### Interface Segregation Principle (ISP)
Small, focused interfaces prevent unnecessary dependencies:
- [`IEventListener`](../src/ContextSensor.Core/Events/IEventListener.cs) - Only event listening concerns
- [`IOutputWriter`](../src/ContextSensor.Core/Output/IOutputWriter.cs) - Only output writing concerns
- [`IEventProcessor`](../src/ContextSensor.Core/Events/IEventProcessor.cs) - Only event processing concerns
- [`IAttributeProvider`](../src/ContextSensor.Core/Attributes/IAttributeProvider.cs) - Only attribute provision concerns

#### Dependency Inversion Principle (DIP)
High-level modules depend on abstractions, not concretions:
```csharp
// Good: Depends on interface
public class EventOrchestrator
{
    private readonly IEnumerable<IEventListener> _listeners;
    private readonly IEnumerable<IOutputWriter> _writers;
    private readonly IEventProcessor _processor;
}

// Not: new MouseEventListener() or new FileOutputWriter()
```

### 2. Separation of Concerns

#### Platform Abstraction
Windows-specific code is isolated:
```csharp
// Core defines the contract
public interface IProcessInformationProvider
{
    string GetProcessName(int processId);
    int GetForegroundProcessId();
}

// Windows implements with Win32 APIs
public class WindowsProcessInformationProvider : IProcessInformationProvider
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    // ...
}
```

Future OS support is straightforward:
```csharp
public class LinuxProcessInformationProvider : IProcessInformationProvider { }
public class MacOSProcessInformationProvider : IProcessInformationProvider { }
```

#### Configuration-Driven Behavior
Business logic is driven by configuration, not hardcoded:

**Event Schema** ([`events.json`](../src/ContextSensor.App/events.json)):
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

**Application Configuration** ([`appsettings.json`](../src/ContextSensor.App/appsettings.json)):
```json
{
  "EventListeners": [
    { "Type": "MouseEventListener", "Enabled": true }
  ],
  "OutputWriters": [
    { "Type": "ConsoleOutputWriter", "Enabled": true, "Format": "csv" },
    { "Type": "FileOutputWriter", "Enabled": true, "Format": "json" }
  ]
}
```

## Core Components

### 1. Event Listening System

#### IEventListener Interface
Defines contract for event capture:
```csharp
public interface IEventListener : IDisposable
{
    string Name { get; }
    event EventHandler<IEventData>? EventCaptured;
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
    bool IsListening { get; }
}
```

#### MouseEventListener Implementation
Captures mouse events using Windows low-level hooks:
- **Win32 Hook**: `SetWindowsHookEx(WH_MOUSE_LL, ...)`
- **Event Types**: WM_LBUTTONDOWN, WM_RBUTTONDOWN, WM_MBUTTONDOWN
- **Data Captured**: Coordinates (x, y), process ID, process name
- **Thread Safety**: Uses `Interlocked` for state management

### 2. Attribute System

The attribute system is the heart of ContextSensor's extensibility.

#### Attribute System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Attribute System Overview                       │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────────────┐         ┌──────────────────────────────┐
│   AttributeRegistry      │         │  AttributeProviderRegistry   │
│   (Static, Singleton)    │         │  (Instance, DI Container)    │
├──────────────────────────┤         ├──────────────────────────────┤
│ Components:              │         │ Registered Providers:        │
│  ├─ process              │         │  ├─ EventAttributeProvider   │
│  │   ├─ id               │◄────────┤  ├─ MouseAttributeProvider   │
│  │   └─ name             │  lookup │  └─ ForegroundProcessAttr... │
│  ├─ mouse                │         │                              │
│  │   ├─ x                │         │ Attribute → Provider Map:    │
│  │   └─ y                │         │  "os_event_type" → Event...  │
│  └─ Global Attributes    │         │  "mouse.x" → Mouse...        │
│      ├─ os_event_type    │         │  "foreground_..." → Fore...  │
│      └─ foreground_*     │         └──────────────────────────────┘
└──────────────────────────┘                      │
                                                  │
                                                  ▼
                                    ┌─────────────────────────────┐
                                    │    IAttributeProvider       │
                                    │      (Interface)            │
                                    ├─────────────────────────────┤
                                    │ + ProviderName: string      │
                                    │ + SupportedAttributes       │
                                    │ + GetAttributeValue(...)    │
                                    │ + GetAttributeValues(...)   │
                                    └─────────────────────────────┘
                                               △
                          ┌────────────────────┼────────────────────┐
                          │                    │                    │
         ┌────────────────┴────────┐  ┌────────┴────────┐  ┌───────┴──────────┐
         │ EventAttributeProvider  │  │ MouseAttribute  │  │ ForegroundProcess│
         │                         │  │   Provider      │  │ AttributeProvider│
         ├─────────────────────────┤  ├─────────────────┤  ├──────────────────┤
         │ Provides:               │  │ Provides:       │  │ Provides:        │
         │  • os_event_type        │  │  • mouse.x      │  │  • foreground_*  │
         │  • timestamp            │  │  • mouse.y      │  │                  │
         │                         │  │                 │  │ Uses:            │
         │ Source:                 │  │ Source:         │  │  • Win32 APIs    │
         │  • Event metadata       │  │  • Event data   │  │  • Caching       │
         └─────────────────────────┘  └─────────────────┘  └──────────────────┘
```

#### Core Concepts

**Components** - Logical groupings of related attributes:
```
┌─────────────────────────────────────────────────────────────┐
│                     Component Hierarchy                      │
└─────────────────────────────────────────────────────────────┘

Component: "process"
  ├── Attribute: "id"              → Qualified: "process.id"
  │   ├─ DataType: Integer
  │   ├─ Description: "Process identifier (PID)"
  │   └─ Provider: IProcessInformationProvider
  │
  └── Attribute: "name"            → Qualified: "process.name"
      ├─ DataType: String
      ├─ Description: "Executable name"
      └─ Provider: IProcessInformationProvider

Component: "mouse"
  ├── Attribute: "x"               → Qualified: "mouse.x"
  │   ├─ DataType: Integer
  │   ├─ Description: "X coordinate"
  │   └─ Provider: IMouseProvider
  │
  └── Attribute: "y"               → Qualified: "mouse.y"
      ├─ DataType: Integer
      ├─ Description: "Y coordinate"
      └─ Provider: IMouseProvider

Global Attributes (no component):
  ├── "os_event_type"              → Qualified: "os_event_type"
  │   ├─ DataType: String
  │   └─ Provider: IEventProvider
  │
  ├── "foreground_process_id"      → Qualified: "foreground_process_id"
  │   ├─ DataType: Integer
  │   ├─ References: "process.id"
  │   └─ Provider: IForegroundProcessAttributeProvider
  │
  └── "foreground_process_name"    → Qualified: "foreground_process_name"
      ├─ DataType: String
      ├─ References: "process.name"
      └─ Provider: IForegroundProcessAttributeProvider
```

**Attributes** - Individual data points with metadata:
```csharp
public class AttributeDefinition
{
    public string Id { get; set; }           // "id"
    public string Name { get; set; }          // "Process ID"
    public AttributeDataType DataType { get; set; }  // Integer
    public string ComponentId { get; set; }   // "process"
    public string QualifiedName { get; }      // "process.id"
}
```

**Providers** - Sources that supply attribute values:
```csharp
public interface IAttributeProvider
{
    string ProviderName { get; }
    IEnumerable<string> SupportedAttributes { get; }
    object? GetAttributeValue(string attributeQualifiedName, AttributeContext? context);
    Dictionary<string, object?> GetAttributeValues(IEnumerable<string> attributeQualifiedNames, AttributeContext? context);
}
```

#### Attribute Retrieval Flow

```
┌──────────────────────────────────────────────────────────────────┐
│            Attribute Retrieval Sequence Diagram                   │
└──────────────────────────────────────────────────────────────────┘

EventProcessor          ProviderRegistry         Provider           Cache
     │                        │                      │                │
     │ GetAttributeValues     │                      │                │
     │  (["os_event_type",    │                      │                │
     │    "mouse.x",          │                      │                │
     │    "mouse.y"])         │                      │                │
     ├───────────────────────>│                      │                │
     │                        │                      │                │
     │                        │ Group by Provider    │                │
     │                        │ - Event: ["os_..."] │                │
     │                        │ - Mouse: ["mouse.x", "mouse.y"]       │
     │                        │                      │                │
     │                        │ GetAttributeValues   │                │
     │                        │  (["mouse.x",        │                │
     │                        │    "mouse.y"])       │                │
     │                        ├─────────────────────>│                │
     │                        │                      │                │
     │                        │                      │ Check Cache    │
     │                        │                      ├───────────────>│
     │                        │                      │                │
     │                        │                      │ Cache Miss     │
     │                        │                      │<───────────────┤
     │                        │                      │                │
     │                        │                      │ Extract from   │
     │                        │                      │ EventData      │
     │                        │                      │                │
     │                        │                      │ Store in Cache │
     │                        │                      ├───────────────>│
     │                        │                      │                │
     │                        │ { "mouse.x": 450,    │                │
     │                        │   "mouse.y": 300 }   │                │
     │                        │<─────────────────────┤                │
     │                        │                      │                │
     │  { "os_event_type": ...│                      │                │
     │    "mouse.x": 450,     │                      │                │
     │    "mouse.y": 300 }    │                      │                │
     │<───────────────────────┤                      │                │
     │                        │                      │                │
```

#### Attribute Registry
Central registry of all available attributes:
- Initialized at startup with standard components
- Provides lookup by qualified name
- Supports custom derived attributes
- Thread-safe singleton pattern

#### Attribute Provider Registry
Manages provider instances:
- Maps attributes to their providers
- Handles batch attribute retrieval for efficiency
- Provides provider statistics
- Thread-safe with `ConcurrentDictionary`

#### Built-in Providers

**EventAttributeProvider**:
- Supplies: `os_event_type`, `timestamp`
- Source: Event metadata

**MouseAttributeProvider**:
- Supplies: `mouse.x`, `mouse.y`
- Source: Mouse hook structure

**ForegroundProcessAttributeProvider**:
- Supplies: `foreground_process_id`, `foreground_process_name`
- Source: Win32 GetForegroundWindow API
- Optimization: Caches process ID, reuses for name lookup

### 3. Event Processing Pipeline

#### EventOrchestrator
Coordinates the entire event pipeline:

```
┌────────────────────────────────────────────────────────────────────────┐
│                    Event Processing Pipeline                            │
└────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┐
│   Event Listeners    │
│  ┌────────────────┐  │         Observer Pattern
│  │ Mouse Listener │  │◄─────── (EventCaptured event)
│  └────────────────┘  │
│  ┌────────────────┐  │
│  │Keyboard Listener│  │
│  │   (Future)     │  │
│  └────────────────┘  │
└──────────┬───────────┘
           │ Fire Event: EventCaptured
           │ EventHandler<IEventData>
           ▼
┌──────────────────────────────────────────────────────────┐
│              EventOrchestrator                            │
│  ┌──────────────────────────────────────────────────┐    │
│  │        OnEventCaptured(sender, eventData)        │    │
│  │               │                                  │    │
│  │               │ TryWrite                         │    │
│  │               ▼                                  │    │
│  │     ┌─────────────────────┐                     │    │
│  │     │  Channel<IEventData>│  ◄── Producer       │    │
│  │     │   (Unbounded)       │                     │    │
│  │     └─────────┬───────────┘                     │    │
│  │               │                                  │    │
│  │               │ ReadAllAsync                     │    │
│  │               ▼                                  │    │
│  │    ┌──────────────────────┐                     │    │
│  │    │  ProcessEventsAsync  │  ◄── Consumer       │    │
│  │    │  (Background Task)   │                     │    │
│  │    └──────────┬───────────┘                     │    │
│  └───────────────┼──────────────────────────────────┘    │
└──────────────────┼───────────────────────────────────────┘
                   │ foreach event
                   ▼
          ┌────────────────────┐
          │  Event Processor   │
          │  (Attribute Based) │
          └────────┬───────────┘
                   │ Enriched Event
                   │ (with attributes)
                   ▼
          ┌─────────────────────────┐
          │   Output Writers        │
          │  ┌─────────────────┐    │
          │  │ Console Writer  │───►│ stdout
          │  └─────────────────┘    │
          │  ┌─────────────────┐    │
          │  │  File Writer    │───►│ ./data/events.log
          │  └─────────────────┘    │
          │  ┌─────────────────┐    │
          │  │  Database (*)   │───►│ SQL Server
          │  └─────────────────┘    │
          └─────────────────────────┘

Thread Safety:
├─ Interlocked.CompareExchange for state (_isRunning)
├─ SemaphoreSlim for writer synchronization
└─ ConcurrentDictionary in registries

Error Handling:
├─ Try-catch around event processing
├─ Isolated failures (one event error doesn't stop pipeline)
└─ Graceful degradation (partial attribute retrieval)

Shutdown Flow:
1. Cancel CancellationToken
2. Unsubscribe from listeners
3. Stop listeners
4. Complete channel (Writer.Complete())
5. Await background task completion
6. Flush all writers
7. Dispose resources
```

**Key Features**:
- **Async Channel**: `Channel<IEventData>` for decoupled processing
- **Graceful Shutdown**: Completes channel, flushes writers
- **Error Isolation**: Exceptions don't crash the pipeline
- **Thread Safety**: `Interlocked` for state, `SemaphoreSlim` for coordination

#### AttributeBasedEventProcessor
Enriches events with configured attributes:

1. **Find Event Definition**: Maps OS event type to schema
2. **Extract Attributes**: Gets list from event definition
3. **Create Context**: Bundles event data + metadata
4. **Batch Retrieval**: Calls `GetAttributeValues()` once per provider
5. **Store Results**: Adds to event metadata as `attr_*`

**Optimization**: Batches attribute requests to minimize provider calls.

#### Event Type Mapping
Abstracts OS-specific event types:

```csharp
public interface IEventTypeMapper
{
    string MapToSchemaName(string osEventType);
}

// Windows implementation
"WM_LBUTTONDOWN" → "mouse_click_down"
"WM_RBUTTONDOWN" → "mouse_click_down"
"WM_MBUTTONDOWN" → "mouse_click_down"
```

This allows:
- Schema portability across OS platforms
- Multiple OS events mapping to same logical event
- Easy configuration changes without code modification

### 4. Output System

#### OutputWriterBase
Provides formatting foundation:

**Supported Formats**:
- **CSV**: Comma-separated values
- **JSON**: Structured JSON objects

**Attribute-Based Formatting**:
```csharp
// Reads event schema from metadata
EventDefinition eventDef = eventData.Metadata["EventDefinition"];

// Formats only configured attributes in order
foreach (var attr in eventDef.OutputAttributes)
{
    var value = eventData.Metadata[$"attr_{attr.Name}"];
    parts.Add(FormatValue(value, attr.Datatype));
}
```

#### ConsoleOutputWriter
- Writes to `Console.Out` (stdout)
- Thread-safe with `SemaphoreSlim`
- No buffering (immediate output)

#### FileOutputWriter
- Appends to file (creates directory if needed)
- Buffered writes with configurable flush interval
- Thread-safe file access
- Auto-flush on shutdown

### 5. Configuration System

#### Two-File Configuration

**appsettings.json** - Application settings:
```json
{
  "EventListeners": [...],    // Which listeners to enable
  "OutputWriters": [...],     // Output configuration
  "Storage": {...},           // Data directory
  "Logging": {...},           // Log configuration
  "AppSettings": {...}        // App metadata
}
```

**events.json** - Event schema:
```json
{
  "events": [
    {
      "name": "mouse_click_down",           // Schema event name
      "description": "...",                 // Documentation
      "output_attributes": [                // What to capture
        { "name": "os_event_type", "datatype": "string" },
        { "name": "foreground_process_id", "datatype": "integer" }
      ]
    }
  ]
}
```

**Why Separate Files?**
1. **Concerns**: App behavior vs. data schema
2. **Versioning**: Schema can be versioned independently
3. **Updates**: Can update event definitions without app changes
4. **Clarity**: Cleaner separation for users

#### Configuration Validation
[`JsonConfigurationLoader`](../src/ContextSensor.Core/Configuration/JsonConfigurationLoader.cs) validates:
- Required files exist
- JSON is valid
- Required fields are present
- Event listeners have types
- Output writers have types

## Design Patterns

### Design Patterns Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│              Design Patterns Used in ContextSensor                    │
└──────────────────────────────────────────────────────────────────────┘

1. Strategy Pattern                    2. Provider Pattern
   ┌────────────────┐                     ┌──────────────────────┐
   │  IEventListener│◄────────────┐       │  IAttributeProvider  │
   └────────────────┘              │       └──────────────────────┘
          △                        │                △
          │                        │                │
    ┌─────┴─────┐                 │        ┌───────┴────────┐
    │           │                 │        │                │
┌───┴───┐  ┌────┴────┐            │  ┌─────┴─────┐  ┌──────┴─────┐
│ Mouse │  │Keyboard │            │  │   Event   │  │   Mouse    │
│Listener  │Listener │            │  │ Provider  │  │  Provider  │
└────────┘ └─────────┘            │  └───────────┘  └────────────┘
                                  │
        Used by:                  │         Managed by:
   ┌────────────────┐             │    ┌──────────────────────┐
   │EventOrchestrator├─────────────┘    │AttributeProviderReg. │
   └────────────────┘                   └──────────────────────┘


3. Registry Pattern                    4. Observer Pattern
   ┌──────────────────┐                   ┌─────────────────┐
   │AttributeRegistry │                   │  IEventListener │
   │    (Static)      │                   └────────┬────────┘
   └────────┬─────────┘                            │
            │ Contains                             │ event
   ┌────────┴──────────┐                          │ EventCaptured
   │  Components Map   │                          ▼
   │  Attributes Map   │                 ┌─────────────────┐
   └───────────────────┘                 │EventOrchestrator│
                                        └─────────────────┘
                                              (Subscriber)

5. Template Method Pattern             6. Dependency Injection
   ┌──────────────────┐                   ┌──────────────────┐
   │OutputWriterBase  │                   │  DI Container    │
   │  (Abstract)      │                   │   (Startup)      │
   ├──────────────────┤                   └────────┬─────────┘
   │ FormatEventData()│◄─── Template              │
   │ WriteAsync()     │◄─── Abstract              │ Injects
   │ GetCsvFallback() │◄─── Hook                  ▼
   └────────┬─────────┘              ┌──────────────────────────┐
            │                        │    All Components       │
       ┌────┴────┐                   │  ┌─────────────────┐    │
       │         │                   │  │  Interfaces     │    │
  ┌────┴───┐ ┌──┴─────┐              │  │  (Abstractions) │    │
  │Console │ │  File  │              │  └─────────────────┘    │
  │ Writer │ │ Writer │              │  ┌─────────────────┐    │
  └────────┘ └────────┘              │  │ Implementations │    │
                                    │  └─────────────────┘    │
                                    └─────────────────────────┘

7. Channel Pattern (Producer-Consumer)
   ┌─────────────┐
   │  Producers  │
   │ (Listeners) │
   └──────┬──────┘
          │ Write
          ▼
   ┌──────────────────┐
   │Channel<IEventData>│  ◄── Thread-safe queue
   └──────┬───────────┘
          │ Read
          ▼
   ┌──────────────┐
   │   Consumer   │
   │(Orchestrator)│
   └──────────────┘
```

### 1. Strategy Pattern
**Problem**: Need pluggable event listeners and output writers.

**Solution**: Define interfaces for interchangeable implementations.

```csharp
// Strategy interface
public interface IEventListener { }

// Concrete strategies
public class MouseEventListener : IEventListener { }
public class KeyboardEventListener : IEventListener { }  // Future

// Context uses strategies
public class EventOrchestrator
{
    private readonly List<IEventListener> _listeners;
}
```

**Benefits**:
- Add new listeners without modifying EventOrchestrator
- Runtime selection based on configuration
- Easy testing with mock implementations

### 2. Provider Pattern
**Problem**: Need extensible attribute collection from various sources.

**Solution**: Provider interface with registry for discovery.

```csharp
public interface IAttributeProvider
{
    string ProviderName { get; }
    IEnumerable<string> SupportedAttributes { get; }
    object? GetAttributeValue(string attributeName, AttributeContext? context);
}

public class AttributeProviderRegistry
{
    public void RegisterProvider(IAttributeProvider provider);
    public object? GetAttributeValue(string attributeName, AttributeContext? context);
}
```

**Benefits**:
- Dynamic registration of attribute sources
- Loose coupling between attributes and their sources
- Efficient batch retrieval with grouping

### 3. Registry Pattern
**Problem**: Need central location for attribute definitions and provider lookup.

**Solution**: Static registry for attributes, instance registry for providers.

```csharp
public static class AttributeRegistry
{
    public static AttributeDefinition? GetAttribute(string qualifiedName);
    public static ComponentDefinition? GetComponent(string componentId);
}

public class AttributeProviderRegistry
{
    public IAttributeProvider? GetProviderForAttribute(string attributeName);
}
```

**Benefits**:
- Centralized attribute metadata
- Fast lookups with dictionary storage
- Thread-safe access patterns

### 4. Observer Pattern
**Problem**: Event listeners need to notify orchestrator of events.

**Solution**: .NET events for loose coupling.

```csharp
public interface IEventListener
{
    event EventHandler<IEventData>? EventCaptured;
}

// Orchestrator subscribes
listener.EventCaptured += OnEventCaptured;
```

**Benefits**:
- Loose coupling between listener and orchestrator
- Multiple subscribers possible
- Standard .NET event pattern

### 5. Template Method Pattern
**Problem**: Output writers share formatting logic but differ in destination.

**Solution**: Base class with abstract methods.

```csharp
public abstract class OutputWriterBase : IOutputWriter
{
    protected string FormatEventData(IEventData eventData) { }  // Common
    public abstract Task WriteAsync(IEventData eventData);      // Specific
    protected virtual string GetCsvFallback(IEventData eventData) { }  // Hook
}
```

**Benefits**:
- Reuse formatting logic across writers
- Customization points via hooks
- Consistent output formatting

### 6. Dependency Injection Pattern
**Problem**: Need loose coupling and testability.

**Solution**: Constructor injection with IoC container.

```csharp
// Register services
services.AddSingleton<IConfigurationLoader, JsonConfigurationLoader>();
services.AddSingleton<IProcessInformationProvider, WindowsProcessInformationProvider>();

// Inject dependencies
public class MouseEventListener
{
    public MouseEventListener(
        ILogger<MouseEventListener> logger,
        IProcessInformationProvider processInfoProvider,
        EventSchema eventSchema,
        IEventTypeMapper eventTypeMapper)
    {
        _logger = logger;
        _processInfoProvider = processInfoProvider;
    }
}
```

**Benefits**:
- Testability (mock dependencies)
- Flexibility (swap implementations)
- Inversion of Control

### 7. Channel Pattern (Async Producer-Consumer)
**Problem**: Decouple event capture from processing for performance.

**Solution**: Bounded/unbounded channel for async communication.

```csharp
private readonly Channel<IEventData> _eventChannel;

// Producer (event listeners)
void OnEventCaptured(object? sender, IEventData eventData)
{
    _eventChannel.Writer.TryWrite(eventData);
}

// Consumer (processing task)
await foreach (var eventData in _eventChannel.Reader.ReadAllAsync(cancellationToken))
{
    await ProcessEventAsync(eventData);
}
```

**Benefits**:
- Decouples event capture from processing
- Back-pressure handling with bounded channels
- Async processing without blocking

## Data Flow

### Complete Data Flow Diagram

```
┌────────────────────────────────────────────────────────────────────────┐
│                   Complete Event Processing Flow                        │
└────────────────────────────────────────────────────────────────────────┘

 ┌─────────────┐
 │    USER     │
 │  (Clicks)   │
 └──────┬──────┘
        │ Mouse Click
        ▼
 ┌──────────────────────────────────────────────────────────────────┐
 │                    Windows Operating System                       │
 │  Generates: WM_LBUTTONDOWN message                               │
 │  At: (x=450, y=300)                                              │
 └───────────────────────────┬──────────────────────────────────────┘
                             │
                             ▼
 ╔═══════════════════════════════════════════════════════════════════╗
 ║              Windows Hook (Low-Level Mouse Hook)                  ║
 ║  SetWindowsHookEx(WH_MOUSE_LL, HookCallback, ...)                ║
 ╚═══════════════════════════┬═══════════════════════════════════════╝
                             │
                             ▼
 ┌────────────────────────────────────────────────────────────────────┐
 │  MouseEventListener.HookCallback(nCode, wParam, lParam)            │
 │  ├─ Extract: MSLLHOOKSTRUCT (coordinates, mouseData, time)         │
 │  ├─ Call: GetForegroundProcessId() → 1234                          │
 │  ├─ Call: GetProcessName(1234) → "notepad.exe"                     │
 │  └─ Create: MouseEventData                                         │
 │             ├─ EventType: "WM_LBUTTONDOWN"                         │
 │             ├─ ProcessId: 1234                                     │
 │             ├─ ProcessName: "notepad.exe"                          │
 │             ├─ X: 450, Y: 300                                      │
 │             └─ Timestamp: 2025-10-31T13:45:23Z                     │
 └────────────────────────────┬───────────────────────────────────────┘
                              │ Fire Event
                              ▼
 ┌────────────────────────────────────────────────────────────────────┐
 │  EventCaptured?.Invoke(this, eventData)                            │
 │  └─> Triggers all subscribers                                      │
 └────────────────────────────┬───────────────────────────────────────┘
                              │
                              ▼
 ┌────────────────────────────────────────────────────────────────────┐
 │  EventOrchestrator.OnEventCaptured(sender, eventData)              │
 │  └─> _eventChannel.Writer.TryWrite(eventData)                      │
 └────────────────────────────┬───────────────────────────────────────┘
                              │
                              ▼
 ╔═════════════════════════════════════════════════════════════════════╗
 ║           Channel<IEventData> (Async Queue)                         ║
 ║  Producer: Event Listeners                                          ║
 ║  Consumer: Background Processing Task                               ║
 ║  Mode: Unbounded (absorbs bursts)                                   ║
 ╚════════════════════════════┬════════════════════════════════════════╝
                              │ await foreach (ReadAllAsync)
                              ▼
 ┌────────────────────────────────────────────────────────────────────┐
 │  EventOrchestrator.ProcessEventsAsync()                            │
 │  └─> Reads event from channel                                      │
 └────────────────────────────┬───────────────────────────────────────┘
                              │ Process Event
                              ▼
 ╔═════════════════════════════════════════════════════════════════════╗
 ║        AttributeBasedEventProcessor.ProcessAsync(eventData)         ║
 ╠═════════════════════════════════════════════════════════════════════╣
 ║ 1. Map Event Type                                                   ║
 ║    EventTypeMapper: "WM_LBUTTONDOWN" → "mouse_click_down"          ║
 ║                                                                     ║
 ║ 2. Find Event Definition in Schema                                  ║
 ║    events.json: "mouse_click_down" → EventDefinition               ║
 ║    ├─ output_attributes: [os_event_type, foreground_process_id,    ║
 ║    │                      foreground_process_name, mouse.x, mouse.y]║
 ║                                                                     ║
 ║ 3. Create Attribute Context                                         ║
 ║    ├─ EventData: MouseEventData                                     ║
 ║    ├─ Timestamp: 2025-10-31T13:45:23Z                              ║
 ║    ├─ Metadata: { EventType, ProcessId }                           ║
 ║    └─ ValueCache: {}  (for optimization)                           ║
 ║                                                                     ║
 ║ 4. Get Attribute Values (Batch)                                     ║
 ║    AttributeProviderRegistry.GetAttributeValues(                    ║
 ║        ["os_event_type", "foreground_process_id",                   ║
 ║         "foreground_process_name", "mouse.x", "mouse.y"])          ║
 ║                                                                     ║
 ║    Group by Provider:                                               ║
 ║    ┌───────────────────────────────────────────────────────┐       ║
 ║    │ EventAttributeProvider                                │       ║
 ║    │  Input: ["os_event_type"]                             │       ║
 ║    │  Output: { "os_event_type": "WM_LBUTTONDOWN" }        │       ║
 ║    └───────────────────────────────────────────────────────┘       ║
 ║    ┌───────────────────────────────────────────────────────┐       ║
 ║    │ ForegroundProcessAttributeProvider                    │       ║
 ║    │  Input: ["foreground_process_id",                     │       ║
 ║    │          "foreground_process_name"]                   │       ║
 ║    │  Call: GetForegroundProcessId() → 1234 (cached)       │       ║
 ║    │  Call: GetProcessName(1234) → "notepad.exe"           │       ║
 ║    │  Output: { "foreground_process_id": 1234,             │       ║
 ║    │            "foreground_process_name": "notepad.exe" } │       ║
 ║    └───────────────────────────────────────────────────────┘       ║
 ║    ┌───────────────────────────────────────────────────────┐       ║
 ║    │ MouseAttributeProvider                                │       ║
 ║    │  Input: ["mouse.x", "mouse.y"]                        │       ║
 ║    │  Extract from: MouseEventData                         │       ║
 ║    │  Output: { "mouse.x": 450, "mouse.y": 300 }          │       ║
 ║    └───────────────────────────────────────────────────────┘       ║
 ║                                                                     ║
 ║ 5. Store Attributes in Event Metadata                               ║
 ║    eventData.Metadata["attr_os_event_type"] = "WM_LBUTTONDOWN"    ║
 ║    eventData.Metadata["attr_foreground_process_id"] = 1234         ║
 ║    eventData.Metadata["attr_foreground_process_name"] = "notepad.exe"║
 ║    eventData.Metadata["attr_mouse.x"] = 450                        ║
 ║    eventData.Metadata["attr_mouse.y"] = 300                        ║
 ║    eventData.Metadata["EventDefinition"] = eventDef                ║
 ╚═════════════════════════════════════════════════════════════════════╝
                              │ Enriched Event
                              ▼
 ┌─────────────────────────────────────────────────────────────────────┐
 │              Output Writers (Parallel Processing)                    │
 └─────────────────────────────────────────────────────────────────────┘
                    │                              │
        ┌───────────┴──────────┐     ┌────────────┴─────────────┐
        │                      │     │                          │
        ▼                      ▼     ▼                          ▼
 ┌──────────────────┐   ┌─────────────────┐      ┌──────────────────────┐
 │ ConsoleWriter    │   │  FileWriter     │      │ DatabaseWriter (*)   │
 │                  │   │                 │      │                      │
 │ Format: CSV      │   │ Format: JSON    │      │ Format: SQL          │
 │                  │   │                 │      │                      │
 │ Read:            │   │ Read:           │      │ Read:                │
 │  EventDefinition │   │  EventDefinition│      │  EventDefinition     │
 │                  │   │                 │      │                      │
 │ Iterate:         │   │ Build Object:   │      │ Build Parameters:    │
 │  output_attrs    │   │  {             │      │  @EventType = ...    │
 │                  │   │   "os_event    │      │  @ProcessId = ...    │
 │ Format:          │   │   _type": "...",│      │                      │
 │  by datatype     │   │   "foreground  │      │ Execute:             │
 │                  │   │   _process_id":│      │  INSERT INTO ...     │
 │ Output:          │   │   1234,        │      │                      │
 │  WM_LBUTTONDOWN, │   │   ...          │      │                      │
 │  1234,           │   │  }             │      │                      │
 │  "notepad.exe",  │   │                 │      │                      │
 │  450,            │   │ Write Line:     │      │                      │
 │  300             │   │  JSON text      │      │                      │
 │                  │   │                 │      │                      │
 │ Write to:        │   │ Buffer:         │      │ Commit:              │
 │  Console.Out     │   │  _eventCount++  │      │  Transaction         │
 │  (immediate)     │   │                 │      │                      │
 │                  │   │ Flush:          │      │                      │
 │                  │   │  if (_eventCount│      │                      │
 │                  │   │   >= FlushInt)  │      │                      │
 └──────┬───────────┘   └────────┬────────┘      └──────────┬───────────┘
        │                        │                          │
        ▼                        ▼                          ▼
  ┌──────────┐          ┌────────────────┐        ┌──────────────┐
  │  stdout  │          │ ./data/        │        │  Database    │
  │          │          │ events.log     │        │  Table       │
  └──────────┘          └────────────────┘        └──────────────┘

(*) Future enhancement
```

### End-to-End Event Processing (Sequential View)

```
1. USER CLICKS MOUSE
   └─> Windows generates WM_LBUTTONDOWN message

2. WINDOWS HOOK CAPTURES EVENT
   MouseEventListener.HookCallback()
   ├─> Extracts coordinates from MSLLHOOKSTRUCT
   ├─> Calls GetForegroundProcessId()
   ├─> Calls GetProcessName()
   └─> Creates MouseEventData

3. EVENT LISTENER FIRES EVENT
   EventCaptured?.Invoke(this, eventData)
   └─> EventOrchestrator.OnEventCaptured()

4. EVENT QUEUED TO CHANNEL
   _eventChannel.Writer.TryWrite(eventData)
   └─> Async channel buffer

5. BACKGROUND TASK PROCESSES EVENT
   EventOrchestrator.ProcessEventsAsync()
   └─> Reads from channel

6. EVENT PROCESSOR ENRICHES DATA
   AttributeBasedEventProcessor.ProcessAsync()
   ├─> Maps "WM_LBUTTONDOWN" → "mouse_click_down"
   ├─> Finds event definition in schema
   ├─> Gets attribute list from definition
   ├─> Creates AttributeContext with event data
   ├─> Calls AttributeProviderRegistry.GetAttributeValues()
   │   ├─> Groups by provider
   │   ├─> EventAttributeProvider.GetAttributeValues()
   │   │   └─> Returns { "os_event_type": "WM_LBUTTONDOWN" }
   │   ├─> ForegroundProcessAttributeProvider.GetAttributeValues()
   │   │   └─> Returns { "foreground_process_id": 1234, "foreground_process_name": "notepad.exe" }
   │   └─> MouseAttributeProvider.GetAttributeValues()
   │       └─> Returns { "mouse.x": 450, "mouse.y": 300 }
   └─> Stores as attr_* in eventData.Metadata

7. OUTPUT WRITERS FORMAT AND WRITE
   ConsoleOutputWriter.WriteAsync()
   ├─> Reads EventDefinition from metadata
   ├─> Iterates output_attributes in order
   ├─> Formats each value by datatype
   ├─> CSV: "WM_LBUTTONDOWN, 1234, notepad.exe, 450, 300"
   └─> Writes to Console.Out

   FileOutputWriter.WriteAsync()
   ├─> Formats as JSON
   ├─> JSON: {"os_event_type":"WM_LBUTTONDOWN","foreground_process_id":1234,...}
   ├─> Writes to file
   └─> Flushes buffer every N events
```

### Attribute Context Flow

```csharp
// Context creation in AttributeBasedEventProcessor
var context = new AttributeContext
{
    EventData = eventData,              // MouseEventData with x, y, processId
    Timestamp = eventData.Timestamp,    // Event time
    Metadata = new Dictionary<string, object>
    {
        ["EventType"] = "WM_LBUTTONDOWN",
        ["ProcessId"] = 1234
    },
    ValueCache = new Dictionary<string, object?>()  // For provider optimization
};

// Context usage in ForegroundProcessAttributeProvider
public override Dictionary<string, object?> GetAttributeValues(...)
{
    // Get process ID once, cache it
    var processId = GetOrComputeValue(
        context,
        "foreground_process_id",
        () => _processInfoProvider.GetForegroundProcessId());
    
    // Reuse cached process ID for process name lookup
    var processName = GetOrComputeValue(
        context,
        "foreground_process_name",
        () => _processInfoProvider.GetProcessName(processId));
    
    return new Dictionary<string, object?>
    {
        ["foreground_process_id"] = processId,
        ["foreground_process_name"] = processName
    };
}
```

## Extensibility

### Adding a New Event Type

**1. Define in schema** ([`events.json`](../src/ContextSensor.App/events.json)):
```json
{
  "name": "key_press",
  "description": "Keyboard key press event",
  "output_attributes": [
    { "name": "os_event_type", "datatype": "string" },
    { "name": "key_code", "datatype": "integer" },
    { "name": "key_char", "datatype": "string" },
    { "name": "foreground_process_id", "datatype": "integer" }
  ]
}
```

**2. Create event data class**:
```csharp
public class KeyboardEventData : IEventData
{
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public int KeyCode { get; set; }
    public char KeyChar { get; set; }
}
```

**3. Implement event listener**:
```csharp
public class KeyboardEventListener : IEventListener
{
    public event EventHandler<IEventData>? EventCaptured;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Set keyboard hook
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, ...);
    }
    
    private void CaptureKeyEvent(int keyCode)
    {
        var eventData = new KeyboardEventData
        {
            EventType = "WM_KEYDOWN",
            KeyCode = keyCode,
            // ...
        };
        EventCaptured?.Invoke(this, eventData);
    }
}
```

**4. Register in configuration** ([`appsettings.json`](../src/ContextSensor.App/appsettings.json)):
```json
{
  "EventListeners": [
    { "Type": "KeyboardEventListener", "Enabled": true }
  ]
}
```

**5. Add factory case** ([`Program.cs`](../src/ContextSensor.App/Program.cs)):
```csharp
IEventListener? listener = listenerConfig.Type switch
{
    "MouseEventListener" => new MouseEventListener(...),
    "KeyboardEventListener" => new KeyboardEventListener(...),  // Add this
    _ => null
};
```

### Adding a New Attribute

**1. Register in AttributeRegistry** ([`AttributeDefinition.cs`](../src/ContextSensor.Core/Attributes/AttributeDefinition.cs)):
```csharp
private static void RegisterKeyboardComponent()
{
    var component = new ComponentDefinition
    {
        Id = "keyboard",
        Name = "Keyboard",
        Attributes = new List<AttributeDefinition>
        {
            new AttributeDefinition
            {
                Id = "key_code",
                Name = "Key Code",
                DataType = AttributeDataType.Integer,
                ComponentId = "keyboard"
            }
        }
    };
    // Register component and attributes...
}
```

**2. Create attribute provider**:
```csharp
public class KeyboardAttributeProvider : AttributeProviderBase
{
    public KeyboardAttributeProvider()
        : base(new[] { "keyboard.key_code", "keyboard.key_char" })
    { }
    
    public override object? GetAttributeValue(string attributeQualifiedName, AttributeContext? context)
    {
        var keyData = context?.EventData as KeyboardEventData;
        return attributeQualifiedName switch
        {
            "keyboard.key_code" => keyData?.KeyCode,
            "keyboard.key_char" => keyData?.KeyChar,
            _ => null
        };
    }
}
```

**3. Register provider** ([`Program.cs`](../src/ContextSensor.App/Program.cs)):
```csharp
registry.RegisterProvider(new KeyboardAttributeProvider());
```

**4. Use in event schema**:
```json
{
  "name": "key_press",
  "output_attributes": [
    { "name": "keyboard.key_code", "datatype": "integer" },
    { "name": "keyboard.key_char", "datatype": "string" }
  ]
}
```

### Adding a New Output Format

**1. Extend OutputWriterBase**:
```csharp
public class XmlOutputWriter : OutputWriterBase
{
    public XmlOutputWriter(string filePath)
        : base("xml")
    { }
    
    protected override string FormatAsXml(IEventData eventData)
    {
        var eventDef = eventData.Metadata["EventDefinition"] as EventDefinition;
        var xml = new StringBuilder("<Event>");
        
        foreach (var attr in eventDef.OutputAttributes)
        {
            var value = eventData.Metadata[$"attr_{attr.Name}"];
            xml.Append($"<{attr.Name}>{value}</{attr.Name}>");
        }
        
        xml.Append("</Event>");
        return xml.ToString();
    }
}
```

**2. Register in configuration**:
```json
{
  "OutputWriters": [
    { "Type": "XmlOutputWriter", "Enabled": true, "Destination": "./data/events.xml" }
  ]
}
```

**3. Add factory case**:
```csharp
IOutputWriter? writer = writerConfig.Type switch
{
    "ConsoleOutputWriter" => new ConsoleOutputWriter(...),
    "FileOutputWriter" => new FileOutputWriter(...),
    "XmlOutputWriter" => new XmlOutputWriter(...),  // Add this
    _ => null
};
```

## Design Strengths

### 1. Scalability

**Async Processing**: Channel-based pipeline scales with event volume
```csharp
// Unbounded channel absorbs bursts
_eventChannel = Channel.CreateUnbounded<IEventData>();

// Background task processes at sustainable rate
await foreach (var eventData in _eventChannel.Reader.ReadAllAsync())
```

**Batch Attribute Retrieval**: Minimizes provider calls
```csharp
// Groups attributes by provider, calls each once
var providerGroups = attributeNames
    .GroupBy(attr => GetProviderForAttribute(attr));

foreach (var group in providerGroups)
{
    var values = group.Key.GetAttributeValues(group.Select(x => x.Attribute));
}
```

**Buffered File Writes**: Reduces I/O overhead
```csharp
// Configurable flush interval (default: 100 events)
if (_eventCount >= _flushInterval)
{
    await _writer.FlushAsync();
}
```

### 2. Maintainability

**Clear Separation of Concerns**: Each layer has distinct responsibility
- App layer: Composition & lifecycle
- Core layer: Business logic & abstractions
- Windows layer: Platform-specific implementations

**Interface-Based Design**: Easy to understand contracts
- 8 focused interfaces vs. monolithic classes
- Clear input/output expectations
- Minimal coupling between components

**Comprehensive Logging**: Structured logging throughout
```csharp
_logger.LogInformation("Registered provider {ProviderName} supporting {AttributeCount} attributes",
    provider.ProviderName, provider.SupportedAttributes.Count());
```

### 3. Testability

**Dependency Injection**: All dependencies are injectable
```csharp
// Easy to mock for testing
public class MouseEventListener
{
    public MouseEventListener(
        ILogger<MouseEventListener> logger,           // Can inject test logger
        IProcessInformationProvider processInfoProvider,  // Can inject mock
        EventSchema eventSchema,                      // Can inject test schema
        IEventTypeMapper eventTypeMapper)             // Can inject test mapper
}
```

**Interface Abstractions**: Can substitute test implementations
```csharp
// Production
services.AddSingleton<IProcessInformationProvider, WindowsProcessInformationProvider>();

// Testing
services.AddSingleton<IProcessInformationProvider, MockProcessInformationProvider>();
```

**Stateless Providers**: Easy to test in isolation
```csharp
[Fact]
public void MouseAttributeProvider_ReturnsCoordinates()
{
    var provider = new MouseAttributeProvider(logger);
    var context = new AttributeContext
    {
        EventData = new MouseEventData { X = 100, Y = 200 }
    };
    
    var value = provider.GetAttributeValue("mouse.x", context);
    Assert.Equal(100, value);
}
```

### 4. Backward Compatibility

**Configuration Versioning**: Can evolve schemas without breaking existing configs
```json
{
  "schema_version": "1.0",  // Future: Support multiple versions
  "events": [...]
}
```

**Optional Attributes**: Missing attributes don't break output
```csharp
// Safely handles missing attributes
if (eventData.Metadata.TryGetValue(attrKey, out var value))
{
    parts.Add(FormatValue(value, attr.Datatype));
}
else
{
    parts.Add(string.Empty);  // Graceful degradation
}
```

**Additive Changes**: New attributes, events, outputs don't affect existing functionality

### 5. Error Resilience

**Isolated Error Handling**: Exceptions don't cascade
```csharp
try
{
    var processedEvent = await _eventProcessor.ProcessAsync(eventData);
    foreach (var writer in _outputWriters)
    {
        await writer.WriteAsync(processedEvent);
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing event: {EventType}", eventData.EventType);
    // Continue processing next event
}
```

**Graceful Degradation**: Partial failures still produce output
```csharp
// If an attribute provider fails, others still work
foreach (var group in providerGroups)
{
    try
    {
        var values = provider.GetAttributeValues(attributes, context);
        // Add successful values
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting batch values from provider");
        // Continue with other providers
    }
}
```

**Resource Cleanup**: Proper disposal even on errors
```csharp
public void Dispose()
{
    try
    {
        StopAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during disposal");
    }
    finally
    {
        _cancellationTokenSource.Dispose();
        // Cleanup all resources
    }
}
```

### 6. Thread Safety

**Concurrent Collections**: Safe multi-threaded access
```csharp
private readonly ConcurrentDictionary<string, IAttributeProvider> _providers;
private readonly ConcurrentDictionary<string, List<string>> _attributeToProviders;
```

**Interlocked Operations**: Atomic state changes
```csharp
if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
{
    _logger.LogWarning("Already running");
    return;
}
```

**Semaphore Synchronization**: Controlled concurrent access
```csharp
await _semaphore.WaitAsync();
try
{
    await _writer.WriteLineAsync(output);
}
finally
{
    _semaphore.Release();
}
```

### 7. Performance Optimizations

**Value Caching**: Avoid redundant computations
```csharp
protected T GetOrComputeValue<T>(
    AttributeContext? context,
    string cacheKey,
    Func<T> computeFunc)
{
    if (context?.ValueCache.TryGetValue(cacheKey, out var cachedValue) == true)
    {
        return (T)cachedValue!;
    }
    
    var value = computeFunc();
    if (context != null)
    {
        context.ValueCache[cacheKey] = value;
    }
    return value;
}
```

**Batch Operations**: Group related work
```csharp
// Get all attributes from a provider in one call
public override Dictionary<string, object?> GetAttributeValues(
    IEnumerable<string> attributeQualifiedNames,
    AttributeContext? context)
{
    // Single batch call vs. multiple individual calls
}
```

**Lazy Initialization**: Create only when needed
```csharp
public static void Initialize()
{
    if (_initialized) return;
    
    lock (_initLock)
    {
        if (_initialized) return;  // Double-check
        // Initialize once
        _initialized = true;
    }
}
```

---

## Conclusion

ContextSensor demonstrates enterprise-grade software architecture with:

✅ **Clean Architecture** - Clear separation of concerns across three layers  
✅ **SOLID Principles** - Exemplifies all five principles in practice  
✅ **Design Patterns** - 7+ patterns applied appropriately  
✅ **Extensibility** - Add events, attributes, outputs without core changes  
✅ **Testability** - Interface-based design with dependency injection  
✅ **Production-Ready** - Thread-safe, async, error-resilient  
✅ **Configuration-Driven** - Declarative event and attribute definitions  
✅ **Platform Abstraction** - OS-agnostic core with Windows implementation  

The architecture is ready for:
- Additional event types (keyboard, clipboard, network, USB)
- New output formats (database, REST API, message queue)
- Cross-platform support (Linux, macOS)
- Advanced features (data transformations, encryption, filtering)

**Next Steps**: See [`CONFIGURATION.md`](CONFIGURATION.md) for configuration details and [`SETUP_WINDOWS.md`](SETUP_WINDOWS.md) for installation instructions.
