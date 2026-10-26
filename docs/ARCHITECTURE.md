# Architecture MonProfAI

## Vue d'ensemble

MonProfAI utilise une **architecture en couches (Clean Architecture)** pour le backend et une **architecture composant** pour le frontend.

## Backend - ASP.NET Core

### Couches

#### 1. Domain Layer (`MonProfAI.Domain`)
- **Responsabilité:** Logique métier pure, entités
- **Dépendances:** Aucune (independant)
- **Contient:**
  - Entités: `User`, `Course`, `Lesson`, `UserCourse`
  - Enums: `UserRole`
  - Business logic pure (sans side effects)

#### 2. Application Layer (`MonProfAI.Application`)
- **Responsabilité:** Cas d'usage, orchestration
- **Dépendances:** Domain
- **Contient:**
  - Use Cases (via MediatR):
    - Commands: `RegisterUserCommand`, `LoginUserCommand`
    - Queries: Futures requêtes
  - Handlers: `RegisterUserCommandHandler`, `LoginUserCommandHandler`
  - Interfaces: Contrats pour l'infrastructure
  - DTOs: Transfer objects pour API

#### 3. Infrastructure Layer (`MonProfAI.Infrastructure`)
- **Responsabilité:** Implémentations techniques
- **Dépendances:** Domain, Application
- **Contient:**
  - DbContext: EF Core (`MonProfAIDbContext`)
  - Repositories: `UserRepository`
  - Services: `AuthService` (JWT, password hashing)
  - Configurations: EF Core mappings

#### 4. API Layer (`MonProfAI.Api`)
- **Responsabilité:** Endpoints HTTP, configuration
- **Dépendances:** Application, Infrastructure
- **Contient:**
  - Controllers: `AuthController`
  - Program.cs: Dependency Injection setup
  - Configuration: appsettings.json

### Flux de requête

```
HTTP Request
    ↓
Controller (MonProfAI.Api)
    ↓
MediatR Command/Query (MonProfAI.Application)
    ↓
Handler (MonProfAI.Application)
    ↓
Repository/Service (MonProfAI.Infrastructure)
    ↓
DbContext/External Services
    ↓
HTTP Response
```

### Exemple: Login

```
POST /api/auth/login
  ↓
AuthController.Login()
  ↓
MediatR.Send(LoginUserCommand)
  ↓
LoginUserCommandHandler.Handle()
  ↓
UserRepository.GetByEmailAsync()
  ↓
DbContext.Users.FirstOrDefaultAsync()
  ↓
AuthService.GenerateTokensAsync()
  ↓
JWT Token Generation
  ↓
LoginUserResponse (200 OK)
```

## Frontend - React + TypeScript

### Structure

```
src/
├── api/              # HTTP clients (axios)
├── components/       # Composants réutilisables
├── pages/           # Pages/écrans
├── store/           # State management (Zustand)
├── types/           # Types TypeScript
└── App.tsx          # Composant principal
```

### State Management (Zustand)

**authStore.ts:**
- State: user, accessToken, isAuthenticated
- Actions: login(), logout(), setUser()
- Persistance: localStorage

### Flux de données

```
Component
    ↓
useAuthStore (Zustand)
    ↓
API Client (axios)
    ↓
Backend API
    ↓
Response
    ↓
Update Store
    ↓
Re-render Component
```

## Base de données

### PostgreSQL Schema

**Users**
```sql
- Id (UUID, PK)
- Email (UNIQUE)
- FullName
- PasswordHash
- Role (Student=0, Teacher=1, Admin=2)
- IsActive
- CreatedAt, UpdatedAt
```

**Courses**
```sql
- Id (UUID, PK)
- Title
- Description
- TeacherId (FK → Users)
- CreatedAt, UpdatedAt
```

**Lessons**
```sql
- Id (UUID, PK)
- Title
- Content
- Order
- CourseId (FK → Courses)
- CreatedAt, UpdatedAt
```

**UserCourses** (Enrollment)
```sql
- Id (UUID, PK)
- UserId (FK → Users)
- CourseId (FK → Courses)
- Progress (0-100%)
- EnrolledAt
- UNIQUE(UserId, CourseId)
```

### Relations

```
User (1) ──→ (Many) Courses (as Teacher)
User (Many) ──→ (Many) Courses (through UserCourses)
Course (1) ──→ (Many) Lessons
```

## Authentification & Autorisation

### JWT Flow

```
1. Utilisateur POST /api/auth/login
2. Backend valide credentials
3. Backend génère JWT tokens:
   - Access Token (1h) : accès API
   - Refresh Token (7j) : obtenir nouveau access token
4. Frontend stocke tokens dans localStorage
5. Frontend envoie Access Token dans Authorization header
6. Backend valide JWT signature & expiration
```

### Middleware d'authentification

**Frontend:**
- `ProtectedRoute` wrapper
- Interceptor Axios pour ajouter token
- Redirect vers login si token invalide

**Backend:**
- JWT Bearer authentication scheme
- Claims validation
- Policy-based authorization (future)

## Points d'extension

### 1. Ajouter une nouvelle fonctionnalité

**Exemple:** Créer un cours

1. **Domain:** Créer entité/value object si nouveau
2. **Application:** `CreateCourseCommand` + `CreateCourseCommandHandler`
3. **Infrastructure:** `CourseRepository` + mappings EF Core
4. **API:** `CoursesController` endpoint
5. **Frontend:** Page React + API client

### 2. Pattern CQRS

- **Commands:** Actions qui modifient (Create, Update, Delete)
- **Queries:** Actions qui lisent (Get, List, Search)
- Chaque command/query a un handler

### 3. Validation

- **Frontend:** Validation côté client (UX)
- **Backend:** Validation avec FluentValidation (security)

## Performance & Scalabilité

### Optimisations actuelles

- JWT au lieu de sessions (stateless)
- DbContext scope par requête (EF Core)
- Index sur FK et fields recherchés
- Lazy loading disabled (explicit loading)

### Améliorations futures

- Caching (Redis)
- Pagination
- Search elastique
- Message queue (RabbitMQ/Kafka)
- GraphQL au lieu de REST

## Sécurité

### Implémenté

- Password hashing avec BCrypt
- JWT signature + validation
- CORS configuré
- SQL injection protection (EF Core paramétrisé)

### À implémenter

- Rate limiting
- HTTPS obligatoire
- CSRF protection
- Audit logging
- Data encryption au repos

## Dépendances entre projets

```
MonProfAI.Api
  ├→ MonProfAI.Application
  │   └→ MonProfAI.Domain
  └→ MonProfAI.Infrastructure
      ├→ MonProfAI.Application
      └→ MonProfAI.Domain

Acyclic Dependency Graph ✓
```

## Testing Strategy

### Backend
- Unit tests: Domain, Application (logique pure)
- Integration tests: Repository, Services (avec BD)
- Controller tests: Endpoints (API)

### Frontend
- Unit tests: Composants, hooks, utilities
- Integration tests: User flows
- E2E tests: Scénarios complets (Cypress/Playwright)

## Déploiement

### Development
- `dotnet run` (backend)
- `npm run dev` (frontend)

### Production
- Docker containers
- PostgreSQL managed service
- Frontend static hosting (S3, Vercel, Netlify)
- Backend: App Service, Heroku, Railway, etc.
- CI/CD: GitHub Actions

## Évolution future

1. **Microservices** - Si scaling nécessaire
2. **Event Sourcing** - Pour audit trail
3. **GraphQL** - Plus flexible que REST
4. **Real-time** - WebSockets pour notifications
5. **Mobile** - React Native/Flutter
