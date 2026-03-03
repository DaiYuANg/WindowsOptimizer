using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using WindowsControlPanel.Context;

namespace WindowsControlPanel.Service;

public enum OptimizationMode
{
    Development,
    Gaming
}

public enum OptionalFeatureState
{
    Unknown,
    Enabled,
    Disabled
}

public enum MaintenanceAction
{
    OpenOptionalFeatures,
    OpenDnsSettings,
    OpenDeliveryOptimization,
    OpenStorageSense,
    GenerateEnergyReport,
    GenerateBatteryReport,
    QueryStartupPrograms
}

public enum AdvancedAction
{
    EnableDeveloperMode,
    DisableDeveloperMode,
    EnableSudo,
    DisableSudo,
    ExportWingetPackages,
    OpenGraphicsSettings,
    OpenGameModeSettings,
    OpenGameBarSettings
}

public sealed class OperationResult
{
    public bool Success { get; init; }
    public bool RequiresElevation { get; init; }
    public bool RequiresRestart { get; init; }
    public int ExitCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Output { get; init; } = string.Empty;
    public string ErrorOutput { get; init; } = string.Empty;

    public static OperationResult Succeeded(
        string message,
        string output = "",
        bool requiresRestart = false,
        int exitCode = 0
    )
    {
        return new OperationResult
        {
            Success = true,
            Message = message,
            Output = output,
            RequiresRestart = requiresRestart,
            ExitCode = exitCode
        };
    }

    public static OperationResult Failed(
        string message,
        string output = "",
        string errorOutput = "",
        int exitCode = -1,
        bool requiresElevation = false
    )
    {
        return new OperationResult
        {
            Success = false,
            Message = message,
            Output = output,
            ErrorOutput = errorOutput,
            ExitCode = exitCode,
            RequiresElevation = requiresElevation
        };
    }
}

public sealed class SystemStatusSnapshot
{
    public string OSVersion { get; init; } = "Unknown OS";
    public string MachineName { get; init; } = "Unknown machine";
    public string CPUInfo { get; init; } = "Unknown CPU";
    public string TotalMemory { get; init; } = "Unknown";
    public string FreeMemory { get; init; } = "Unknown";
    public bool IsAdministrator { get; init; }
    public bool IsVbsEnabled { get; init; }
    public bool IsHvciEnabled { get; init; }
    public bool IsSecureBootEnabled { get; init; }
    public bool? IsVirtualizationFirmwareEnabled { get; init; }
    public string HypervisorLaunchType { get; init; } = "Unknown";
    public OptionalFeatureState HyperVState { get; init; }
    public OptionalFeatureState WslState { get; init; }
    public OptionalFeatureState VmPlatformState { get; init; }
    public OptionalFeatureState SandboxState { get; init; }
}

public sealed class AuditItem
{
    public DateTime Timestamp { get; init; }
    public string Message { get; init; } = string.Empty;
}

public interface ISystemControlService
{
    bool IsRunningAsAdministrator();
    bool TryRestartAsAdministrator();
    Task<SystemStatusSnapshot> GetStatusSnapshotAsync();
    Task<OperationResult> ApplyModeAsync(
        OptimizationMode mode,
        bool autoReboot,
        Action<string>? onOutput = null
    );
    Task<OperationResult> SetOptionalFeatureAsync(
        string featureName,
        bool enable,
        Action<string>? onOutput = null
    );
    Task<OperationResult> RunMaintenanceActionAsync(
        MaintenanceAction action,
        Action<string>? onOutput = null
    );
    Task<OperationResult> RunAdvancedActionAsync(
        AdvancedAction action,
        Action<string>? onOutput = null
    );
    Task<IReadOnlyList<AuditItem>> GetRecentAuditsAsync(int take = 20);
}

public sealed class SystemControlService : ISystemControlService
{
    private const int DefaultTimeoutMs = 5 * 60 * 1000;
    private readonly ISystemInfoService _systemInfoService;
    private readonly AppDbContext _dbContext;
    private readonly SemaphoreSlim _dbLock = new(1, 1);

