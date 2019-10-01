[CmdletBinding()]
param (
   $functionsResourceGroupName,
   $eventQueueStorageAccount
)
$ErrorActionPreference = "Stop"

#get storageKey
Write-Host "Get storage key for $(eventQueueStorageAccount)" -verbose
$storageKey1=(Get-AzureRmStorageAccountKey -ResourceGroupName $functionsResourceGroupName -AccountName $eventQueueStorageAccount).Value[0]
Write-Host "Writing key to eventQueueStorageConnectionString variable"
Write-Host "##vso[task.setvariable variable=eventQueueStorageConnectionString;issecret=true]DefaultEndpointsProtocol=https;AccountName=$eventQueueStorageAccount;AccountKey=$storageKey1;EndpointSuffix=core.windows.net"