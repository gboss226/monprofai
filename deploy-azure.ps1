# MonProfAI - Azure Deployment Script (PowerShell)
# Usage: .\deploy-azure.ps1

Write-Host "🚀 MonProfAI - Azure Deployment" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

# Configuration
$RESOURCE_GROUP = "monprofai-rg"
$LOCATION = "westeurope"
$DB_SERVER = "monprofai-db-$(Get-Random -Minimum 1000 -Maximum 9999)"
$DB_ADMIN = "postgres"
$DB_PASSWORD = "MonProfAI@$(Get-Random -Minimum 100000 -Maximum 999999)!"
$APP_SERVICE_PLAN = "monprofai-plan"
$APP_SERVICE_NAME = "monprofai-api"
$STATIC_APP_NAME = "monprofai-web"

Write-Host "Configuration:" -ForegroundColor Cyan
Write-Host "  Resource Group: $RESOURCE_GROUP"
Write-Host "  Location: $LOCATION"
Write-Host "  DB Server: $DB_SERVER"
Write-Host "  DB Password: $DB_PASSWORD"
Write-Host ""

# 1. Créer Resource Group
Write-Host "1️⃣  Créer Resource Group..." -ForegroundColor Yellow
az group create `
  --name $RESOURCE_GROUP `
  --location $LOCATION

# 2. Créer Azure Database for PostgreSQL
Write-Host "2️⃣  Créer PostgreSQL Flexible Server..." -ForegroundColor Yellow
az postgres flexible-server create `
  --resource-group $RESOURCE_GROUP `
  --name $DB_SERVER `
  --admin-user $DB_ADMIN `
  --admin-password $DB_PASSWORD `
  --location $LOCATION `
  --sku-name "Standard_B1ms" `
  --tier "Burstable" `
  --storage-size 32 `
  --version 16 `
  --high-availability Disabled `
  --backup-retention 7

# 3. Créer database
Write-Host "3️⃣  Créer base de données..." -ForegroundColor Yellow
az postgres flexible-server db create `
  --resource-group $RESOURCE_GROUP `
  --server-name $DB_SERVER `
  --database-name monprofai

# 4. Configurer firewall
Write-Host "4️⃣  Configurer firewall..." -ForegroundColor Yellow
az postgres flexible-server firewall-rule create `
  --resource-group $RESOURCE_GROUP `
  --name $DB_SERVER `
  --rule-name "AllowAzureServices" `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# 5. Créer App Service Plan
Write-Host "5️⃣  Créer App Service Plan..." -ForegroundColor Yellow
az appservice plan create `
  --name $APP_SERVICE_PLAN `
  --resource-group $RESOURCE_GROUP `
  --sku B1 `
  --is-linux

# 6. Créer App Service
Write-Host "6️⃣  Créer App Service (.NET 8)..." -ForegroundColor Yellow
az webapp create `
  --resource-group $RESOURCE_GROUP `
  --plan $APP_SERVICE_PLAN `
  --name $APP_SERVICE_NAME `
  --runtime "DOTNET|8.0"

# 7. Configurer variables d'environnement
Write-Host "7️⃣  Configurer variables d'environnement..." -ForegroundColor Yellow

$CONNECTION_STRING = "Host=$DB_SERVER.postgres.database.azure.com;Port=5432;Database=monprofai;Username=$DB_ADMIN;Password=$DB_PASSWORD;SSL Mode=Require;"
$JWT_SECRET = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Random).ToString())) -replace '[^a-zA-Z0-9]', ''

az webapp config appsettings set `
  --resource-group $RESOURCE_GROUP `
  --name $APP_SERVICE_NAME `
  --settings `
    ASPNETCORE_ENVIRONMENT=Production `
    "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" `
    "JwtSettings__Secret=$JWT_SECRET" `
    "JwtSettings__Issuer=MonProfAI" `
    "JwtSettings__Audience=MonProfAI"

# 8. Créer Static Web Apps
Write-Host "8️⃣  Créer Static Web Apps..." -ForegroundColor Yellow
$STATIC_APP = az staticwebapp create `
  --name $STATIC_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --source https://github.com/gboss226/monprofai.git `
  --location $LOCATION `
  --branch main `
  --app-location "src/Frontend" `
  --output-location "dist" `
  --query "id" -o tsv

Write-Host ""
Write-Host "✅ Déploiement Azure terminé!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Ressources créées:" -ForegroundColor Cyan
Write-Host "  PostgreSQL: $DB_SERVER.postgres.database.azure.com"
Write-Host "  App Service (API): https://$APP_SERVICE_NAME.azurewebsites.net"
Write-Host "  Static Web Apps: https://$STATIC_APP_NAME.azurestaticapps.net"
Write-Host ""
Write-Host "🔐 Secrets générés (À SAUVEGARDER):" -ForegroundColor Magenta
Write-Host "  DB Password: $DB_PASSWORD"
Write-Host "  JWT Secret: $JWT_SECRET"
Write-Host ""
Write-Host "⚙️  Prochaines étapes:" -ForegroundColor Yellow
Write-Host "  1. Télécharger le profil de publication (App Service → Export profile)"
Write-Host "  2. Ajouter les secrets GitHub:"
Write-Host "     - AZURE_PUBLISH_PROFILE"
Write-Host "  3. Pusher du code pour déclencher le déploiement"
