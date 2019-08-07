param (
    [string]$aadDisplayName = "",
    [string]$servicePrincipalDisplayName = "CompliancyCompletenessChecker",
    [string]$subscriptionId = "",
    [string]$loganalyticsResourceGroupName = "",
    [string]$loganalytics = "",
    [string]$scope = "/subscriptions/$( $subscriptionId )/resourceGroups/$( $loganalyticsResourceGroupName )/providers/Microsoft.OperationalInsights/workspaces/$( $loganalytics )"
)



$ErrorActionPreference = "Stop"

Write-Host "Check if there exists an application with $( $aadDisplayName )"
$appReg = Get-AzADApplication -DisplayName $aadDisplayName

Write-Host "Check if there is an exisiting service principal for application with name $( $aadDisplayName ) and ApplicationId $($appReg.ApplicationId)" 
$sp = Get-AzADServicePrincipal -ApplicationId $appReg.ApplicationId
if ($sp -eq $null)
{
    try {
        Write-Host "No Service Principal found for Application with id $( $appReg.ApplicationId ) Create new Service Principal"
        $sp = New-AzADServicePrincipal -DisplayName $servicePrincipalDisplayName -ApplicationId $appReg.ApplicationId -
        Write-Host "Created service principal with id $( $sp.Id )"
    }
    catch
    {
        Write-Error -Message "Creation of Service Principal failed"
    }
}
else
{
    Write-Host "No Service Principal created for application with name $( $aadDisplayName ) and ApplicationId $($appReg.ApplicationId). The SPN exists with id $($sp.Id)"
}

# Create role assignement
#New-AzRoleAssignment -RoleDefinitionName Contributor -ServicePrincipalName $sp.ApplicationId -Scope $scope | Write-Verbose -ErrorAction SilentlyContinue