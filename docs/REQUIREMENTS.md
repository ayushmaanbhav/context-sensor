Mini ContextSensor Project Requirements
Build a sensor for the Windows platform that collects information based on mouse clicks.
The requirements are to:
1. Work on Windows 10 and Windows 11
2. Use C# as the primary programming language.
    a. If you want or need a C++ DLL that is fine, but it is not required to solve this problem.
3. Upon every left mouse click:
    a. Log the type of action (left mouse click)
        i. Values: use the same names as Microsoft documentation
            - WM_LBUTTONDOWN message (Winuser.h) - Win32 apps | Microsoft Learn
    b. Log the process ID (PID) that the action is intended to go to, typically the foreground application:
        i. Data type: integer
    c. Log the process name, basically convert the PID into an EXE name:
        i. Data type: string
4. Send the logs to the console, stdout
    a. Example:
        WM_LBUTTONDOWN , 2160, notepad.exe



Solution:
Design and other non functional Inputs:
- Need to build a mature app with config driven, scalable, upgradable, extensible, backward compatible design.
- The events to listen and the data to record from each event should be configurable like if we want to record key strokes in future, add screen position data, add event when a device connects/disconnects through usb or bluetooth, etc.
- Design so that events can be subscribed to when needed/configured in a pluggable way.
- Also need a layer so that the underlying OS calls and interactions are abstracted and the above layers are OS aganostic.
- The above config should be designed as a json containing event definitions according to above requirements with wrappers whereever required so that there can be overrides in specific cases like for a particular OS version and should have support for configurable data transformations as well like masking a password or encryption. 
- Need a good project and file structure which can accomodate updates in config and upgrades.
- The output format and destination should also be configurable and abstracted out. It should support file output as well.
- Use design patterns and interfaces where applicable, follow OOPS, SOLID principles when writing code and avoid common anti patterns and implement in a backward compatible way.
- Add graceful error handling at sepcific code executions what can throw errors like subscribing to events or writing to file. Add info and error logging to a separate file.
- Add support to as user for permissions if required to subscribe to an event. We will implement this separately and ask permissions/elevation at time of installation and create a msi package. Dont need to do it now but can add support if permissions are needed for win 10/11 or other OS.
- The stateful data like config data and output data should be stored in a separate location specified in config file. It would help if the app is running and updating then the stateful data should not be overwritten/replaced.
