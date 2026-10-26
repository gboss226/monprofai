# Getting Started with MonProfAI

Bienvenue! Ce guide vous aidera à démarrer le développement sur MonProfAI.

## ✅ Prérequis

Avant de commencer, installez:

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download)**
   - Vérification: `dotnet --version` (doit afficher 9.0.x)

2. **[Node.js 20+](https://nodejs.org/)**
   - Vérification: `node --version` et `npm --version`

3. **[PostgreSQL 16](https://www.postgresql.org/download/)**
   - Vérification: `psql --version`
   - Alternative: Utilisez Docker (voir plus bas)

4. **[Visual Studio Code](https://code.visualstudio.com/)** ou **[JetBrains Rider](https://www.jetbrains.com/rider/)**

## 🚀 Démarrage rapide (5 minutes)

### Option 1: Docker (Recommandé)

Le plus simple, tout est containerisé.

```bash
# 1. Cloner et entrer dans le dossier
cd C:\Users\GANEMA\RiderProjects\monprofai

# 2. Démarrer les services
docker-compose -f infra/docker/docker-compose.yml up -d

# 3. Installer les dépendances frontend
cd src/Frontend
npm install

# 4. Lancer le frontend (dans un nouveau terminal)
npm run dev

# Frontend: http://localhost:3000
# API: http://localhost:5000
# PostgreSQL: localhost:5432
```

Arrêter les services:
```bash
docker-compose -f infra/docker/docker-compose.yml down
```

### Option 2: Installation locale

#### Backend Setup

```bash
# 1. Entrer dans le dossier
cd C:\Users\GANEMA\RiderProjects\monprofai

# 2. Restaurer les dépendances NuGet
dotnet restore src/Backend/MonProfAI.Api/MonProfAI.Api.csproj

# 3. Vérifier la connexion à PostgreSQL
# Assurez-vous que PostgreSQL est démarré
# Valeurs par défaut dans appsettings.json:
#   - Host: localhost
#   - Port: 5432
#   - Database: monprofai
#   - Username: postgres
#   - Password: postgres

# 4. Créer la base de données et appliquer les migrations
cd src/Backend/MonProfAI.Api
dotnet run --launch-profile https
```

L'API démarre sur `https://localhost:5001` et `http://localhost:5000`

✅ Vous verrez Swagger UI sur `http://localhost:5000/swagger`

#### Frontend Setup

```bash
# 1. Accéder au dossier frontend
cd src/Frontend

# 2. Installer les dépendances
npm install

# 3. Créer fichier .env (copie de .env.example)
copy .env.example .env

# 4. Lancer le serveur de développement
npm run dev
```

L'app démarre sur `http://localhost:3000`

## 🎯 Premier test - Flux d'authentification

1. Ouvrez `http://localhost:3000`
2. Cliquez sur "S'inscrire"
3. Remplissez le formulaire:
   - Email: `test@example.com`
   - Nom: `Test User`
   - Mot de passe: `Password123!`
4. Cliquez "S'inscrire"
5. Vous êtes redirigé vers Login
6. Entrez vos identifiants et connectez-vous
7. Vous arrivez sur le Dashboard ✅

## 📂 Structure du projet à connaître

```
monprofai/
├── src/Backend/           # Code .NET C#
│   ├── MonProfAI.Api/     # Points d'entrée HTTP ← MODIFIEZ ICI pour API
│   ├── MonProfAI.Application/  # Logique métier ← MODIFIEZ ICI pour features
│   ├── MonProfAI.Infrastructure/ # Données & services
│   └── MonProfAI.Domain/  # Entités de domaine
│
├── src/Frontend/          # Code React TypeScript
│   ├── src/pages/         # Pages (Login, Register, Dashboard) ← MODIFIEZ ICI pour UI
│   ├── src/components/    # Composants réutilisables
│   ├── src/store/         # État global (Zustand)
│   └── src/api/           # Appels API
│
├── infra/docker/          # Configuration Docker
└── infra/database/        # Migrations SQL
```

## 🛠️ Commandes utiles

### Backend

```bash
cd src/Backend/MonProfAI.Api

# Lancer en développement
dotnet run

# Lancer avec logs détaillés
dotnet run --verbosity detailed

# Build seulement
dotnet build

# Créer une migration EF Core
dotnet ef migrations add MyMigrationName -p ../MonProfAI.Infrastructure

# Appliquer les migrations
dotnet ef database update -p ../MonProfAI.Infrastructure

# Voir l'SQL généré
dotnet ef migrations script -p ../MonProfAI.Infrastructure
```

### Frontend

```bash
cd src/Frontend

# Développement (avec hot reload)
npm run dev

# Build pour production
npm run build

# Preview du build
npm run preview

# Linter TypeScript
npx tsc --noEmit

# Format code
npm run format
```

### Docker

```bash
cd infra/docker

# Démarrer tout
docker-compose up -d

# Voir les logs
docker-compose logs -f

# Arrêter tout
docker-compose down

# Arrêter et supprimer données
docker-compose down -v
```

## 🐛 Troubleshooting

### Backend - Erreur de connexion PostgreSQL
```
Error: Unable to connect to database
```
**Solution:**
1. Vérifiez que PostgreSQL est démarré
2. Vérifiez les credentials dans `appsettings.json`
3. Créez la BD manuelle: `createdb -U postgres monprofai`

### Frontend - Erreur CORS
```
Access to XMLHttpRequest blocked by CORS
```
**Solution:**
- Vérifiez que le backend tourne sur `http://localhost:5000`
- Vérifiez que CORS est activé dans `Program.cs`

### Port déjà utilisé
```
Address already in use
```
**Solution:**
```bash
# Trouver le processus qui utilise le port
netstat -ano | findstr :5000

# Tuer le processus
taskkill /PID <PID> /F
```

## 📝 Premier changement

### Ajouter un nouveau champ à User

1. **Backend - Domain layer** (`MonProfAI.Domain/Entities/User.cs`)
   ```csharp
   public string PhoneNumber { get; set; } = null!;
   ```

2. **Backend - Database migration**
   ```bash
   dotnet ef migrations add AddPhoneNumberToUser -p ../MonProfAI.Infrastructure
   dotnet ef database update
   ```

3. **Frontend - API types** (`src/types/auth.ts`)
   ```typescript
   export interface User {
     id: string
     email: string
     fullName: string
     phoneNumber?: string  // Nouveau
   }
   ```

4. **Frontend - Form** (`src/pages/Register.tsx`)
   ```tsx
   // Ajouter input pour phoneNumber
   ```

## 🔑 Variables d'environnement

### Backend (`src/Backend/MonProfAI.Api/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=monprofai;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-at-least-32-chars", // ⚠️ CHANGEZ EN PRODUCTION
    "Issuer": "MonProfAI",
    "Audience": "MonProfAI"
  }
}
```

### Frontend (`src/Frontend/.env`)
```
VITE_API_URL=http://localhost:5000/api
```

## 📚 Ressources

- **Architecture:** Lire `docs/ARCHITECTURE.md`
- **API Docs:** `http://localhost:5000/swagger` (après démarrage)
- **Code style:** Suivez les patterns existants
- **Questions?** Contactez: cherifganema@gmail.com

## ✅ Checklist - Prêt pour développer?

- [ ] .NET SDK 9 installé
- [ ] Node.js 20+ installé
- [ ] PostgreSQL 16 installé ou Docker disponible
- [ ] Code clloné dans `C:\Users\GANEMA\RiderProjects\monprofai`
- [ ] Backend démarre sans erreurs
- [ ] Frontend démarre sans erreurs
- [ ] Vous pouvez vous inscrire et vous connecter
- [ ] Dashboard s'affiche après connexion

Vous êtes prêt? Commencez à coder! 🚀

## Prochaines étapes

1. Lire `docs/ARCHITECTURE.md` pour comprendre la structure
2. Ajouter une nouvelle fonctionnalité (voir `CLAUDE.md` → "Adding a new API endpoint")
3. Écrire des tests
4. Commit et push vers GitHub
5. Ouvrir une Pull Request

Bienvenue dans MonProfAI! 👋
