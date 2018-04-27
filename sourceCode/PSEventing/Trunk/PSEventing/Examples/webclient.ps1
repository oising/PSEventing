# webclient

if ((get-pssnapin pseventing -ea silentlycontinue) -eq $null) { Add-PSSnapin pseventing }

$bel = "`a"

function dbg([string]$msg) {
    [system.diagnostics.debug]::writeline($msg, "webclient.ps1 : " + [system.threading.thread]::currentthread.managedthreadid)
}

function download-async($url) {

    $wc = new-object system.net.webclient
    connect-event wc downloadprogresschanged,downloadfilecompleted -verbose

    $file =  $url.substring($url.lastindexof("/") + 1)
    $id = $file.gethashcode()
    
    "starting async webclient download of $file ..."
    $wc.DownloadFileAsync($url, [system.io.path]::combine([system.io.path]::gettempdir(), $file), $file)
    
    return @($id, (get-item variable:wc))
}

function process-downloads($hash) {
    
    start-keyhandler -capturectrlc
    
    trap {
        "${bel}error; unhooking keyhandler."
        stop-keyhandler
    }
    
    $done = $false
    $cancelled = $false

    while (!$done) {

        foreach ($event in (read-event -wait)) {
            
            switch ($event.name) {

                "ctrlc" {
                    dbg("cancelling...")
                    
                    $wc.CancelAsync() # generates downloadfilecompleted event
                    
                    if ($cancelled -eq $false) {
                        # first attempt, let $wc do the work
                        $cancelled = $true
                    } else {
                        # second time around (cancel async is blocked, or impatient user!), lets force exit
                        dbg("force exit.")
                        $done = $true
                    }
                }
                
                "downloadprogresschanged" {
                    write-progress -id $id -activity "downloading $url" -status $("{0} of {1} byte(s)" -f @($event.args.BytesReceived, $event.args.TotalBytesToReceive)) -percent $event.args.ProgressPercentage
                }
                
                "downloadfilecompleted" {
                    $done = $true
                }
                
                default {
                    $event
                    throw "unexpected event!"
                }
            }
        }    
    }

    if ($cancelled)
        { "user cancelled operation." }
    else
        { "downloaded successfully." }
        
}
function cleanup() {
    disconnect-event wc downloadprogresschanged,downloadfilecompleted -verbose
    stop-keyhandler
}

$wc = new-object system.net.webclient
connect-event wc downloadprogresschanged,downloadfilecompleted -verbose
start-keyhandler -capturectrlc

trap {
    "error caught: cleaning up"
    cleanup()
}

#$url = [uri]"http://images.google.com/intl/en_ALL/images/images_hp.gif"
#$url = [uri]"http://www.firstpr.com.au/astrophysics/hubble-deep-field/hubble-deep-field-northern-detail-rw.bmp" # 2MB
#$url = [uri]"ftp://ftp.microsoft.com/Softlib/MSLFILES/smsupda.exe" # 107MB
#$url = [uri]"http://www.microsoft.com/downloads/info.aspx?na=90&p=&SrcDisplayLang=en&SrcCategoryId=&SrcFamilyId=049c9dbe-3b8e-4f30-8245-9e368d3cdb5a&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f1%2f6%2f5%2f165b076b-aaa9-443d-84f0-73cf11fdcdf8%2fWindowsXP-KB835935-SP2-ENU.exe"

$url1 = [uri]"ftp://ftp.microsoft.com/Softlib/MSLFILES/smsupda.exe" # 107MB
$url2 = [uri]"http://localhost/WindowsXP-KB835935-SP2-ENU.exe" # 270MB

$d1 = download-async($url1)
$d2 = download-async($url2)

# create hashtable of id => webclient pairs
$downloads = @{ $d1[0] = $d1[1], $d2[0] = $d2[1] }

process-downloads($downloads)

cleanup()
