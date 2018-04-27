# source: Jim Truher's blog at http://jtruher.spaces.live.com/
# title:  "background jobs and powershell"

# get my job
function get-job([int[]]$range = $(0..($jobs.count-1)))
{
    $jobs[$range] 
}

# make a UNIX like alias
set-alias jobs get-job
function clear-job
{
    # remove all the variables that hold my job info
    rm variable:jobs
    rm variable:jobshash
    # call the garbage collector, just because I can
    [system.gc]::Collect()
}