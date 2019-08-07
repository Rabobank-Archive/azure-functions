
@echo "Going to create app registration with display name %1 and identifier uri %2"
az ad app create --display-name %1 --available-to-other-tenants false --homepage "http://localhost:3000/login" --oauth2-allow-implicit-flow true --password %2 --reply-urls "http://localhost:3000/login" --identifier-uris %3 --end-date "2099-12-31" --required-resource-accesses ./aad-required-permissions-manifest.json
@echo "Created app registration with appId"