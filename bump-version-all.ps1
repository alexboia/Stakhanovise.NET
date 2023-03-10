Param(
	[Parameter(Mandatory=$True)]
	[string]$versionNumber
)

[string]$fullVersionNumber = $versionNumber + ".0"
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

function Set-Version {
	param([string]$csprojFile, 
		[string]$versionNumber, 
		[string]$fullVersionNumber)

	[string]$csprojFileName = (Split-path $csprojFile -leaf)
	Write-Host ("Processing " + $csprojFileName + ", version number = "  + $versionNumber + "...") -ForegroundColor Yellow
		
	[xml]$csproj = (Get-Content $csprojFile)
	[System.Xml.XmlNamespaceManager]$xMgr = New-Object System.Xml.XmlNamespaceManager($csproj.NameTable)

	[System.Xml.XmlNode]$versionNode = $csproj.SelectSingleNode("/Project/PropertyGroup/Version", $xMgr)
	if ($versionNode -ne $null) {
		$versionNode.InnerText = $versionNumber
	}

	[System.Xml.XmlNode]$fileVersionNode = $csproj.SelectSingleNode("/Project/PropertyGroup/FileVersion", $xMgr)
	if ($fileVersionNode -ne $null) {
		$fileVersionNode.InnerText = $fullVersionNumber
	}

	[System.Xml.XmlNode]$assemblyVersionNode = $csproj.SelectSingleNode("/Project/PropertyGroup/AssemblyVersion", $xMgr)
	if ($assemblyVersionNode -ne $null) {
		$assemblyVersionNode.InnerText = $fullVersionNumber
	}

	$csproj.Save($csprojFile) | Out-Null
}

Foreach ($csprojFile in $csprojFiles) {
	Set-Version -csprojFile $csprojFile -versionNumber $versionNumber -fullVersionNumber $fullVersionNumber
}

Write-Host ("Done setting version numbers to " + $versionNumber) -ForegroundColor DarkGreen