    public SystemControlService(ISystemInfoService systemInfoService, AppDbContext dbContext)
    {
        _systemInfoService = systemInfoService;
        _dbContext = dbContext;
    }

    public bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public bool TryRestartAsAdministrator()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return false;
            }

            var arguments = Environment
                .GetCommandLineArgs()
                .Skip(1)
                .Select(QuoteArgument)
                .ToArray();

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = string.Join(" ", arguments),
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<SystemStatusSnapshot> GetStatusSnapshotAsync()
    {
        var hyperVStateTask = QueryOptionalFeatureStateAsync("Microsoft-Hyper-V-All");
        var wslStateTask = QueryOptionalFeatureStateAsync("Microsoft-Windows-Subsystem-Linux");
        var vmPlatformStateTask = QueryOptionalFeatureStateAsync("VirtualMachinePlatform");
        var sandboxStateTask = QueryOptionalFeatureStateAsync("Containers-DisposableClientVM");
        var hypervisorLaunchTypeTask = QueryHypervisorLaunchTypeAsync();

        await Task.WhenAll(
            hyperVStateTask,
            wslStateTask,
            vmPlatformStateTask,
            sandboxStateTask,
            hypervisorLaunchTypeTask
        );

        return new SystemStatusSnapshot
        {
            OSVersion = _systemInfoService.OSVersion,
            MachineName = _systemInfoService.MachineName,
            CPUInfo = _systemInfoService.CPUInfo,
            TotalMemory = _systemInfoService.TotalMemory,
            FreeMemory = _systemInfoService.FreeMemory,
            IsAdministrator = IsRunningAsAdministrator(),
            IsVbsEnabled = _systemInfoService.IsVbsEnabled(),
            IsHvciEnabled = IsHvciEnabled(),
            IsSecureBootEnabled = IsSecureBootEnabled(),
            IsVirtualizationFirmwareEnabled = GetVirtualizationFirmwareEnabled(),
            HypervisorLaunchType = await hypervisorLaunchTypeTask,
            HyperVState = await hyperVStateTask,
            WslState = await wslStateTask,
            VmPlatformState = await vmPlatformStateTask,
            SandboxState = await sandboxStateTask
        };
    }

    public async Task<OperationResult> ApplyModeAsync(
        OptimizationMode mode,
        bool autoReboot,
        Action<string>? onOutput = null
    )
    {
        if (!IsRunningAsAdministrator())
        {
            return OperationResult.Failed("当前操作需要管理员权限。", requiresElevation: true);
        }

        var scriptName = mode == OptimizationMode.Development ? "enable-dev.ps1" : "enable-game.ps1";
        var scriptPath = ResolveScriptPath(scriptName);
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            return OperationResult.Failed($"未找到脚本文件: {scriptName}");
        }

        onOutput?.Invoke($"开始执行模式切换: {mode}");
        var rebootArgument = autoReboot ? "-AutoReboot" : string.Empty;
        var arguments =
            $"-NoLogo -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {rebootArgument}".Trim();
        var result = await ExecuteProcessAsync(
            "powershell.exe",
            arguments,
            requireAdministrator: true,
            timeoutMs: 20 * 60 * 1000,
            onOutput: onOutput
        );

        var auditMessage =
            $"Mode={mode}; Success={result.Success}; ExitCode={result.ExitCode}; AutoReboot={autoReboot}";
        await AppendAuditAsync($"[MODE] {auditMessage}");
        await SaveSettingAsync("last_mode", mode.ToString());
        await SaveSettingAsync("last_mode_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        if (!result.Success)
        {
            return result;
        }

        return OperationResult.Succeeded(
            autoReboot ? "模式已应用，系统将自动重启。" : "模式已应用，请手动重启系统完成生效。",
            result.Output,
            requiresRestart: !autoReboot,
            exitCode: result.ExitCode
        );
    }

    public async Task<OperationResult> SetOptionalFeatureAsync(
        string featureName,
        bool enable,
        Action<string>? onOutput = null
    )
    {
        if (!IsRunningAsAdministrator())
        {
            return OperationResult.Failed("切换可选功能需要管理员权限。", requiresElevation: true);
        }

        var verb = enable ? "/enable-feature" : "/disable-feature";
        var additional = enable ? " /all" : string.Empty;
        var arguments = $"/online {verb} /featurename:{featureName} /norestart{additional}";

        onOutput?.Invoke($"{(enable ? "启用" : "禁用")}功能: {featureName}");
        var result = await ExecuteProcessAsync(
            "dism",
            arguments,
            requireAdministrator: true,
            timeoutMs: 15 * 60 * 1000,
            onOutput: onOutput
        );

        await AppendAuditAsync(
            $"[FEATURE] Feature={featureName}; Enable={enable}; Success={result.Success}; ExitCode={result.ExitCode}"
        );

        return result.Success
            ? OperationResult.Succeeded(
                $"功能操作完成: {featureName}",
                result.Output,
                requiresRestart: true,
                exitCode: result.ExitCode
            )
            : result;
    }

    public async Task<OperationResult> RunMaintenanceActionAsync(
        MaintenanceAction action,
        Action<string>? onOutput = null
    )
    {
        OperationResult result;
        switch (action)
        {
            case MaintenanceAction.OpenOptionalFeatures:
                result = OpenShellTarget("optionalfeatures.exe");
                break;
            case MaintenanceAction.OpenDnsSettings:
                result = OpenShellTarget("ms-settings:network-advancedsettings");
                break;
            case MaintenanceAction.OpenDeliveryOptimization:
                result = OpenShellTarget("ms-settings:delivery-optimization-advanced");
                break;
            case MaintenanceAction.OpenStorageSense:
                result = OpenShellTarget("ms-settings:storagesense");
                break;
            case MaintenanceAction.GenerateEnergyReport:
                result = await GeneratePowerReportAsync(
                    "energy",
                    "energy-report",
                    "/energy /duration 10",
                    onOutput
                );
                break;
            case MaintenanceAction.GenerateBatteryReport:
                result = await GeneratePowerReportAsync("battery", "battery-report", "/batteryreport", onOutput);
                break;
            case MaintenanceAction.QueryStartupPrograms:
                result = await ExecuteProcessAsync(
                    "powershell.exe",
                    "-NoLogo -NoProfile -ExecutionPolicy Bypass -Command \"Get-CimInstance Win32_StartupCommand | Select-Object Name,Location,Command | Format-Table -AutoSize\"",
                    timeoutMs: DefaultTimeoutMs,
                    onOutput: onOutput
                );
                break;
            default:
                result = OperationResult.Failed("未知维护动作。");
                break;
        }

        await AppendAuditAsync($"[MAINTENANCE] Action={action}; Success={result.Success}; ExitCode={result.ExitCode}");
        return result;
    }

    public async Task<OperationResult> RunAdvancedActionAsync(
        AdvancedAction action,
        Action<string>? onOutput = null
    )
    {
        OperationResult result;
        switch (action)
        {
            case AdvancedAction.EnableDeveloperMode:
                result = await ExecuteProcessAsync(
                    "reg",
                    "add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock /v AllowDevelopmentWithoutDevLicense /t REG_DWORD /d 1 /f",
                    requireAdministrator: true,
                    timeoutMs: DefaultTimeoutMs,
                    onOutput: onOutput
                );
                break;
            case AdvancedAction.DisableDeveloperMode:
                result = await ExecuteProcessAsync(
                    "reg",
                    "add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock /v AllowDevelopmentWithoutDevLicense /t REG_DWORD /d 0 /f",
                    requireAdministrator: true,
                    timeoutMs: DefaultTimeoutMs,
                    onOutput: onOutput
                );
                break;
            case AdvancedAction.EnableSudo:
                result = await ExecuteProcessAsync(
                    "sudo",
                    "config --enable normal",
                    requireAdministrator: true,
                    timeoutMs: DefaultTimeoutMs,
                    onOutput: onOutput
                );
                break;
            case AdvancedAction.DisableSudo:
                result = await ExecuteProcessAsync(
                    "sudo",
                    "config --disable",
                    requireAdministrator: true,
                    timeoutMs: DefaultTimeoutMs,
                    onOutput: onOutput
                );
                break;
            case AdvancedAction.ExportWingetPackages:
                result = await ExportWingetPackagesAsync(onOutput);
                break;
            case AdvancedAction.OpenGraphicsSettings:
                result = OpenShellTarget("ms-settings:display-advancedgraphics");
                break;
            case AdvancedAction.OpenGameModeSettings:
                result = OpenShellTarget("ms-settings:gaming-gamemode");
                break;
            case AdvancedAction.OpenGameBarSettings:
                result = OpenShellTarget("ms-settings:gaming-gamebar");
                break;
            default:
                result = OperationResult.Failed("未知高级动作。");
                break;
        }

        await AppendAuditAsync($"[ADVANCED] Action={action}; Success={result.Success}; ExitCode={result.ExitCode}");
        return result;
    }

    public async Task<IReadOnlyList<AuditItem>> GetRecentAuditsAsync(int take = 20)
    {
        var boundedTake = Math.Clamp(take, 1, 100);
        await _dbLock.WaitAsync();
        try
        {
            return await _dbContext
                .SystemLogs.AsNoTracking()
                .OrderByDescending(x => x.Timestamp)
                .Take(boundedTake)
                .Select(x => new AuditItem { Timestamp = x.Timestamp, Message = x.Message })
                .ToListAsync();
        }
        catch
        {
            return Array.Empty<AuditItem>();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task<OperationResult> ExportWingetPackagesAsync(Action<string>? onOutput)
    {
        var outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "WindowsControlPanel",
            "winget"
        );
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"winget-packages-{DateTime.Now:yyyyMMdd-HHmmss}.json");

        var result = await ExecuteProcessAsync(
            "winget",
            $"export -o \"{outputPath}\" --accept-source-agreements",
            timeoutMs: 10 * 60 * 1000,
            onOutput: onOutput
        );

        if (!result.Success)
        {
            return result;
        }

        return OperationResult.Succeeded($"Winget 导出完成: {outputPath}", result.Output, exitCode: result.ExitCode);
    }

    private async Task<OperationResult> GeneratePowerReportAsync(
        string reportType,
        string filePrefix,
        string commandArguments,
        Action<string>? onOutput
    )
    {
        var reportDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "WindowsControlPanel",
            "reports"
        );
        Directory.CreateDirectory(reportDirectory);
        var reportPath = Path.Combine(reportDirectory, $"{filePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.html");

        var result = await ExecuteProcessAsync(
            "powercfg",
            $"{commandArguments} /output \"{reportPath}\"",
            timeoutMs: DefaultTimeoutMs,
            onOutput: onOutput
        );

        if (!result.Success)
        {
            return result;
        }

        return OperationResult.Succeeded(
            $"已生成 {reportType} 报告: {reportPath}",
            result.Output,
            exitCode: result.ExitCode
        );
    }

    private OperationResult OpenShellTarget(string target)
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                }
            );
            return OperationResult.Succeeded($"已打开: {target}");
        }
        catch (Exception ex)
        {
            return OperationResult.Failed($"打开失败: {target}", errorOutput: ex.Message);
        }
    }

    private async Task<OptionalFeatureState> QueryOptionalFeatureStateAsync(string featureName)
    {
        var result = await ExecuteProcessAsync(
            "dism",
            $"/online /Get-FeatureInfo /FeatureName:{featureName}",
            timeoutMs: DefaultTimeoutMs
        );
        if (!result.Success)
        {
            return OptionalFeatureState.Unknown;
        }

        var content = $"{result.Output}\n{result.ErrorOutput}";
        if (ContainsAny(content, "State : Enabled", "状态 : 已启用", "状态: 已启用"))
        {
            return OptionalFeatureState.Enabled;
        }

        if (ContainsAny(content, "State : Disabled", "状态 : 已禁用", "状态: 已禁用"))
        {
            return OptionalFeatureState.Disabled;
        }

        return OptionalFeatureState.Unknown;
    }

    private async Task<string> QueryHypervisorLaunchTypeAsync()
    {
        var result = await ExecuteProcessAsync("bcdedit", "/enum {current}", timeoutMs: DefaultTimeoutMs);
        if (!result.Success)
        {
            return "Unknown";
        }

        foreach (var rawLine in result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (!line.Contains("hypervisorlaunchtype", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains("off", StringComparison.OrdinalIgnoreCase))
            {
                return "Off";
            }

            if (line.Contains("auto", StringComparison.OrdinalIgnoreCase))
            {
                return "Auto";
            }
        }

        return "Unknown";
    }

    private bool IsHvciEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"
            );
            var value = key?.GetValue("Enabled");
            return value is int intValue && intValue == 1;
        }
        catch
        {
            return false;
        }
    }

    private bool IsSecureBootEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            var value = key?.GetValue("UEFISecureBootEnabled");
            return value is int intValue && intValue == 1;
        }
        catch
        {
            return false;
        }
    }

    private bool? GetVirtualizationFirmwareEnabled()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "select VirtualizationFirmwareEnabled from Win32_Processor"
            );
            foreach (var item in searcher.Get())
            {
                if (item["VirtualizationFirmwareEnabled"] is bool enabled)
                {
                    return enabled;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task AppendAuditAsync(string message)
    {
        await _dbLock.WaitAsync();
        try
        {
            _dbContext.SystemLogs.Add(
                new SystemLog
                {
                    Message = message,
                    Timestamp = DateTime.Now
                }
            );
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task SaveSettingAsync(string key, string value)
    {
        await _dbLock.WaitAsync();
        try
        {
            var existing = await _dbContext.UserSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (existing is null)
            {
                _dbContext.UserSettings.Add(new UserSetting { Key = key, Value = value });
            }
            else
            {
                existing.Value = value;
            }

            await _dbContext.SaveChangesAsync();
        }
        catch
        {
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task<OperationResult> ExecuteProcessAsync(
        string fileName,
        string arguments,
        bool requireAdministrator = false,
        int timeoutMs = DefaultTimeoutMs,
        Action<string>? onOutput = null
    )
    {
        if (requireAdministrator && !IsRunningAsAdministrator())
        {
            return OperationResult.Failed("当前操作需要管理员权限。", requiresElevation: true);
        }

        Process? process = null;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            process = new Process { StartInfo = startInfo };
            onOutput?.Invoke($"> {fileName} {arguments}");
            process.Start();

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            await process.WaitForExitAsync(timeoutCts.Token);

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;

            PushOutputLines(stdOut, onOutput);
            PushOutputLines(stdErr, onOutput);

            if (process.ExitCode == 0)
            {
                return OperationResult.Succeeded("命令执行成功。", stdOut, exitCode: process.ExitCode);
            }

            return OperationResult.Failed(
                $"命令执行失败，退出码: {process.ExitCode}",
                output: stdOut,
                errorOutput: stdErr,
                exitCode: process.ExitCode
            );
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (process is { HasExited: false })
                {
                    process.Kill(true);
                }
            }
            catch
            {
            }

            return OperationResult.Failed($"命令执行超时（>{timeoutMs / 1000}s）。");
        }
        catch (Win32Exception ex)
        {
            return OperationResult.Failed("命令启动失败。", errorOutput: ex.Message);
        }
        catch (Exception ex)
        {
            return OperationResult.Failed("命令执行异常。", errorOutput: ex.Message);
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static void PushOutputLines(string content, Action<string>? onOutput)
    {
        if (onOutput is null || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            onOutput(line.TrimEnd());
        }
    }

    private static string? ResolveScriptPath(string scriptName)
    {
        var direct = Path.Combine(AppContext.BaseDirectory, "Scripts", scriptName);
        if (File.Exists(direct))
        {
            return direct;
        }

        var rootRelative = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Scripts",
            scriptName
        );
        return File.Exists(rootRelative) ? rootRelative : null;
    }

    private static bool ContainsAny(string source, params string[] patterns)
    {
        return patterns.Any(pattern => source.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static string QuoteArgument(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return "\"\"";
        }

        return argument.Contains(' ') ? $"\"{argument.Replace("\"", "\\\"")}\"" : argument;
    }
}
