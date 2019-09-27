    [CmdletBinding()]
    param (
        $LogAnalyticsName,
        $LogAnalyticsResourceGroupName
    )

    $ErrorActionPreference = "Stop"

    Write-Host "Trying to get log analytics workspace id"

    $oms = Get-AzureRmOperationalInsightsWorkspace -Name $LogAnalyticsName -ResourceGroupName $LogAnalyticsResourceGroupName
    $logAnalyticsWorkspaceID = $oms.CustomerId

    Write-Host "Workspace id is: $($logAnalyticsWorkspaceID)"

    Write-Host "Get Log Analytics Shared key for workspace $( $logAnalyticsWorkspaceID )"

    $oiws=Get-AzureRmOperationalInsightsWorkspaceSharedKeys -ResourceGroupName $lLogAnalyticsResourceGroupName -Name $LogAnalyticsName

    $foundLogAnalyticsKey=$oiws.PrimarySharedKey

    Write-Host "Setting workspaceId $($logAnalyticsWorkspaceID) to variable logAnalyticsWorkspace"
    Write-Host "##vso[task.setvariable variable=logAnalyticsWorkspace]$logAnalyticsWorkspaceID"
    Write-Host "Settings sharedkey to variable logAnalyticsKey"
    Write-Host "##vso[task.setvariable variable=logAnalyticsKey]$foundLogAnalyticsKey"