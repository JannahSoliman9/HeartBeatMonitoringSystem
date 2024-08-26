# List of executable paths
$executables = @(
    "C:\Work\Hearbeat Demo\UdpApp5\bin\Debug\net6.0-windows\UdpApp5.exe",
    "C:\Work\Hearbeat Demo\UdpApp6\bin\Debug\net6.0-windows\UdpApp6.exe",
    "C:\Work\Hearbeat Demo\UdpApp7\bin\Debug\net6.0-windows\UdpApp7.exe",
    "C:\Work\Hearbeat Demo\UdpApp8\bin\Debug\net6.0-windows\UdpApp8.exe",
    "C:\Work\Hearbeat Demo\UdpWatcher\bin\Debug\net6.0\UdpWatcher.exe"
    
)

# Start each executable in parallel
foreach ($exePath in $executables) {
    if (Test-Path $exePath) {
        Start-Process -FilePath $exePath -WorkingDirectory (Split-Path $exePath)
        Write-Host "Started: $exePath"
    } else {
        Write-Host "Executable not found: $exePath"
    }
}

Write-Host "All specified executables have been started."
