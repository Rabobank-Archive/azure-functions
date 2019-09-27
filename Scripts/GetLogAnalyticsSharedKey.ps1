$ErrorActionPreference = "Stop"

Write-Verbose "Get logAnalytics key for $(loganalytics)" -verbose

$oiws=Get-AzureRmOperationalInsightsWorkspaceSharedKeys -ResourceGroupName $(loganalytics.ResourceGroupName) -Name $(loganalytics)

$foundLogAnalyticsKey=$oiws.PrimarySharedKey

Write-Verbose "Writing found key to logAnalyticsKey variable" -verbose

Write-Host "##vso[task.setvariable variable=logAnalyticsKey]$foundLogAnalyticsKey"