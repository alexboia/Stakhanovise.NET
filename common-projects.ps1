function Get-AllProjectFiles {
	[string[]]$csprojFiles = @(
		(gi .\LVD.Stakhanovise.NET.Common.Interfaces\LVD.Stakhanovise.NET.Common.Interfaces.csproj).FullName,
		(gi .\LVD.Stakhanovise.NET.Interfaces\LVD.Stakhanovise.NET.Interfaces.csproj).FullName,
		(gi .\LVD.Stakhanovise.NET.Common\LVD.Stakhanovise.NET.Common.csproj).FullName,
	
		(gi .\LVD.Stakhanovise.NET\LVD.Stakhanovise.NET.csproj).FullName,
	
		(gi .\LVD.Stakhanovise.NET.Info\LVD.Stakhanovise.NET.Info.csproj).FullName,
		(gi .\LVD.Stakhanovise.NET.Producer\LVD.Stakhanovise.NET.Producer.csproj).FullName,

		(gi .\LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings\LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings.csproj).FullName,
		(gi .\LVD.Stakhanovise.NET.Logging.Log4NetLogging\LVD.Stakhanovise.NET.Logging.Log4NetLogging.csproj).FullName
	)

	return $csprojFiles
}

function Get-ProjectFileName {
	param(
		[string]$csprojFile
	)

	[string]$csprojFileName = (Split-path $csprojFile -leaf)
	return $csprojFileName
}

function Get-ProjectName {
	param(
		[string]$csprojFile
	)

	[string]$csprojFileName = (ProjectFileName -csprojFile $csprojFile)
	return $csprojFileName.Replace(".csproj", "")
}