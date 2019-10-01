[CmdletBinding()]
param (
    $value
)

$user1 = "tada"
$password1 = "hbytg3majk2cyny7yxuhl3m5ns3zzzguhbwo4jtuulw5vup3vzlq"
$url2 = "https://dev.azure.com/somecompany/tas/_apis/distributedtask/variablegroups/630?api-version=5.1-preview.1"
$pair = "$($user1):$($password1)"
$encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
$basicAuthValue = "Basic $encodedCreds"
$Headers = @{
    Authorization = $basicAuthValue
}

$json = @"
{
"variables": {
  "tralala": {
    "value": "$($value)"
  }
},
"type": "Vsts",
"name": "dennyisgek"    ,
"description": "Updated variable group"
}
"@

Write-host $url2
Invoke-RestMethod -Uri $url2 -Method Put -Body $json -ContentType "application/json" -Headers $Headers