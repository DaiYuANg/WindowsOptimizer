echo [*] 开启开发模式：启用 Hyper-V、VBS、WSL、Docker 所需组件...

# 启用 Hyper-V
dism /online /enable-feature /featurename:Microsoft-Hyper-V-All /norestart

# 启用 WSL 和虚拟机平台
dism /online /enable-feature /featurename:VirtualMachinePlatform /norestart
dism /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /norestart

# 启用安全虚拟化 (VBS)
bcdedit /set hypervisorlaunchtype auto

# 启用核心隔离 / 内存完整性
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 1 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 1 /f

echo [*] 所有功能已启用，系统将在重启后生效。
pause
shutdown /r /t 5
