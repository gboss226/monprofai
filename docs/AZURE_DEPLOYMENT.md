# Déploiement MonProfAI sur Azure

Guide complet pour déployer MonProfAI sur Azure avec CI/CD automatique.

---

## 🎯 Architecture

```
GitHub Repository
    ↓ (git push)
GitHub Actions (CI/CD)
    ├→ Build & Test Backend
    ├→ Deploy API → App Service
    └→ Build & Deploy Frontend → Static Web Apps

App Service (Backend API)
    ↓ HTTPS
PostgreSQL Flexible Server (Database)

Static Web Apps (Frontend)
    ↓ HTTPS
Browser
```

---

## 📋 Prérequis

1. **Azure CLI installé**
   ```bash
   # Windows - via Chocolatey
   choco install azure-cli
   
   # Ou télécharger: https://aka.ms/installazurecliwindows
   ```

2. **Compte Azure** (avec abonnement actif)

3. **GitHub compte** avec le repo `gboss226/monprofai`

---

## 🚀 Déploiement en 3 étapes

### Étape 1: Créer les ressources Azure

**Option A: PowerShell (Windows)**
```powershell
cd C:\Users\GANEMA\RiderProjects\monprofai
.\deploy-azure.ps1
```

**Option B: Bash (Mac/Linux)**
```bash
cd ~/monprofai
chmod +x deploy-azure.sh
./deploy-azure.sh
```

**Option C: Manuellement via Azure Portal**
- Créer Resource Group: `monprofai-rg`
- Créer PostgreSQL Flexible Server
- Créer App Service (B1 Linux, .NET 8)
- Créer Static Web Apps

---

### Étape 2: Configurer GitHub Secrets

#### Secret 1: AZURE_PUBLISH_PROFILE

1. Allez sur **Azure Portal**
2. App Service → `monprofai-api`
3. **Get Publish Profile** (bouton en haut à droite)
4. Sauvegardez le fichier XML

#### Secret 2: AZURE_STATIC_WEB_APPS_API_TOKEN

1. Allez sur **Azure Portal**
2. Static Web Apps → `monprofai-web`
3. **Manage deployment token**
4. Copiez le token

#### Secret 3: AZURE_DATABASE_CONNECTION_STRING

Format:
```
Host=monprofai-db-XXXX.postgres.database.azure.com;Port=5432;Database=monprofai;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;
```

---

### Ajouter les secrets à GitHub

1. Allez sur: https://github.com/gboss226/monprofai/settings/secrets/actions
2. Cliquez **New repository secret**
3. Ajoutez:

| Nom | Valeur |
|-----|--------|
| `AZURE_PUBLISH_PROFILE` | Contenu du fichier XML |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Token Static Web Apps |
| `AZURE_DATABASE_CONNECTION_STRING` | Connection string PostgreSQL |

---

### Étape 3: Déclencher le déploiement

```bash
git push origin main
```

GitHub Actions va automatiquement:
1. ✅ Build le backend (.NET)
2. ✅ Tester le backend
3. ✅ Publier sur App Service
4. ✅ Appliquer les migrations BD
5. ✅ Build le frontend (React)
6. ✅ Publier sur Static Web Apps

---

## 📊 Vérifier le déploiement

### Affichage du statut GitHub Actions
https://github.com/gboss226/monprofai/actions

### URLs en production

| Service | URL |
|---------|-----|
| Frontend | `https://monprofai-web.azurestaticapps.net` |
| API | `https://monprofai-api.azurewebsites.net` |
| API Swagger | `https://monprofai-api.azurewebsites.net/swagger` |
| PostgreSQL | `monprofai-db-XXXX.postgres.database.azure.com` |

---

## 🔐 Configuration de sécurité

### SSL/HTTPS
✅ Automatique sur Azure
- App Service: certificat gratuit Azure
- Static Web Apps: certificat Let's Encrypt
- PostgreSQL: SSL obligatoire

