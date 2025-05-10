set -e

# -------------------------------------------------------------------
# createLocalDotenv.sh
# -------------------------------------------------------------------
# Uses the values from the infrastructure provisioning to create a
# local .env to run examples.
# -------------------------------------------------------------------

filepath="./examples/.env"

echo ""
echo "\033[1mCreating local .env for examples (azd hook: postprovision)\033[0m"
echo "  - Creating a local .env for running examples at $filepath"

if [ -f $filepath ]; then
    echo -n "    The local .env already exists. Do you want to override it? (y/N): "
    read response
    if [ "$response" != "Y" ] && [ "$response" != "y" ]; then
        echo "  - Exiting without modifying the .env"
        exit 0
    fi
    echo "  - Deleting the existing .env"
    rm $filepath
fi

echo "  - Adding values from the provisioning output / azd env"

output=$(azd env get-values)

# Parse the output to get the values we need to run local examples
# and append each matching value to the .env file
while IFS='=' read -r key value; do
    if [ "$key" = "AZURE_AI_PROJECT_CONNECTION_STRING" ]; then
        value=$(echo "$value" | sed 's/^"//' | sed 's/"$//')
        echo "$key=$value" >> $filepath
    fi
    if [ "$key" = "AZURE_AI_AGENTS_SERVICE_HOSTNAME" ]; then
        value=$(echo "$value" | sed 's/^"//' | sed 's/"$//')
        echo "$key=$value" >> $filepath
    fi
done <<EOF
$(azd env get-values)
EOF

echo "  - Adding an AI Agent Service access token"

# Retrieve an access token for AI Agents Service REST API calls
accessToken=$(az account get-access-token --resource "https://ml.azure.com/" --query accessToken -o tsv)
echo "AZURE_AI_AGENTS_SERVICE_ACCESS_TOKEN=$accessToken" >> $filepath

echo "  \033[0;32m(✓) Done:\033[0m Creating local .env for examples"
