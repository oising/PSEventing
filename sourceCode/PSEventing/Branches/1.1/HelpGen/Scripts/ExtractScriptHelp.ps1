#---------------------------------------------------------
### <Script>
### <Author>Keith Hill</Author>
### <Description>This script extracts the XML comments from
### PowerShell script files and emits an XML file describing
### the documented contents of that script file.
### </Description>
### </Script>
#---------------------------------------------------------

param([string[]]$paths = $(throw "You must specify the path to one or more PowerShell script files"), 
      [string]$outputDir = $(throw "You must specify the output path to emit the generated file"))

begin {
	$outputDir   = Resolve-Path $outputDir

	function ProcessPath($path) {
		Write-Host "Processing $(split-path $path -leaf)"

		$DocComments = (Get-Content $path | where {$_ -match '^\s*###'} | Foreach {$_ -replace '^\s*###\s*', ''})
		if (!$DocComments) {
			# No XML comments in file so bail
			return 
		}

		$outputPath = Join-Path $outputDir ([IO.Path]::GetFilenameWithoutExtension($path) + ".xml")
		"<ScriptFile path='$path'>" > $outputPath
		
		$DocComments >> $outputPath
		
		"</ScriptFile>" >> $outputPath
	}
}

process {
	if ($_) {
		ProcessPath($_)
	}
}

end {
	foreach ($path in $paths) {
		$rpaths = Resolve-Path $path
		foreach ($rpath in $rpaths) {
			ProcessPath($rpath)	
		}	
	}	
}
