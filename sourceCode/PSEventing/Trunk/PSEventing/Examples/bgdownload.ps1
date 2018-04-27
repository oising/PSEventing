
if ((get-pssnapin pseventing -ea silentlycontinue) -eq $null) { Add-PSSnapin pseventing }

function queue-download($url) {
    if ((test-path variable:__dlqueue) -eq $false) {
        # create a download queue
        $global:__dlqueue = @{i=0;q=@()}        
    }
    
    if ($__dlqueue.i -lt 3) {        
        
        $i = ++$__dlqueue.i
        
        $var = set-item variable:wc$i -value (new-object system.net.webclient) -passthru
        $__dlqueue.q[$i] = $var
        
        connect-event $var DownloadProgressChanged,DownloadFileCompleted
        
        $wc = $var.Value
        
        $file =  $url.substring($url.lastindexof("/") + 1)
        $id = $file.gethashcode()
    
        "starting async download of $file ..."
        $wc.DownloadFileAsync($url, [system.io.path]::combine([system.io.path]::gettempdir(), $file), $file)
        
    } else {
        "maximum number of downloads already in progress."
    }
}

# Occurred             Source      Name     Args                                                                          
# --------             ------      ----     ----                                                                          
# 5/13/2007 8:04:20 PM variable:wc Disposed System.EventArgs                                                              

function watch-queue() {
    if (test-path variable:__dlqueue) {
        while ($__dlqueue.i -gt 0) {                       
            foreach ($event in (read-event -wait)) {
                # Write-Progress
                #   [-activity] <string>
                #   [-status] <string>
                #   [[-id] <int>]
                #   [-percentComplete <int>]
                #   [-secondsRemaining <int>]
                #   [-currentOperation <string>]
                #   [-parentId <int>]
                #   [-completed]
                #   [-sourceId <int>]
                #   [<CommonParameters>]
            }
        }
    }
}