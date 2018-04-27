# pseventing 1.0

if ((get-pssnapin pseventing -ea silentlycontinue) -eq $null) { add-pssnapin pseventing }

$smopath = 'C:\Program Files\Microsoft SQL Server\90\SDK\Assemblies'
$dbname = 'Northwind'
$filename = 'Northwind.bkp'

[System.Reflection.Assembly]::LoadFile("$smopath\Microsoft.SqlServer.ConnectionInfo.dll")
[System.Reflection.Assembly]::LoadFile("$smopath\Microsoft.SqlServer.Smo.dll")
[System.Reflection.Assembly]::LoadFile("$smopath\Microsoft.SqlServer.SqlEnum.dll")
[System.Reflection.Assembly]::LoadFile("$smopath\Microsoft.SqlServer.SmoEnum.dll")

$backup = new-object Microsoft.SqlServer.Management.Smo.Backup

$backup.Database = $dbname
$backup.PercentCompleteNotification = 1

$DeviceType = [Microsoft.SqlServer.Management.Smo.DeviceType]::File
$backupItem = new-object Microsoft.SqlServer.Management.Smo.BackupDeviceItem($filename,$DeviceType)
$backup.devices.add($backupItem)

connect-event backup percentcomplete
$backup.SqlBackupAsync(".") # server name, e.g. .\sqlexpress

trap { 
    disconnect-event backup percentcomplete
}

$percent = 0

while ($percent -lt 100)  {
    
    $events = @(read-event -wait)
    
    foreach ($event in $events) {
    
        if ($event.Args -is [Microsoft.SqlServer.Management.Smo.PercentCompleteEventArgs]) {
        
            $percent = $event.args.percent
            write-progress -activity "Database Backup" -status $dbname -percentComplete $percent
        }
    }
}

disconnect-event backup percentcomplete
"done."