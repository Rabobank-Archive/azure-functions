$ErrorActionPreference = "Stop"

Write-Verbose "Trying to get workspace id" -verbose

$oms = Get-AzureRmOperationalInsightsWorkspace -Name "$(loganalytics)" -ResourceGroupName "$(loganalytics.ResourceGroupName)"
$logAnalyticsWorkspaceID = $oms.CustomerId

Write-Verbose "Workspace is $logAnalyticsWorkspaceID" -verbose
Write-Verbose "Setting workspaceId $logAnalyticsWorkspaceID to variable logAnalyticsWorkspace" -verbose

Write-Host "##vso[task.setvariable variable=logAnalyticsWorkspace]$logAnalyticsWorkspaceID"