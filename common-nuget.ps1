function Get-NuGetApiKey {
	return $env:SK_NUGET_APIKEY
}

function Get-NuGetSourceUrl {
	return $env:SK_NUGET_SOURCE
}

function Publish-Project {
	param (
		[string]$projectName,
		[string]$configuration,
		[string]$packageVersion,
		[string]$apiKey,
		[string]$sourceUrl
	)

	Write-Host ("Publishing project " + $projectName + ", version = " + $packageVersion + "...") -ForegroundColor Yellow

	cd ./$projectName
	dotnet clean -c $configuration
	dotnet build -c $configuration

	[string]$projectFileName = ($projectName + ".csproj")
	dotnet pack $projectFileName -c $configuration -p:PackageVersion=$packageVersion

	cd ./bin/$configuration
	dotnet nuget push .\$projectName.$packageVersion.nupkg --api-key $ApiKey --source $sourceUrl --skip-duplicates
	cd ../../..
}