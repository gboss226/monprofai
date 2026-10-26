#!/bin/bash
# MonProfAI - Azure Deployment Script
# Usage: ./deploy-azure.sh

set -e

echo "🚀 MonProfAI - Azure Deployment"
echo "================================"

# Configuration
RESOURCE_GROUP="monprofai-rg"
LOCATION="westeurope"
DB_SERVER="monprofai-db-$(date +%s | tail -c 5)"
DB_ADMIN="postgres"
DB_PASSWORD="MonProfAI@$(openssl rand -base64 12 | tr -d '=+/' | cut -c1-16)"
APP_SERVICE_PLAN="monprofai-plan"
APP_SERVICE_NAME="monprofai-api"
STATIC_APP_NAME="monprofai-web"

echo "Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  DB Server: $DB_SERVER"
echo "  DB Password: $DB_PASSWORD (sauvegardez-le!)"
echo ""

# 1. Créer Resource Group
echo "1️⃣  Créer Resource Group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# 2. Créer Azure Database for PostgreSQL
echo "2️⃣  Créer PostgreSQL Flexible Server..."
az postgres flexible-server create \
  --resource-group $RESOURCE_GROUP \
  --name $DB_SERVER \
  --admin-user $DB_ADMIN \
  --admin-password "$DB_PASSWORD" \
  --location $LOCATION \
  --sku-name "Standard_B1ms" \
  --tier "Burstable" \
  --storage-size 32 \
  --version 16 \
  --high-availability Disabled \
  --backup-retention 7

# 3. Créer database
echo "3️⃣  Créer base de données..."
az postgres flexible-server db create \
  --resource-group $RESOURCE_GROUP \
  --server-name $DB_SERVER \
  --database-name monprofai

# 4. Configurer firewall (Allow Azure Services)
echo "4️⃣  Configurer firewall..."
az postgres flexible-server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --name $DB_SERVER \
  --rule-name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# 5. Créer App Service Plan
echo "5️⃣  Créer App Service Plan..."
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# 6. Créer App Service (.NET)
echo "6️⃣  Créer App Service (.NET 8)..."
az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --name $APP_SERVICE_NAME \
  --runtime "DOTNET|8.0"

# 7. Configurer les variables d'environnement
echo "7️⃣  Configurer les variables d'environnement..."

CONNECTION_STRING="Host=$DB_SERVER.postgres.database.azure.com;Port=5432;Database=monprofai;Username=$DB_ADMIN;Password=$DB_PASSWORD;SSL Mode=Require;"
JWT_SECRET="$(openssl rand -base64 32)"

az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
    "JwtSettings__Secret=$JWT_SECRET" \
    "JwtSettings__Issuer=MonProfAI" \
    "JwtSettings__Audience=MonProfAI"

# 8. Créer Static Web Apps
echo "8️⃣  Créer Static Web Apps..."
STATIC_APP=$(az staticwebapp create \
  --name $STATIC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --source https://github.com/gboss226/monprofai.git \
  --location $LOCATION \
  --branch main \
  --app-location "src/Frontend" \
  --output-location "dist" \
  --query "id" -o tsv)

echo ""
echo "✅ Déploiement Azure terminé!"
echo ""
echo "📊 Ressources créées:"
echo "  PostgreSQL: $DB_SERVER.postgres.database.azure.com"
echo "  App Service (API): https://$APP_SERVICE_NAME.azurewebsites.net"
echo "  Static Web Apps: https://$STATIC_APP_NAME.azurestaticapps.net"
echo ""
echo "🔐 Secrets générés (À SAUVEGARDER):"
echo "  DB Password: $DB_PASSWORD"
echo "  JWT Secret: $JWT_SECRET"
echo ""
echo "⚙️  Prochaines étapes:"
echo "  1. Créer un Personal Access Token GitHub (https://github.com/settings/tokens)"
echo "  2. Ajouter les secrets GitHub:"
echo "     - AZURE_PUBLISH_PROFILE (télécharger depuis App Service)"
echo "     - AZURE_STATIC_WEB_APPS_TOKEN"
echo "  3. Pusher du code pour déclencher le déploiement CI/CD"
echo ""
