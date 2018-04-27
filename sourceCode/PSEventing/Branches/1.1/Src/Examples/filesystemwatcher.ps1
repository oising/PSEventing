# pseventing 1.1
# filesystemwatcher

Add-PSSnapin pseventing

$fsw = new-object system.io.filesystemwatcher
$fsw.Path = [io.path]::GetTempPath()
$fsw.EnableRaisingEvents = $true
connect-event fsw changed,deleted -verbose

"watching $($fsw.path)"
"entering loop... ctrl+c to exit"

$done = $false;
while ($events = @(read-event -wait)) {

	# read-event always returns a collection
	foreach ($event in $events) {	    
		switch ($event.name) {
		    "ctrlc" {
		        "cancelling..."
		        $done = $true
		    }
		    
			"changed" {
			    $event
			    #$done = $true
			}
			
			"deleted" {
				$event
			}
		}
	}
	
	if ($done -eq $true) {
		break
	}
}