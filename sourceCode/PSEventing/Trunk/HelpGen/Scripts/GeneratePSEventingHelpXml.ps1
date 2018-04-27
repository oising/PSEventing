#---------------------------------------------------------
# Desc: This script generates the Nivot.PowerShell.Eventing.dll-Help.xml file
#---------------------------------------------------------
param([string]$outputDir = $(throw "You must specify the output path to emit the generated file"),
      [string]$localizedHelpPath = $(throw "You must specify the path to the localized help dir"))

$outputDir   = Resolve-Path $outputDir
$transformsDir = Join-Path (Split-Path $outputDir -parent) Transformations
$providerHelpPath = Split-Path $outputDir -parent

$PSEventingHelpPath = (join-path $outputDir Nivot.PowerShell.Eventing.dll-Help.xml) 
$MergedHelpPath = (join-path $outputDir MergedHelp.xml)

# needed for Test-Xml
Add-PSSnapin Pscx -ea SilentlyContinue

# Test the XML help files
gci $localizedHelpPath\*.xml  | Foreach {
	if (!(Test-Xml $_)) {
		Test-Xml $_ -verbose
		Write-Error "$_ is not a valid XML file"
		exit 1
	}
}

gci $providerHelpPath\Provider*.xml  | Foreach {
	if (!(Test-Xml $_)) {
		Test-Xml $_ -verbose
		Write-Error "$_ is not a valid XML file"
		exit 1
	}
}

Add-PSSnapin PSEventing -ea SilentlyContinue
Get-PSSnapinHelp (Get-PSSnapin PSEventing).ModuleName -LocalizedHelpPath $localizedHelpPath > $MergedHelpPath

Convert-Xml $MergedHelpPath -xslt $transformsDir\Maml.xslt | Out-File $PSEventingHelpPath -Encoding Utf8

# Low tech approach to merging in the provider help
$helpfile = Get-Content $PSEventingHelpPath | ? {$_ -notmatch '</helpItems>'}
$providerHelp = @()
gci $providerHelpPath\Provider*.xml | ? {$_.Name -notmatch 'Provider_template'} | Foreach {
	Write-Host "Processing $_"
	$providerHelp += Get-Content $_
}

$helpfile += $providerHelp
$helpfile += '</helpItems>'
$helpfile > $PSEventingHelpPath