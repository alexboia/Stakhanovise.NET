. .\common-manifest.ps1
. .\common-projects.ps1
. .\common-nuget.ps1

function Publish-Projects-All {
	[string[]]$csprojFiles = (Get-AllProjectFiles)
	[string]$apiKey = (Get-NuGetApiKey)
	[string]$sourceUrl = (Get-NuGetSourceUrl)
	[string]$version = (Get-CurrentVersion)
	[string]$currentWorkingDir = (Get-Location)

	if (($apiKey -eq $null) -or ($apiKey -like '')) {
		Write-Host "NuGet api key not set. Run ./nuget-setup.ps1 to configure NuGet publishing" -ForegroundColor Red
		return
	}

	if (($sourceUrl -eq $null) -or ($sourceUrl -like '')) {
		Write-Host "NuGet source URL not set. Run ./nuget-setup.ps1 to configure NuGet publishing" -ForegroundColor Red
		return
	}

	Foreach ($csprojFile in $csprojFiles) {
		[string]$projectName = (Get-ProjectName -csprojFile $csprojFile)
		
		Set-Location $currentWorkingDir
		Publish-Project -projectName $projectName -configuration "Release" -packageVersion $version -apiKey $apiKey -sourceUrl $sourceUrl
	}

	Write-Host ("Done publishing NuGet packages") -ForegroundColor DarkGreen
}

Publish-Projects-All