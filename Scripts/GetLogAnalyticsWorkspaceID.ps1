$ErrorActionPreference = "Stop"

Write-Verbose "Trying to get workspace id" -verbose

$oms = Get-AzureRmOperationalInsightsWorkspace -Name $($loganalytics) -ResourceGroupName $($loganalytics.ResourceGroupName)
$logAnalyticsWorkspaceID = $oms.CustomerId

Write-Host "Workspace id: $($logAnalyticsWorkspaceID)"
Write-Host "Setting workspaceId $($logAnalyticsWorkspaceID) to variable logAnalyticsWorkspace"

Write-Host "##vso[task.setvariable variable=logAnalyticsWorkspace]$logAnalyticsWorkspaceID"