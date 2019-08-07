$aad-display-name =
$aadIdentifierUri = "http://compliancycheckercompleteness.tas"
$homepageUrl = "http://localhost:3000/login"
$aadPassword = ConvertTo-SecureString "8dz1s7hq4GqUrasJFg0H35%bbM4UTcGM%*IJdU!fRb$%IjfJkBKX$%zpYw8lkWA" -AsPlainText -Force
$servicePrincipalDisplayName = "CompliancyCompletenessChecker"
$subscriptionId = "d65193b5-a500-405c-b6f4-767fb5063969"
$loganalyticsResourceGroupName = "rg-d02-dev-azuredevopslogsdev-01"
$loganalytics = "AzureDevOpsLogsDev"
$scope = "/subscriptions/$( $subscriptionId )/resourceGroups/$( $loganalyticsResourceGroupName )/providers/Microsoft.OperationalInsights/workspaces/$( $loganalytics )"

$ErrorActionPreference = "Stop"

Write-Host "Check if there exists an application with $( $aad-display-name )"
$appReg = Get-AzADApplication -DisplayName $aad-display-name
#if ($appReg -eq $null)
#{
#    try
#    {
#        Write-Host "Going to create app registration with display name $( $aadDisplayName ) and identifier uri $( $aadIdentifierUri )"
#        $appReg = New-AzADApplication -DisplayName $aadDisplayName -Password $aadPassword -IdentifierUris $aadIdentifierUri -Homepage $homepageUrl -ReplyUrls $homepageUrl -AvailableToOtherTenants $false -EndDate "2099-12-31" -RequiredResourceAccess ./aad-required-permissions-manifest.json -ErrorAction Stop
#        Write-Host "Created app registration with appId $( $appReg.ApplicationId )"
#    }
#    catch
#    {
#        Write-Error -Message "Creation of Application fails"
#    }
#
#}
#else
#{
#    Write-Host "Application with name $( $aadDisplayName ) exists, so it won't be created"
#}

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