<#
.DESCRIPTION
  Updates an variable group inside Azure DevOps. WARNING! the variable group first gets cleaned and then all the variables inside the variableHashTable parameter are set in the group.
.PARAMETER variableGroupName
  The name of the variable group.
.PARAMETER variableHashTable
  A hashtable with all variables that are needed in the variable group.
.PARAMETER userName
  Username is needed for the update but can be everything.
.PARAMETER vstsPat
  The private access token.
.PARAMETER collectionName
  The name of the collection where the project is part of.
.PARAMETER projectName
  The name of the project where the variable group stands.
.PARAMETER variableGroupId
  The ID of the variable group can be found in the url inside the library.
.EXAMPLE
  UpdateVariableGroup.ps1 -variableGroupName dennyisgek -variableHashTable @{applicationName = @{value = 'Johntrallala'; isSecret= 'false'}; url = @{ value = 'TestUrl'; isSecret='true'}} -userName UserNameMagLeegZijn -vstsPat hbytg3majk2cyny7yxuhl3m5ns3zzzguhbwo4jtuulw5vup3vzlq -collectionName somecompany -projectName tas -variableGroupId 630
#>
[CmdletBinding()]
Param(
  [Parameter(Mandatory=$true)] $variableGroupName,
  [Parameter(Mandatory=$true)] $variableHashTable,
  [Parameter(Mandatory=$true)] $userName,
  [Parameter(Mandatory=$true)] $vstsPat,
  [Parameter(Mandatory=$true)] $collectionName,
  [Parameter(Mandatory=$true)] $projectName,
  [Parameter(Mandatory=$true)] $variableGroupId
)
$ErrorActionPreference = "Stop"
#[Functions]
<#
.DESCRIPTION
  Build a json that updates the variable group.
.PARAMETER variableGroupName
  The name of the variable group.
.PARAMETER body
  All the variables that are being set.
#>
function BuildJson($variableGroupName, $body)
{
  $jsonBase = @{}
  #De body moet als parameter worden meegegeven is dus een hashtable
  #json opbouwen
  $jsonBase.Add("variables",$body)
  $jsonBase.Add("type",'Vsts')
  $jsonBase.Add("name",$variableGroupName)
  $jsonBase.Add("description",'Update variable group from script')
  return $jsonBase | ConvertTo-Json -Depth 3
}
<#
.DESCRIPTION
  Create a header for the rest api.
.PARAMETER userName
  Username is needed for the update but can be everything.
.PARAMETER vstsPat
  The private access token.
#>
function CreateHeader($userName, $vstsPat)
{
   $pair = "$($userName):$($vstsPat)"
   $encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
   $basicAuthValue = "Basic $encodedCreds"
   $headers = @{
      Authorization = $basicAuthValue
   }
   return $headers
}
<#
.DESCRIPTION
  Create the url based on parameters to update the variable group with the rest api call.
.PARAMETER collectionName
  The name of the collection where the project is part of.
.PARAMETER projectName
  The name of the project where the variable group stands.
.PARAMETER variableGroupId
  The ID of the variable group can be found in the url inside the library.
#>
function CreateUrl($collectionName, $projectName, $variableGroupId)
{
   return "https://dev.azure.com/$($collectionName)/$($projectName)/_apis/distributedtask/variablegroups/$($variableGroupId)?api-version=5.1-preview.1"
}
<#
.DESCRIPTION
  Updates the variable group through the rest api.
.PARAMETER url
  The url that the rest api is using.
.PARAMETER body
  The body of the request.
.PARAMETER header
  The header for authentication.
#>
function UpdateVariableGroup($url, $body, $header)
{
   Invoke-RestMethod -Uri $url -Method Put -Body $body -ContentType "application/json" -Headers $header
}
#[Script]
$variableJson = BuildJson -variableGroupName $variableGroupName -body $variableHashTable
$header = CreateHeader -userName $userName -vstsPat $vstsPat
$url = CreateUrl -collectionName $collectionName -projectName $projectName -variableGroupId $variableGroupId
UpdateVariableGroup -url $url -body $variableJson -header $header