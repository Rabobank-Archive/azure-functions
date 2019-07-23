while getopts n:i:p: option
do
    case "${option}"
        in
        n) AADDISPLAYNAME=${OPTARG};;
        i) AADIDENTIFIERURI=${OPTARG};;
        p) AADPASSWORD=${OPTARG};;
    esac
done

echo "Going to create app registration with display name '$AADDISPLAYNAME' and identifier uri '$AADIDENTIFIERURI'"
az ad app create --display-name "$AADDISPLAYNAME" --available-to-other-tenants false --homepage "http://localhost:3000/login" --oauth2-allow-implicit-flow true --password "$AADPASSWORD" --reply-urls "http://localhost:3000/login" --identifier-uris "$AADIDENTIFIERURI" --end-date "2099-12-31" --required-resource-accesses ./aad-required-permissions-manifest.json