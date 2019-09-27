[CmdletBinding()]
param (
    $LogAnalyticsName,
    $LogAnalyticsResourceGroupName
)

$ErrorActionPreference = "Stop"

Write-Verbose "Trying to get workspace id" -verbose

$oms = Get-AzureRmOperationalInsightsWorkspace -Name $LogAnalyticsName -ResourceGroupName $LogAnalyticsResourceGroupName
$logAnalyticsWorkspaceID = $oms.CustomerId

Write-Host "Workspace id: $($logAnalyticsWorkspaceID)"
Write-Host "Setting workspaceId $($logAnalyticsWorkspaceID) to variable logAnalyticsWorkspace"

Write-Host "##vso[task.setvariable variable=logAnalyticsWorkspace]$logAnalyticsWorkspaceID"