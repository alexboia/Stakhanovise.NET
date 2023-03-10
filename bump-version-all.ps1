Param(
	[Parameter(Mandatory=$True)]
	[string]$versionNumber
)

. .\common-manifest.ps1
. .\common-projects.ps1

function Set-Version {
	param(
		[string]$csprojFile, 
		[string]$versionNumber, 
		[string]$fullVersionNumber
	)

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

function Set-AllVersions {
	[string]$fullVersionNumber = (Get-FullVersionNumber -versionNumber $versionNumber)
	[string[]]$csprojFiles = (Get-AllProjectFiles)

	Foreach ($csprojFile in $csprojFiles) {
		Set-Version -csprojFile $csprojFile -versionNumber $versionNumber -fullVersionNumber $fullVersionNumber
	}

	Write-Host ("Done setting version numbers to " + $versionNumber) -ForegroundColor DarkGreen
}

Set-AllVersions