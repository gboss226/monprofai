# MonProfAI - Project Documentation

## Project Overview

**MonProfAI** est une plateforme d'apprentissage pour étudiants construite avec:
- **Backend:** ASP.NET Core 9 (Clean Architecture)
- **Frontend:** React 18 + TypeScript
- **Database:** PostgreSQL 16
- **Architecture:** Clean Architecture + CQRS (MediatR)

## Stack & Technologies

### Backend
- Framework: ASP.NET Core 8
- Database: PostgreSQL 16 + EF Core 9
- Patterns: Clean Architecture, CQRS (MediatR)
- Authentication: JWT with BCrypt password hashing
- Packages:
  - `MediatR` - CQRS pattern
  - `FluentValidation` - Validation
  - `Entity Framework Core` - ORM
  - `BCrypt.Net-Next` - Password hashing
  - `System.IdentityModel.Tokens.Jwt` - JWT tokens

### Frontend
- Framework: React 18 + TypeScript
- Bundler: Vite
- Styling: TailwindCSS
- State: Zustand
- HTTP: Axios
- Routing: React Router v6
- Build: TypeScript + tsc

### Infrastructure
- Containerization: Docker + Docker Compose
- Database: PostgreSQL in Docker
- CI/CD: GitHub Actions

## Project Structure

```
src/Backend/
  ├── MonProfAI.Domain/              # Business entities (pure C#)
  │   └── Entities/
  │       ├── User.cs               # User entity + roles
  │       ├── Course.cs             # Course entity
  │       ├── Lesson.cs             # Lesson entity
  │       └── UserCourse.cs         # Enrollment entity
  │
  ├── MonProfAI.Application/         # Use cases & orchestration
  │   ├── UseCases/Auth/
  │   │   ├── LoginUserCommand.cs
  │   │   ├── LoginUserCommandHandler.cs
  │   │   ├── RegisterUserCommand.cs
  │   │   └── RegisterUserCommandHandler.cs
  │   └── Interfaces/
  │       ├── IAuthService.cs
  │       └── IUserRepository.cs
  │
  ├── MonProfAI.Infrastructure/      # Technical implementations
  │   ├── Data/
  │   │   └── MonProfAIDbContext.cs  # EF Core DbContext
  │   ├── Services/
  │   │   └── AuthService.cs        # JWT + password hashing
  │   └── Repositories/
  │       └── UserRepository.cs     # Data access
  │
  └── MonProfAI.Api/                 # HTTP API
      ├── Controllers/
      │   └── AuthController.cs
      ├── Program.cs                # DI setup + middleware
      └── appsettings.json

src/Frontend/
  ├── src/
  │   ├── api/
  │   │   └── client.ts             # Axios client with JWT interceptors
  │   ├── components/
  │   │   └── ProtectedRoute.tsx    # Route guard component
  │   ├── pages/
  │   │   ├── Login.tsx
  │   │   ├── Register.tsx
  │   │   └── Dashboard.tsx
  │   ├── store/
  │   │   └── authStore.ts          # Zustand auth state
  │   ├── types/
  │   │   └── auth.ts               # TypeScript interfaces
  │   ├── App.tsx                   # Router setup
  │   └── main.tsx
  ├── index.html
  ├── vite.config.ts
  ├── tsconfig.json
  ├── tailwind.config.js
  └── package.json

infra/
  ├── docker/
  │   ├── docker-compose.yml
  │   └── Dockerfile.api
  └── database/
      └── migrations/
          └── 001_InitialSchema.sql

.github/workflows/
  └── build.yml                      # CI pipeline
```

## Key Architectural Decisions

1. **Clean Architecture** - Pour maintainabilité et testabilité
2. **CQRS with MediatR** - Pour séparation concerns (Commands vs Queries)
3. **Repository Pattern** - Pour abstraction de la persistence
4. **JWT Authentication** - Stateless, scalable
5. **Zustand over Redux** - Plus léger, simpler pour ce projet
6. **TailwindCSS** - Utility-first CSS, faster development
7. **PostgreSQL** - Open source, powerful, free tier available

