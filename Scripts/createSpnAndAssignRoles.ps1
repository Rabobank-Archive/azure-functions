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
        $sp = New-AzADServicePrincipal -DisplayName $servicePrincipalDisplayName -ApplicationId $appReg.ApplicationId
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

Write-Host "Check if there is a Roleassignment"
$role = Get-AzRoleAssignment -RoleDefinitionName Contributor -ServicePrincipalName $sp.ApplicationId
$NewRole = $null

if ($role -eq $null) {
    Write-Host "No Roleassignment found. Creating it"

    $Retries = 0;While ($NewRole -eq $null -and $Retries -le 6)
    {
        # Sleep here for a few seconds to allow the service principal application to become active (usually, it will take only a couple of seconds)
        Sleep 15
        New-AzRoleAssignment -RoleDefinitionName Contributor -ServicePrincipalName $sp.ApplicationId -Scope $scope | Write-Verbose -ErrorAction SilentlyContinue
        $NewRole = Get-AzRoleAssignment -ServicePrincipalName $sp.ApplicationId -ErrorAction SilentlyContinue
        $Retries++;
    }
} else {
    Write-Host "Role Assignment was found. No new role is created"
}

