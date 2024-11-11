# This script updates all .csproj files to target 10.0.26100.0 and set the minimum OS version
Set-Location ..\
Get-ChildItem -Recurse -Filter "*.csproj" | ForEach-Object {
    $csprojFile = $_.FullName
    (Get-Content $csprojFile) -replace "<TargetPlatformVersion>.*<\/TargetPlatformVersion>", "<TargetPlatformVersion>10.0.26100.0</TargetPlatformVersion>" `
        -replace "<TargetPlatformMinVersion>.*<\/TargetPlatformMinVersion>", "<TargetPlatformMinVersion>10.0.18362.0</TargetPlatformMinVersion>" | Set-Content $csprojFile
    Write-Host "Updated: $csprojFile"
}