## Development Workflow

### Backend Development
1. Changes go in the **Domain** layer first (entities)
2. **Application** layer contains use cases (Commands/Queries + Handlers)
3. **Infrastructure** layer implements the interfaces from Application
4. **API** layer exposes endpoints

**Golden rule:** Dependencies flow inward only (API → Infrastructure → Application → Domain)

### Frontend Development
1. Components live in `src/components/`
2. Pages in `src/pages/`
3. State management in `src/store/`
4. API interactions in `src/api/`
5. Types in `src/types/`

## Database Migrations

Currently handled by EF Core migrations (future):
```bash
# Create migration
dotnet ef migrations add MigrationName -p src/Backend/MonProfAI.Infrastructure -s src/Backend/MonProfAI.Api

# Apply migration
dotnet ef database update -p src/Backend/MonProfAI.Infrastructure -s src/Backend/MonProfAI.Api
```

## Authentication Flow

1. **Register:** POST `/api/auth/register` → Create user with hashed password
2. **Login:** POST `/api/auth/login` → Return JWT access + refresh tokens
3. **Request:** Add `Authorization: Bearer <accessToken>` header
4. **Validate:** JWT middleware checks signature & expiration
5. **Refresh:** Use refresh token to get new access token (future)

## Common Tasks

### Adding a new API endpoint
1. Create Command/Query in `MonProfAI.Application/UseCases/`
2. Create Handler for the command/query
3. Add Repository/Service method in `MonProfAI.Infrastructure/`
4. Add Controller action in `MonProfAI.Api/Controllers/`

### Adding a new page
1. Create component in `src/pages/`
2. Add route in `src/App.tsx`
3. Create API client method in `src/api/` if needed
4. Use Zustand store for shared state

### Running locally
```bash
# Backend
cd src/Backend/MonProfAI.Api
dotnet run

# Frontend (another terminal)
cd src/Frontend
npm install
npm run dev
```

### Docker development
```bash
cd infra/docker
docker-compose up -d
```

## Important Files to Know

| File | Purpose |
|------|---------|
| `MonProfAI.Api/Program.cs` | Backend configuration, DI setup |
| `MonProfAI.Infrastructure/Data/MonProfAIDbContext.cs` | Database schema |
| `src/Frontend/src/App.tsx` | Frontend routing |
| `src/Frontend/src/store/authStore.ts` | Global auth state |
| `.github/workflows/build.yml` | CI/CD pipeline |
| `infra/docker/docker-compose.yml` | Local development environment |

## Security Considerations

- ✅ Passwords hashed with BCrypt (never store plain text)
- ✅ JWT tokens signed with secret key
- ✅ CORS configured for frontend only
- ✅ Parameterized queries (EF Core prevents SQL injection)
- ⚠️ TODO: Rate limiting, HTTPS enforcement, CSRF tokens
- ⚠️ TODO: Change JWT secret in production

## Performance Notes

- JWT is stateless (no DB lookup per request)
- LazyLoading disabled in EF Core (explicit loading)
- Database indexes on foreign keys
- Frontend caching via axios interceptors

## Testing Strategy

- Backend: xUnit + Moq (future)
- Frontend: Vitest + React Testing Library (future)
- E2E: Playwright (future)

## Deployment Checklist

- [ ] Change `JwtSettings__Secret` to strong random key
- [ ] Set `ASPNETCORE_ENVIRONMENT` to Production
- [ ] Enable HTTPS
- [ ] Configure database backups
- [ ] Set up monitoring/logging
- [ ] Configure rate limiting
- [ ] Enable CORS only for production frontend domain

## Future Enhancements

1. **Courses Module** - CRUD courses, lessons
2. **Quiz System** - Create/take quizzes
3. **Progress Tracking** - Student progress dashboard
4. **Notifications** - Real-time notifications
5. **AI Integration** - Personalized recommendations
6. **Mobile App** - React Native companion
7. **Teacher Dashboard** - Course management interface
8. **Analytics** - Learning analytics

## Contact

Project lead: cherifganema@gmail.com
