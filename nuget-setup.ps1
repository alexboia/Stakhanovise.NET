$apiKey = (Read-Host "Please enter NuGet API Key for use when publishing SK.NET packages")
[System.Environment]::SetEnvironmentVariable('SK_NUGET_APIKEY', $apiKey, "Machine")

$source = (Read-Host "Please enter NuGet source URL for use when publishing SK.NET packages")
[System.Environment]::SetEnvironmentVariable('SK_NUGET_SOURCE', $source, "Machine")