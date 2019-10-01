[CmdletBinding()]
param (
    $userName,
    $url,
    $applicationName,
    $eventQueueStorageConnectionString,
    $logAnalyticsKey,
    $vstsPat,
    $logAnalyticsWorkspace,
    $groupName,
    [hashtable] $variables
)

$pair = "$($userName):$($vstsPat)"
$encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
$basicAuthValue = "Basic $encodedCreds"
$Headers = @{
    Authorization = $basicAuthValue
}

$json = @"
{
"variables": {
 "applicationName": {
     "value": "$($applicationName)"
   },
   "eventQueueStorageConnectionString": {
     "value": "$($eventQueueStorageConnectionString)",
     "isSecret": true
   },
   "logAnalyticsKey": {
     "value": "$($logAnalyticsKey)",
     "isSecret": true
   },
   "vstsPat": {
     "value": "$($vstsPat)",
     "isSecret": true
   },
   "logAnalyticsWorkspace": {
     "value": "$($logAnalyticsWorkspace)",
     "isSecret": true
   }
},
"type": "Vsts",
"name": "$($groupName)",
"description": "Updated variable group"
}
"@
Invoke-RestMethod -Uri $url -Method Put -Body $json -ContentType "application/json" -Headers $Headers