### Firewall PostgreSQL
```bash
# Permettre seulement App Service
az postgres flexible-server firewall-rule create \
  --resource-group monprofai-rg \
  --name monprofai-db-XXXX \
  --rule-name "AllowAppService" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Secrets d'environnement
Les variables sensibles sont dans **App Service → Configuration**:
- `ConnectionStrings__DefaultConnection`
- `JwtSettings__Secret`

---

## 💾 Gestion des données

### Backups PostgreSQL
Azure fait automatiquement:
- ✅ Backup quotidien (7 jours rétention)
- ✅ Geo-redundant storage
- ✅ Restore à un point dans le temps

### Restaurer une sauvegarde
```bash
az postgres flexible-server restore \
  --resource-group monprofai-rg \
  --name monprofai-db-restore \
  --source-server monprofai-db-XXXX \
  --restore-time "2024-12-15T14:30:00Z"
```

---

## 💰 Coûts mensuels estimés

| Service | Tier | Coût/mois |
|---------|------|-----------|
| PostgreSQL | Burstable B1ms | €16 |
| App Service | Basic B1 | €10 |
| Static Web Apps | Free | €0 |
| Bandwidth | Outbound | ~€5 |
| **Total** | | **~€31/mois** |

Pour réduire les coûts:
- Passer à `B1ms` moins cher
- Utiliser `Consumption plan` pour API sans traffic constant
- Archive les old data

---

## 🔧 Dépannage

### Erreur: "Unable to connect to database"

```bash
# Vérifier la connection string
az webapp config appsettings list \
  --resource-group monprofai-rg \
  --name monprofai-api

# Tester la connexion PostgreSQL
psql -h monprofai-db-XXXX.postgres.database.azure.com \
  -U postgres \
  -d monprofai
```

### Erreur: "Static Web Apps deployment failed"

1. Vérifier token `AZURE_STATIC_WEB_APPS_API_TOKEN`
2. Vérifier `app_location` dans le workflow
3. Vérifier que `npm run build` produit le dossier `dist`

### Erreur: "Publish Profile invalid"

1. Retélécharger le profile depuis App Service
2. Remplacer le secret `AZURE_PUBLISH_PROFILE`
3. Retrigger le workflow

---

## 📈 Monitoring & Logs

### Voir les logs API
```bash
az webapp log tail \
  --resource-group monprofai-rg \
  --name monprofai-api
```

### Voir les logs PostgreSQL
```bash
# Via Azure Portal: PostgreSQL → Logs
# Chercher: slow_log, postgresql_log
```

### Application Insights (Monitoring avancé)
```bash
az monitor app-insights component create \
  --app monprofai-insights \
  --location westeurope \
  --resource-group monprofai-rg \
  --application-type web
```

---

## 🚀 Mise à jour en production

Workflow de déploiement:

1. **Développement local**
   ```bash
   git checkout -b feature/new-feature
   # ... développer ...
   git commit -m "feat: new feature"
   git push origin feature/new-feature
   ```

2. **Pull Request & Review**
   ```bash
   # Créer PR sur GitHub
   # Tests automatiques via GitHub Actions
   # Review du code
   ```

3. **Merge vers main**
   ```bash
   # Approuver et merger la PR
   # GitHub Actions déclenche automatiquement le déploiement
   ```

4. **Production**
   - ✅ Nouveau code en production en ~5 minutes
   - ✅ Rollback facile (git revert)

---

## 🔄 Scale Up / Down

### Augmenter la performance

```bash
# App Service: Basic → Standard
az appservice plan update \
  --name monprofai-plan \
  --resource-group monprofai-rg \
  --sku S1

# PostgreSQL: Burstable → General Purpose
# (via Azure Portal, redémarrage nécessaire)
```

### Augmenter la base de données
```bash
# Augmenter le storage (32GB → 64GB)
az postgres flexible-server update \
  --resource-group monprofai-rg \
  --name monprofai-db-XXXX \
  --storage-size 64
```

---

## 🗑️ Supprimer les ressources

**ATTENTION: Ceci supprime tout!**

```bash
az group delete \
  --name monprofai-rg \
  --yes
```

Cela supprime:
- PostgreSQL (données perdues!)
- App Service
- Static Web Apps
- Tous les autres services

---

## 📚 Ressources utiles

- [Azure CLI Documentation](https://learn.microsoft.com/cli/azure/)
- [App Service Pricing](https://azure.microsoft.com/pricing/details/app-service/)
- [PostgreSQL Flexible Server Pricing](https://azure.microsoft.com/pricing/details/postgresql/flexible-server/)
- [Static Web Apps Documentation](https://learn.microsoft.com/azure/static-web-apps/)

---

**Besoin d'aide?** Contactez: cherifganema@gmail.com
