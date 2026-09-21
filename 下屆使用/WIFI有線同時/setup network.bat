@echo off
:: ========================================================
:: 自動以系統管理員身分提權 (Auto Elevate to Administrator)
:: ========================================================
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    pushd "%CD%"
    CD /D "%~dp0"
:: ========================================================

chcp 65001 >nul
echo 正在設定網路介面與路由...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$eth = Get-NetAdapter | Where-Object { $_.PhysicalMediaType -match '802.3|Ethernet' -and $_.Virtual -eq $false -and $_.InterfaceDescription -notmatch 'VPN|TAP|TUN|Virtual|Hyper-V|vEthernet|Tailscale|WireGuard' -and $_.Status -eq 'Up' } | Select-Object -First 1;" ^
    "if ($eth) { Set-NetIPInterface -InterfaceIndex $eth.ifIndex -AddressFamily IPv4 -InterfaceMetric 150; Write-Host ('[V] 已將實體 Ethernet (' + $eth.Name + ') 的 Metric 設為 150') -ForegroundColor Green } else { Write-Host '[-] 未找到作用中的實體 Ethernet 網卡' -ForegroundColor Yellow };" ^
    "$wifi = Get-NetAdapter | Where-Object { $_.PhysicalMediaType -match 'Native 802.11|Wireless' -and $_.Virtual -eq $false -and $_.InterfaceDescription -notmatch 'Virtual|Direct' -and $_.Status -eq 'Up' } | Select-Object -First 1;" ^
    "if ($wifi) { Set-NetIPInterface -InterfaceIndex $wifi.ifIndex -AddressFamily IPv4 -InterfaceMetric 5; Write-Host ('[V] 已將實體 Wi-Fi (' + $wifi.Name + ') 的 Metric 設為 5') -ForegroundColor Green } else { Write-Host '[-] 未找到作用中的實體 Wi-Fi 網卡' -ForegroundColor Yellow };" ^
    "$destIp = '20.239.91.31'; $gw = '192.168.17.1'; $met = '1';" ^
    "route.exe delete $destIp 2>$null | Out-Null;" ^
    "$res = route.exe -p add $destIp mask 255.255.255.255 $gw metric $met;" ^
    "if ($LASTEXITCODE -eq 0) { Write-Host ('[V] 已成功設定持續路由: ' + $destIp + '/32 -> ' + $gw + ' (Metric: ' + $met + ')') -ForegroundColor Green } else { Write-Host '[!] 路由設定失敗' -ForegroundColor Red };"

echo.
echo 設定完成！
echo.
pause