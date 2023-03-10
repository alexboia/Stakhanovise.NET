function Get-CopyrightNotice {
	param([string]$author)

	[string]$year = (Get-Date -f yyyy)
	[string]$notice = ("Copyright (c) 2020-" + $year + ", " + $author)

	return $notice
}