<#
.SYNOPSIS
	Shared console-output helpers for the repository's build and maintenance scripts.

.DESCRIPTION
	Dot-source this file to get Write-BuildMessage, which renders coloured console
	output the same way Write-Host does — by writing a HostInformationMessage to the
	information stream — but, unlike Write-Host, stays capturable, redirectable and
	suppressible. That keeps the scripts readable in a terminal without tripping
	PSScriptAnalyzer's PSAvoidUsingWriteHost rule.

.EXAMPLE
	. (Join-Path $PSScriptRoot 'Build/BuildOutput.ps1')
	Write-BuildMessage 'Building...' -ForegroundColor Yellow
#>

function Write-BuildMessage
{
	[CmdletBinding()]
	param(
		[Parameter(Position = 0, ValueFromPipeline = $true)]
		[AllowEmptyString()]
		[AllowNull()]
		[string]$Message = '',

		[System.ConsoleColor]$ForegroundColor
	)

	process
	{
		$record = [System.Management.Automation.HostInformationMessage]@{
			Message   = $Message
			NoNewline = $false
		}

		if ($PSBoundParameters.ContainsKey('ForegroundColor'))
		{
			$record.ForegroundColor = $ForegroundColor
		}

		Write-Information $record -InformationAction Continue
	}
}
