# -------------------------------------------------------------------
# createLocalDotenv.ps1
# -------------------------------------------------------------------
# Uses the values from the infrastructure provisioning to create a
# local .env to run examples.
# -------------------------------------------------------------------

$ErrorActionPreference = "Stop"

$filepath = ".\examples\.env"

Write-Host ""
Write-Host ("**Creating local .env for examples (azd hook: postprovision)**" | ConvertFrom-Markdown -AsVT100EncodedString).VT100EncodedString
Write-Host "  - Creating a local .env for running examples at $filepath"

if (Test-Path $filepath) {
    $response = Read-Host "    The local .env already exists. Do you want to override it? (y/N): "
    if ($response -eq "y") { # Case-insensitive
        Write-Host "  - Exiting without modifying the .env"
        Exit 0
    }

    Write-Host "  - Deleting the existing .env"
    Remove-Item -Path $filepath
}

# Create the .env file
$null = New-Item $filepath -ItemType File


Write-Host "  - Adding values from the provisioning output / azd env"

$output = azd env get-values

# Parse the output to get the values we need to run local examples
# and append each matching value to the .env file
foreach ($line in $output) {
    if ($line -match "AZURE_AI_PROJECT_CONNECTION_STRING"){
        $val = ($line -split "=")[1] -replace '"',''
        Add-Content $filepath "AZURE_AI_PROJECT_CONNECTION_STRING=$val"
    }
    if ($line -match "AZURE_AI_AGENTS_SERVICE_HOSTNAME"){
        $val = ($line -split "=")[1] -replace '"',''
        Add-Content $filepath "AZURE_AI_AGENTS_SERVICE_HOSTNAME=$val"
    }
}


Write-Host "  - Adding an AI Agent Service access token"

# Retrieve and access token for AI Agents Service REST API calls
$accessToken = az account get-access-token --resource "https://ml.azure.com/" --query accessToken -o tsv
Add-Content $filepath "AZURE_AI_AGENTS_SERVICE_ACCESS_TOKEN=$accessToken"


Write-Host "  (✓) Done: " -ForegroundColor Green -NoNewLine
Write-Host "Creating local .env for examples"