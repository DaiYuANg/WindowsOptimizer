echo [*] 开启游戏模式：关闭 Hyper-V、VBS、虚拟化平台...

# 关闭 Hyper-V
dism /online /disable-feature /featurename:Microsoft-Hyper-V-All /norestart

# 关闭 WSL、虚拟机平台
dism /online /disable-feature /featurename:VirtualMachinePlatform /norestart
dism /online /disable-feature /featurename:Microsoft-Windows-Subsystem-Linux /norestart

# 禁用 hypervisor
bcdedit /set hypervisorlaunchtype off

# 禁用 VBS / 核心隔离
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 0 /f

echo [*] 所有虚拟化功能已关闭，系统将在重启后生效。
pause
shutdown /r /t 5
