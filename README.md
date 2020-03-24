```bash
export azdev_token=
export azdev_organization=
export extension_secret=
export extension_name=azure-devops-compliance
export extension_publisher=riezebosch

export rg=
export name=

az group create --name $rg --location westeurope
az storage account create -g $rg -n $name

function=$(az functionapp create -g $rg -n $name -s $name --consumption-plan-location westeurope --os-type windows --runtime dotnet --functions-version 3 -o tsv --query "id")
az ad sp create-for-rbac --name $name --role contributor --scopes $function --sdk-auth

az functionapp cors add -a https://$extension_publisher.gallerycdn.vsassets.io -n $name -g $rg
az functionapp config appsettings set --name $name --resource-group $rg --settings TOKEN=$azdev_token ORGANIZATION=$azdev_organization
az functionapp config appsettings set --name $name --resource-group $rg --settings EXTENSION_NAME=$extension_name EXTENSION_PUBLISHER=$extension_publisher EXTENSION_SECRET=$extension_secret  

curl https://github.com/azure-devops-compliance/azure-functions/releases/latest/download/release.zip -L --output release.zip
az functionapp deployment source config-zip -g $rg -n $name --src release.zip
```

