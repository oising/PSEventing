#---------------------------------------------------------
### <Script>
### <Author>Keith Hill</Author>
### <Description>
### This script generates the about_PSEventing.help.txt file
### </Description>
### </Script>
#---------------------------------------------------------
param([string]$outputDir = $(throw "You must specify the output path to emit the generated file"))

function WrapText($width = 74, $indent = 8) {

	begin {
		$indent = " " * $indent
		$whitespace = "`t`r`n ".ToCharArray()
		[Text.StringBuilder]$output = $indent
		$currentLineWidth = 0
	}

	process {
		if (!$_) {
			return
		}
		
		$words = $_.Split($whitespace, [StringSplitOptions]::RemoveEmptyEntries)
		
		$index = 0
		$count = 0
		
		while(($index + $count) -lt $words.Length) {
		
			while($currentLineWidth -le $width) {
				$w = $words[$index + $count].Length + 1;
				
				if(($currentLineWidth + $w) -gt $width) {
					break
				}
				
				$currentLineWidth += $w
				$count++
			}
			
			if($count -gt 0) {
				$words[$index..($index + $count - 1)] |% {
					$output.Append($_)  | out-null
					$output.Append(' ') | out-null
				}
				
				$output.AppendLine() | out-null
				$output.Append($indent) | out-null
			}
			else {
				$word = $words[$index]
				$line = $null;
				
				do {
					if($word.Length -gt $width) {
						$line = $word.Substring(0, $width)
						$word = $word.Substring($width)
					}
					else {
						$line = $word;
						$word = $null;
					}
					
					$output.AppendLine($indent + $line) | out-null
				} while($word)
				
				$count++
			}
		
			$index += $count

			$count = 0
			$currentLineWidth = 0
		}
	}

	end {
		$output.ToString()
	}
}

function WriteLine {
	"" | OutAboutHelpFile
}

filter OutAboutHelpFile {
	$_ | Out-File $AboutPSEventingHelpPath -Encoding Utf8 -Append
}

#---------------------------------------------------------
# Script main
#---------------------------------------------------------
$outputDir   = Resolve-Path $outputDir
$templateDir = Split-Path $outputDir -parent
$providerHelpPath = Split-Path $outputDir -parent

$AboutPSEventingHelpPath = (join-path $outputDir about_PSEventing.help.txt) 
$MergedHelpPath    = (join-path $outputDir MergedHelp.xml)

Add-PSSnapin PSEventing -ea SilentlyContinue
$PSEventingSnapin = Get-PSSnapin PSEventing

$filters = $null
$functions = $null
$scripts = @()
foreach ($file in (Get-ChildItem $scriptHelpDir\*.xml)) {
	$xml = [xml](get-content $file)
	$filters += $xml.SelectNodes('//Filter')
	$functions += $xml.SelectNodes('//Function')
	if ($xml.SelectSingleNode('//Script')) {
		$scripts += $xml
	}
}
$filters   = $filters | sort name
$functions = $functions | sort name
$scripts   = $scripts | sort @{e={$_.ScriptFile.path}}

#---------------------------------------------------------
# Insert Header template
#---------------------------------------------------------
New-Item $AboutPSEventingHelpPath -Type File -Force > $null
$version = "$($PSEventingSnapin.Version.Major).$($PSEventingSnapin.Version.Minor)"
Get-Content (join-path $templateDir about_PSEventing_header.txt) |
	% { $_ -replace '<VERSION_NUMBER>', $version } | OutAboutHelpFile
	
#---------------------------------------------------------
# Process Cmdlets
#---------------------------------------------------------
WriteLine
"CMDLETS" | OutAboutHelpFile
"    The current PSEventing cmdlets are listed below:" | OutAboutHelpFile
WriteLine
$MergedXml = [Xml](Get-Content $MergedHelpPath)
$MergedXml.Cmdlets.Cmdlet | Sort Noun, Verb | % {
	"    $($_.Verb)-$($_.Noun)"
	$_.DetailedDescription.Trim() | WrapText
} | OutAboutHelpFile

#---------------------------------------------------------
# Append footer template
#---------------------------------------------------------
Get-Content (join-path $templateDir about_PSEventing_footer.txt) | OutAboutHelpFile
