
param(
    [ValidateSet('serveo', 'relayo')]
    [string]$RelayServiceName='relayo'
)

. "$PSScriptRoot\_IGNORE\Run-Serveo.local.ps1"

$tmpPath = "$PSScriptRoot/_TMP"
$sshKeyFile = "$tmpPath/Run-Serveo.sshKey"
$fwdTcpRegex = "Forwarding TCP connections from (serveo.net:\S+)"
$fwdHttpRegex = "Forwarding HTTP traffic from (https://\S+)"

if (-not (Test-Path $tmpPath)) {
    mkdir $tmpPath | Out-Null
}

## $dnsNames is defined in LOCAL

$serveoRelayService = @{
    Name           = "Serveo"
    DnsName        = "serveo.net"
    AliasIpAddress = "159.89.214.31"
    SshOptions     = @('-T', '-i', $sshKeyFile)
    AddDnsTxt      = $true
    PatternMatch   = $true
}
## relayoRelayService is defined in LOCAL

switch ($RelayServiceName) {
    'serveo' { $relayService = $serveoRelayService }
    'relayo' { $relayService = $relayoRelayService }
}

$relayDnsName      = $relayService.DnsName
$relayIpAddress    = $relayService.AliasIpAddress
$relaySshOptions   = $relayService.SshOptions
$relayAddDnsTxt    = $relayService.AddDnsTxt
$relayPatternMatch = $relayService.PatternMatch


if (-not (Test-Path -PathType Leaf $sshKeyFile)) {
    Write-Output "No SSH Key file found; generating..."
    & ssh-keygen.exe -f $sshKeyFile -P '""'
}
## Something like:
## 2048 SHA256:bESwmiIWfUrNiON9pbJoQxsRDtR5gto2Ol42JXMM3SE ebekker@ezshield@ezs-001388 (RSA)
Write-Output "Getting SSH Key file fingerprint..."
if ((& ssh-keygen.exe -l -f $sshKeyFile) -imatch "(SHA256:[^ ]+)") {
    $sshKeyHash = $Matches[1]
    $sshKeyHash
}
else {
    Write-Error "Failed to generate SSH Key File or Extract Fingerprint"
    return
}

$r53Zone = Get-R53HostedZones -ProfileName acmesharp-tests |
        Where-Object { $_.Name -eq 'zyborg.io.' }
if (-not $r53Zone.Id) {
    Write-Error "Failed to resolve R53 Zone ID"
    return
}
foreach ($dn in $dnsNames) {
    $rrchg = [Amazon.Route53.Model.Change]@{
        Action = "UPSERT"
        ResourceRecordSet = [Amazon.Route53.Model.ResourceRecordSet]@{
            Name = $dn
            Type = "A"
            TTL = 60
            ResourceRecords = [Amazon.Route53.Model.ResourceRecord]@{
                Value = $relayIpAddress
            }
        }
    }

    ## Create the A record
    Edit-R53ResourceRecordSet -HostedZoneId $r53Zone.Id -ChangeBatch_Change $rrchg -ProfileName acmesharp-tests

    $rrchg.ResourceRecordSet.Type = "TXT"
    $rrchg.ResourceRecordSet.ResourceRecords[0].Value = "`"authkeyfp=$sshKeyHash`""
    ## Create the TXT record with the SSH key
    Edit-R53ResourceRecordSet -HostedZoneId $r53Zone.Id -ChangeBatch_Change $rrchg -ProfileName acmesharp-tests
}


$sshJob = Start-Job {
    $sshArgs = New-Object System.Collections.ArrayList
    $relayOpts = $using:relaySshOptions
    $addDnsTxt = $using:relayAddDnsTxt

    $sshArgs.Add("-o") | Out-Null
    $sshArgs.Add("StrictHostKeyChecking=no") | Out-Null
    foreach ($dn in $using:dnsNames) {
        if ($addDnsTxt) {
            $sshArgs.Add("-R") | Out-Null
            $sshArgs.Add("$($dn):80:localhost:5000") | Out-Null
            $sshArgs.Add("-R") | Out-Null
            $sshArgs.Add("$($dn):443:localhost:5001") | Out-Null
        }
        else {
            $sshArgs.Add("-R") | Out-Null
            $sshArgs.Add("80:localhost:5000") | Out-Null
            $sshArgs.Add("-R") | Out-Null
            $sshArgs.Add("443:localhost:5001") | Out-Null
        }
    }
    #& ssh @relayOpts @sshArgs $using:relayDnsName
    & ssh @relayOpts @sshArgs $using:relayDnsName 2>&1
}
Write-Output "Started Job:"
$sshJob

Start-Sleep -Seconds 5
$sshOut = Receive-Job $sshJob
$sshOutJoined = $sshOut -join "`r`n"

Write-Output "SSH Sent----------------"
Write-Output $sshOutJoined
Write-Output "------------------------"

#Forwarding TCP connections from serveo.net:40074

if ($relayPatternMatch) {
    if ($sshOutJoined -imatch $fwdTcpRegex) {
        Write-output "Forwarding TCP on ($($Matches[1]))"
        $url = [uri]"https://$($Matches[1])"
        Write-Output "HTTPS URL is [$($url)]"
    }
    if ($sshOutJoined -imatch $fwdHttpRegex) {
        Write-output "Forwarding HTTP on ($($Matches[1]))"
        $url = [uri]$Matches[1]
        Write-Output "HTTP Host is [$($url.Host)]"
    }
}

Read-Host -Prompt "Hit Enter to End..." | Out-Null

Write-Output "Stopping and waiting on Job..."
Stop-Process -Name ssh -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Stop-Job $sshJob
Receive-Job $sshJob -Wait
