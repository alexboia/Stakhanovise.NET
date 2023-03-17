function Get-CurrentVersion {
	$manifest = (Get-CommonsManifestData)
	return ($manifest.version)
}

function Get-FullVersionNumber {
	param([string]$versionNumber)
	return ($versionNumber + ".0")
}

function Get-CurrentFullVersionNumber {
	$version = (Get-CurrentVersion)
	return (Get-FullVersionNumber -versionNumber $version)
}

function Get-ManifestXml {
	[string]$manifestFileName = (gi .\manifest.xml).FullName
	[xml]$manifest = (Get-Content $manifestFileName)
	return $manifest
}

function Get-CommonsManifestData {
	[xml]$manifest = (Get-ManifestXml)
	[System.Xml.XmlNamespaceManager]$xMgr = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
	[System.Collections.Hashtable]$manifestData = @{}

	$manifestData["version"] = (_Get-CommonVersion -manifest $manifest)
	$manifestData["authors"] = (_Get-CommonAuthors -manifest $manifest)
	$manifestData["company"] = (_Get-CommonCompany -manifest $manifest)
	$manifestData["description"] = (_Get-CommonDescription -manifest $manifest)
	$manifestData["tags"] = (_Get-CommonTags -manifest $manifest)
	$manifestData["releaseNotes"] = (_Get-CommonReleaseNotes -manifest $manifest)

	return $manifestData
}

function _Get-CommonVersion {
	param (
		[xml]$manifest
	)

	[System.Xml.XmlNode]$versionNode = $manifest.SelectSingleNode("/manifest/version", $xMgr)
	if ($versionNode -ne $null) {
		return $versionNode.InnerText.Trim()
	} else {
		return ""
	}
}

function _Get-CommonAuthors {
	param (
		[xml]$manifest
	)
	[System.Xml.XmlNode]$authorsNode = $manifest.SelectSingleNode("/manifest/authors", $xMgr)
	if ($authorsNode -ne $null) {
		return $authorsNode.InnerText.Trim()
	} else {
		return ""
	}
}

function _Get-CommonCompany {
	param(
		[xml]$manifest
	)

	[System.Xml.XmlNode]$companyNode = $manifest.SelectSingleNode("/manifest/company", $xMgr)
	if ($companyNode -ne $null) {
		return $companyNode.InnerText.Trim()
	} else {
		return ""
	}
}

function _Get-CommonDescription {
	param(
		[xml]$manifest
	)

	[System.Xml.XmlNode]$descriptionNode = $manifest.SelectSingleNode("/manifest/description", $xMgr)	
	if ($descriptionNode -ne $null) {
		return $descriptionNode.InnerText.Trim()
	} else {
		return ""
	}
}

function _Get-CommonTags {
	param(
		[xml]$manifest
	)

	[System.Xml.XmlNode]$tagsNode = $manifest.SelectSingleNode("/manifest/tags", $xMgr)
	if ($tagsNode -ne $null) {
		return $tagsNode.InnerText.Trim()
	} else {
		return ""
	}
}

function _Get-CommonReleaseNotes {
	param(
		[xml]$manifest
	)

	[System.Xml.XmlNode]$releaseNotesNode = $manifest.SelectSingleNode("/manifest/releaseNotes", $xMgr)
	if ($releaseNotesNode -ne $null) {
		return $releaseNotesNode.InnerText.Trim()
	} else {
		return ""
	}
}

function Get-PerProjectManifestData {
	[xml]$manifest = (Get-ManifestXml)
	[System.Xml.XmlNamespaceManager]$xMgr = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
	
	[string]$commonDescription = (_Get-CommonDescription -manifest $manifest)
	[string]$commonReleaseNotes = (_Get-CommonReleaseNotes -manifest $manifest)

	$result = New-Object PsObject -Property @{descriptions=$null;releaseNotes=$null}
	$result.descriptions = @{}
	$result.releaseNotes = @{}

	$projectNodes = $manifest.SelectNodes("/manifest/packages/p")
	if ($projectNodes -ne $null) {
		foreach ($pNode in $projectNodes) {
			$nameNode = $pNode.Attributes["name"]
			if ($nameNode -ne $null) {
				[string]$name = $nameNode.Value
				[string]$description = $commonDescription
				[string]$releaseNotes = $commonReleaseNotes
				
				[System.Xml.XmlNode]$descriptionNode = $pNode.SelectSingleNode("description")
				if ($descriptionNode -ne $null) {
					$descriptionModeNode = $descriptionNode.Attributes["mode"]
					if ($descriptionModeNode -ne $null -and $descriptionModeNode.Value -eq "replace") {
						$description = $descriptionNode.InnerText
					} else {
						$description = ($description + ". " + $descriptionNode.InnerText)
					}
				}

				[System.Xml.XmlNode]$releaseNotesNode = $pNode.SelectSingleNode("releaseNotes")
				if ($releaseNotesNode -ne $null) {
					$releaseNotesModeNode = $releaseNotesNode.Attributes["mode"]
					if ($releaseNotesModeNode -ne $null -and $releaseNotesModeNode.Value -eq "replace") {
						$releaseNotes = $releaseNotesNode.InnerText
					} else {
						$releaseNotes = ($releaseNotes + ". " + $releaseNotesNode.InnerText)
					}
				}

				$result.descriptions[$name] = $description
				$result.releaseNotes[$name] = $releaseNotes
			}
		}
	}

	return $result
}