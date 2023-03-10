function Get-CurrentVersion() {
	return "1.0.5"
}

function Get-FullVersionNumber {
	param([string]$versionNumber)
	return ($versionNumber + ".0")
}