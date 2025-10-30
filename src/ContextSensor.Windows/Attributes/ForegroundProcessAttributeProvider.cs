using ContextSensor.Core.Attributes;
using ContextSensor.Core.OS;
using Microsoft.Extensions.Logging;

namespace ContextSensor.Windows.Attributes;

/// <summary>
/// Provider for foreground process information attributes
/// Uses the process component to retrieve information about the currently active (foreground) process
/// </summary>
public class ForegroundProcessAttributeProvider : AttributeProviderBase
{
    private readonly IProcessInformationProvider _processInfoProvider;
    private readonly ILogger<ForegroundProcessAttributeProvider> _logger;

    public ForegroundProcessAttributeProvider(
        IProcessInformationProvider processInfoProvider,
        ILogger<ForegroundProcessAttributeProvider> logger)
        : base(new[]
        {
            "foreground_process_id",
            "foreground_process_name"        })
    {
        _processInfoProvider = processInfoProvider ?? throw new ArgumentNullException(nameof(processInfoProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override string ProviderName => "ForegroundProcessInfoProvider";

    public override object? GetAttributeValue(string attributeQualifiedName, AttributeContext? context = null)
    {
        return SafeGetValue<object>(
            attributeQualifiedName,
            () => attributeQualifiedName switch
            {
                "foreground_process_id" => GetForegroundProcessId(context),
                "foreground_process_name" => GetForegroundProcessName(context),
                _ => null
            },
            _logger);
    }

    public override Dictionary<string, object?> GetAttributeValues(
        IEnumerable<string> attributeQualifiedNames,
        AttributeContext? context = null)
    {
        var result = new Dictionary<string, object?>();
        var requestedAttributes = attributeQualifiedNames.ToHashSet();

        // Optimize: Get process ID once if any attribute is requested
        if (requestedAttributes.Any())
        {
            return SafeGetValue(
                "batch_attributes",
                () =>
                {
                    // Get the process ID first (cached in context if available)
                    var processId = GetForegroundProcessId(context);
                    
                    foreach (var attrName in requestedAttributes)
                    {
                        if (!CanProvideAttribute(attrName))
                            continue;

                        switch (attrName)
                        {
                            case "foreground_process_id":
                                result[attrName] = processId;
                                break;
                            
                            case "foreground_process_name":
                                if (processId > 0)
                                {
                                    result[attrName] = GetOrComputeValue(
                                        context,
                                        "foreground_process_name",
                                        () => _processInfoProvider.GetProcessName(processId));
                                }
                                else
                                {
                                    result[attrName] = "unknown.exe";
                                }
                                break;
                        }
                    }
                    
                    return result;
                },
                _logger,
                result) ?? result;
        }

        return result;
    }

    private int GetForegroundProcessId(AttributeContext? context)
    {
        return GetOrComputeValue(
            context,
            "foreground_process_id",
            () =>
            {
                var processId = _processInfoProvider.GetForegroundProcessId();
                _logger.LogDebug("Foreground process ID: {ProcessId}", processId);
                return processId;
            });
    }

    private string GetForegroundProcessName(AttributeContext? context)
    {
        return GetOrComputeValue(
            context,
            "foreground_process_name",
            () =>
            {
                var processId = GetForegroundProcessId(context);
                if (processId <= 0)
                {
                    _logger.LogWarning("Invalid foreground process ID: {ProcessId}", processId);
                    return string.Empty;
                }

                var processName = _processInfoProvider.GetProcessName(processId);
                _logger.LogDebug("Foreground process name: {ProcessName}", processName);
                return processName;
            });
    }
}
