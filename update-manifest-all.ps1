. .\common-manifest.ps1
. .\common-projects.ps1

function Set-Manifest {
	param(
		[string]$csprojFile, 
		[Hashtable]$commonData,
		[PsObject]$perProjectData
	)
	
	[xml]$csproj = (Get-Content $csprojFile)
	[System.Xml.XmlNamespaceManager]$xMgr = New-Object System.Xml.XmlNamespaceManager($csproj.NameTable)

	[string]$versionNumber = $commonData.version
	[string]$fullVersionNumber = (Get-FullVersionNumber -versionNumber $versionNumber)

	[string]$csprojName = (Get-ProjectName -csprojFile $csprojFile)
	[string]$csprojFileName = (Get-ProjectFileName -csprojFile $csprojFile)

	Write-Host ("Processing " + $csprojName + ", file = " + $csprojFileName + ", version number = "  + $versionNumber + "...") -ForegroundColor Yellow

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

	[System.Xml.XmlNode]$assemblyVersionNode = $csproj.SelectSingleNode("/Project/PropertyGroup/AssemblyVersion", $xMgr)
	if ($assemblyVersionNode -ne $null) {
		$assemblyVersionNode.InnerText = $fullVersionNumber
	}

	[System.Xml.XmlNode]$authorsNode = $csproj.SelectSingleNode("/Project/PropertyGroup/Authors", $xMgr)
	if ($authorsNode -ne $null) {
		$authorsNode.InnerText = $commonData.authors
	}

	[System.Xml.XmlNode]$companyNode = $csproj.SelectSingleNode("/Project/PropertyGroup/Company", $xMgr)
	if ($companyNode -ne $null) {
		$companyNode.InnerText = $commonData.company
	}

	[System.Xml.XmlNode]$packageReleaseNotesNode = $csproj.SelectSingleNode("/Project/PropertyGroup/PackageReleaseNotes", $xMgr)
	if ($packageReleaseNotesNode -ne $null) {
		$releaseNotes = $commonData.releaseNotes
		if ($perProjectData.releaseNotes[$csprojName] -ne $null) {
			$releaseNotes = $perProjectData.releaseNotes[$csprojName]
		}

		$packageReleaseNotesNode.InnerText = $releaseNotes
	}

	[System.Xml.XmlNode]$packageDescriptionNode = $csproj.SelectSingleNode("/Project/PropertyGroup/Description", $xMgr)
	if ($packageDescriptionNode -ne $null) {
		$description = $commonData.description
		if ($perProjectData.descriptions[$csprojName] -ne $null) {
			$description = $perProjectData.descriptions[$csprojName]
		}

		$packageDescriptionNode.InnerText = $description
	}

	$csproj.Save($csprojFile) | Out-Null
}

function Set-Manifest-All {
	[string[]]$csprojFiles = (Get-AllProjectFiles)
	[Hashtable]$commonManifestData = (Get-CommonsManifestData)
	[PsObject]$perProjectManifestData = (Get-PerProjectManifestData)

	Foreach ($csprojFile in $csprojFiles) {
		Set-Manifest -csprojFile $csprojFile -commonData $commonManifestData -perProjectData $perProjectManifestData
	}

	Write-Host ("Done setting manifest data") -ForegroundColor DarkGreen
}

Set-Manifest-All