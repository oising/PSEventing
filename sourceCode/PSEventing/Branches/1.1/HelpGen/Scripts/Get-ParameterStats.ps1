Add-PSSnapin Pscx -ea SilentlyContinue

[xml]$Merged = (Get-PSSnapinHelp (Get-PSSnapin Pscx).ModuleName)
[xml]$Params = ($Merged | Convert-Xml -xslt Transformations\ParameterStats.xslt)

$Params.Cmdlets.Cmdlet | Select Name, @{ Name='Parameter'; E= { $_.Parameter | sort } } | sort { $_.Parameter.Count } -desc | ft -a -wrap