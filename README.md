# MonProfAI - Plateforme d'apprentissage pour étudiants

Une plateforme web moderne pour accompagner les étudiants dans leur apprentissage avec une architecture clean et scalable.

## 🎯 Caractéristiques

- ✅ Authentification JWT sécurisée
- ✅ Architecture Clean Architecture (Backend)
- ✅ Frontend React moderne avec TypeScript
- ✅ Base de données PostgreSQL
- ✅ API REST avec ASP.NET Core
- ✅ Docker pour le développement
- ✅ CI/CD prêt (GitHub Actions)

## 🚀 Démarrage rapide

### Prérequis

- **.NET 8.0** - [Télécharger](https://dotnet.microsoft.com/en-us/download)
- **Node.js 20+** - [Télécharger](https://nodejs.org/)
- **Docker & Docker Compose** (optionnel) - [Télécharger](https://www.docker.com/)
- **PostgreSQL 16** (si pas Docker)

### Installation locale

#### 1. Backend

```bash
# Accéder au répertoire du projet
cd C:\Users\GANEMA\RiderProjects\monprofai

# Restaurer les dépendances
dotnet restore src/Backend/MonProfAI.Api/MonProfAI.Api.csproj

# Créer la base de données
# Assurez-vous que PostgreSQL est en cours d'exécution
# Puis exécutez les migrations
dotnet ef database update -p src/Backend/MonProfAI.Infrastructure -s src/Backend/MonProfAI.Api

# Lancer l'API
cd src/Backend/MonProfAI.Api
dotnet run
```

L'API sera disponible sur `http://localhost:5000`

#### 2. Frontend

```bash
# Accéder au répertoire du frontend
cd src/Frontend

# Installer les dépendances
npm install

# Lancer le serveur de développement
npm run dev
```

L'application sera disponible sur `http://localhost:3000`

### Utilisation avec Docker

```bash
# Démarrer tous les services
docker-compose -f infra/docker/docker-compose.yml up -d

# Les services seront disponibles sur:
# - API: http://localhost:5000
# - Frontend: http://localhost:3000
# - PostgreSQL: localhost:5432

# Arrêter les services
docker-compose -f infra/docker/docker-compose.yml down
```

## 📁 Structure du projet

```
monprofai/
├── src/
│   ├── Backend/                      # API ASP.NET Core
│   │   ├── MonProfAI.Api/           # Point d'entrée de l'API
│   │   ├── MonProfAI.Application/   # Logique métier (CQRS/MediatR)
│   │   ├── MonProfAI.Domain/        # Modèles de domaine
│   │   └── MonProfAI.Infrastructure/ # Accès données, services
│   │
│   └── Frontend/                     # Application React
│       ├── src/
│       │   ├── api/                 # Clients API
│       │   ├── components/          # Composants réutilisables
│       │   ├── pages/               # Pages
│       │   ├── store/               # State management (Zustand)
│       │   └── types/               # Types TypeScript
│       └── index.html
│
├── infra/
│   ├── docker/                       # Configuration Docker
│   └── database/
│       └── migrations/              # Scripts SQL
│
├── .github/
│   └── workflows/                    # CI/CD
│
└── docs/                             # Documentation
```

## 🏗️ Architecture

### Backend - Clean Architecture

```
Domain Layer
    ↓
Application Layer (CQRS/MediatR)
    ↓
Infrastructure Layer (EF Core, JWT, Repositories)
    ↓
API Layer (REST Controllers)
```

**Avantages:**
- Séparation des responsabilités
- Facile à tester
- Scalable et maintenable

### Frontend - React + TypeScript

**Stack:**
- Vite (bundler rapide)
- React Router (navigation)
- Zustand (state management)
- TailwindCSS (styling)
- Axios (HTTP client)

## 🔐 Authentification

### Flux d'authentification

1. **Inscription** - Créer un compte avec email/mot de passe
2. **Login** - Obtenir access token + refresh token
3. **Requêtes authentifiées** - Envoyer token dans header Authorization
4. **Refresh** - Utiliser refresh token pour obtenir nouveau access token

### Tokens JWT

- **Access Token** - Valide 1 heure
- **Refresh Token** - Valide 7 jours

## 📦 Dépendances principales

### Backend
- `MediatR` - Pattern CQRS
- `FluentValidation` - Validation
- `Entity Framework Core` - ORM
- `JWT Bearer` - Authentification
- `BCrypt` - Hachage de mots de passe

### Frontend
- `React 18` - Framework UI
- `React Router` - Navigation
- `Zustand` - State management
- `TailwindCSS` - Styling
- `Axios` - HTTP client
- `Vite` - Bundler

## 🧪 Tests

```bash
# Tests backend
cd src/Backend
dotnet test

# Tests frontend
cd src/Frontend
npm test
```

## 📊 Base de données

PostgreSQL avec EF Core migrations.

### Schéma principal

- **Users** - Utilisateurs (étudiants, professeurs, admins)
- **Courses** - Cours
- **Lessons** - Leçons/contenus
- **UserCourses** - Inscriptions et progression

## 🚢 Déploiement

### Production

1. **Build Docker:**
```bash
docker build -f infra/docker/Dockerfile.api -t monprofai-api:latest .
```

2. **Variables d'environnement requises:**
- `ConnectionStrings__DefaultConnection`
- `JwtSettings__Secret` (changez cette valeur!)
- `ASPNETCORE_ENVIRONMENT`

3. **Déploiement:**
- Heroku, Railway, Azure, AWS, etc.

## 📝 Prochaines étapes

1. [ ] Ajouter les tests unitaires
2. [ ] Implémenter système de cours/leçons
3. [ ] Ajouter système de quiz/évaluations
4. [ ] Intégration avec IA pour recommandations
5. [ ] Dashboard professeur
6. [ ] Système de notifications

## 🤝 Contribution

Créez une branche, faites vos modifications, et créez une pull request.

## 📄 Licence

MIT

## 📧 Support

Pour toute question: cherifganema@gmail.com
