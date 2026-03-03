param(
    [switch]$AutoReboot
)

$ErrorActionPreference = "Stop"

function Invoke-Tool {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList
    )

    Write-Host "[>] $FilePath $($ArgumentList -join ' ')"
    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $FilePath (exit code: $LASTEXITCODE)"
    }
}

Write-Host "[*] 开启游戏模式：关闭 Hyper-V、VBS、虚拟化平台..."

Invoke-Tool -FilePath "dism" -ArgumentList @("/online", "/disable-feature", "/featurename:Microsoft-Hyper-V-All", "/norestart")
Invoke-Tool -FilePath "dism" -ArgumentList @("/online", "/disable-feature", "/featurename:VirtualMachinePlatform", "/norestart")
Invoke-Tool -FilePath "dism" -ArgumentList @("/online", "/disable-feature", "/featurename:Microsoft-Windows-Subsystem-Linux", "/norestart")
Invoke-Tool -FilePath "bcdedit" -ArgumentList @("/set", "hypervisorlaunchtype", "off")
Invoke-Tool -FilePath "reg" -ArgumentList @("add", "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "/v", "EnableVirtualizationBasedSecurity", "/t", "REG_DWORD", "/d", "0", "/f")
Invoke-Tool -FilePath "reg" -ArgumentList @("add", "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "/v", "Enabled", "/t", "REG_DWORD", "/d", "0", "/f")

Write-Host "[*] 游戏模式操作已完成。重启后生效。"

if ($AutoReboot) {
    Write-Host "[*] 5 秒后自动重启..."
    Invoke-Tool -FilePath "shutdown" -ArgumentList @("/r", "/t", "5")
} else {
    Write-Host "[*] 当前未自动重启。请手动重启系统以应用配置。"
}
