while getopts n:i:p:s:r:w: option
do
    case "${option}"
        in
        n) AADDISPLAYNAME=${OPTARG};;
        i) AADIDENTIFIERURI=${OPTARG};;
        p) AADPASSWORD=${OPTARG};;
        s) SUBSCRIPTIONID=${OPTARG};;
        r) LOGANALYTICSRESOURCEGROUP=${OPTARG};;
        w) LOGANALYTICSWORKSPACENAME=${OPTARG};;
    esac
done

echo "Going to create app registration with display name '$AADDISPLAYNAME' and identifier uri '$AADIDENTIFIERURI'"
APPREGISTRATIONID=`az ad app create --display-name "$AADDISPLAYNAME" --available-to-other-tenants false --homepage "http://localhost:3000/login" --oauth2-allow-implicit-flow true --password "$AADPASSWORD" --reply-urls "http://localhost:3000/login" --identifier-uris "$AADIDENTIFIERURI" --end-date "2099-12-31" --required-resource-accesses ./aad-required-permissions-manifest.json | jq -r .appId`
echo "Created app registration with appId '$APPREGISTRATIONID'"

## We need to have an SPN before we can assign a role to the app registration
echo "Checking if there is already a SPN association with this app registration"
SPNSHOWRESULT=`az ad sp show --id $APPREGISTRATIONID 2>/dev/null`

## Did we find an existing SPN?
if [ $? -eq 0 ]
then
    SPNOBJECTID=`echo $SPNSHOWRESULT | jq -r .objectId`
    echo "Found existing SPN with id '$SPNOBJECTID', using that"
else
    echo "SPN not found. Going to create service principal for app registration"
    SPNOBJECTID=`az ad sp create --id $APPREGISTRATIONID | jq -r .objectId`
    echo "Created SPN with id '$SPNOBJECTID'"
    echo "Waiting 10 seconds for SPN to become ready."
    sleep 10
fi

echo "Going to assign contributor role to SPN id '$SPNOBJECTID' in subscription '$SUBSCRIPTIONID', resource group '$LOGANALYTICSRESOURCEGROUP' and workspace '$LOGANALYTICSWORKSPACENAME'"
az role assignment create --role contributor --assignee $SPNOBJECTID --scope /subscriptions/$SUBSCRIPTIONID/resourceGroups/$LOGANALYTICSRESOURCEGROUP/providers/Microsoft.OperationalInsights/workspaces/$LOGANALYTICSWORKSPACENAME > /dev/null
[ $? -eq 0 ] && echo "Succesfully assigned role"