[CmdletBinding()]
param (
    $EventQueueStorageAccount,
    $FunctionsResourceGroupName
)

#get storageKey
Write-Host "Get storage key for $(EventQueueStorageAccount)" -verbose

$storageKey1=Get-AzureRmStorageAccountKey -ResourceGroupName $FunctionsResourceGroupName -AccountName $EventQueueStorageAccount.Value[0]

Write-Verbose "Writing key to eventQueueStorageConnectionString variable" -verbose

Write-Host "##vso[task.setvariable variable=eventQueueStorageConnectionString;issecret=true]DefaultEndpointsProtocol=https;AccountName=$EventQueueStorageAccount;AccountKey=$storageKey1;EndpointSuffix=core.windows.net"