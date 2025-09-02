using System.Management;
using Microsoft.Win32;

namespace WindowsOptimizer.Service;

public interface ISystemInfoService
{
    bool IsVbsEnabled();
}

public class SystemInfoService : ISystemInfoService
{
    public bool IsVbsEnabled()
    {
        // 方法1：通过 WMI Win32_DeviceGuard 检查 SecurityServicesRunning
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\DeviceGuard",
                "SELECT * FROM Win32_DeviceGuard");
            foreach (var o in searcher.Get())
            {
                var obj = (ManagementObject)o;
                var running = obj["SecurityServicesRunning"];
                if (running != null)
                {
                    var arr = (UInt32[])running;
                    if (arr.Length > 0) // 数组有值说明 VBS 或 HVCI 运行
                        return true;
                }
            }
        }
        catch
        {
            // WMI 查询失败，可忽略
        }

        // 方法2：通过注册表检查 EnableVirtualizationBasedSecurity
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard");
            if (key != null)
            {
                var value = key.GetValue("EnableVirtualizationBasedSecurity");
                if (value is int intVal && intVal == 1)
                    return true;
            }
        }
        catch
        {
            // 注册表读取失败，可忽略
        }

        return false; // 都没有检测到开启
    }
}