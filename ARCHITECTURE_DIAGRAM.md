# MonProfAI Architecture Diagrams

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        FRONTEND LAYER                           │
│                                                                  │
│  Browser Client (React 18 + TypeScript)                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │  Login Page  │  │ Register Pg  │  │  Dashboard   │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│         │                 │                  │                  │
│         └─────────────────┴──────────────────┘                  │
│                        │                                        │
│         ┌──────────────────────────────────┐                    │
│         │      Zustand Auth Store          │                    │
│         │  (Global State Management)       │                    │
│         └──────────────────────────────────┘                    │
│                        │                                        │
│         ┌──────────────────────────────────┐                    │
│         │    Axios HTTP Client             │                    │
│         │ (JWT Interceptors, Error Handle) │                    │
│         └──────────────────────────────────┘                    │
└────────────────────────┬──────────────────────────────────────┘
                         │
                  HTTP/REST (JSON)
                         │
┌────────────────────────▼──────────────────────────────────────┐
│                      API LAYER                                │
│              (ASP.NET Core 9)                                 │
│                                                               │
│  ┌────────────────────────────────────┐                      │
│  │   Controllers (API Endpoints)      │                      │
│  │  ┌────────────────────────────┐   │                      │
│  │  │  AuthController            │   │                      │
│  │  │  GET  /api/auth/profile    │   │                      │
│  │  │  POST /api/auth/register   │   │                      │
│  │  │  POST /api/auth/login      │   │                      │
│  │  │  POST /api/auth/refresh    │   │                      │
│  │  └────────────────────────────┘   │                      │
│  │  ┌────────────────────────────┐   │                      │
│  │  │  CoursesController (Future)│   │                      │
│  │  └────────────────────────────┘   │                      │
│  └────────────────────────────────────┘                      │
│                      │                                        │
│         ┌────────────▼────────────┐                          │
│         │   Middleware Pipeline   │                          │
│         │  ┌────────────────────┐ │                          │
│         │  │ CORS               │ │                          │
│         │  │ Authentication     │ │                          │
│         │  │ Error Handling     │ │                          │
│         │  └────────────────────┘ │                          │
│         └────────────────────────────┘                        │
└────────────────────────┬──────────────────────────────────────┘
                         │
                         │
┌────────────────────────▼──────────────────────────────────────┐
│               APPLICATION LAYER (CQRS)                        │
│                                                               │
│  ┌──────────────────────────────────┐                        │
│  │       Commands & Queries         │                        │
│  │  ┌────────────────────────────┐ │                        │
│  │  │ RegisterUserCommand        │ │                        │
│  │  │ LoginUserCommand           │ │                        │
│  │  │ GetUserQuery (future)      │ │                        │
│  │  └────────────────────────────┘ │                        │
│  └──────────────────────────────────┘                        │
│                      │                                        │
│  ┌──────────────────▼──────────────────┐                    │
│  │      MediatR Command Handlers       │                    │
│  │  ┌────────────────────────────────┐ │                    │
│  │  │ RegisterUserCommandHandler     │ │                    │
│  │  │ LoginUserCommandHandler        │ │                    │
│  │  └────────────────────────────────┘ │                    │
│  └──────────────────┬───────────────────┘                    │
│                     │                                        │
│  ┌──────────────────▼──────────────────┐                    │
│  │      Application Services          │                    │
│  │  ┌────────────────────────────────┐ │                    │
│  │  │ IUserRepository                │ │                    │
│  │  │ IAuthService                   │ │                    │
│  │  └────────────────────────────────┘ │                    │
│  └──────────────────────────────────────┘                    │
└────────────────────────┬──────────────────────────────────────┘
                         │
┌────────────────────────▼──────────────────────────────────────┐
│               INFRASTRUCTURE LAYER                            │
│                                                               │
│  ┌─────────────────────────────┐  ┌──────────────────────┐  │
│  │   Repositories              │  │   Services           │  │
│  │  ┌────────────────────────┐ │  │  ┌────────────────┐  │  │
│  │  │ UserRepository         │ │  │  │ AuthService    │  │  │
│  │  │  - GetByIdAsync        │ │  │  │  - JWT Gen     │  │  │
│  │  │  - GetByEmailAsync     │ │  │  │  - HashPwd     │  │  │
│  │  │  - CreateAsync         │ │  │  │  - VerifyPwd   │  │  │
│  │  └────────────────────────┘ │  │  └────────────────┘  │  │
│  └─────────────────────────────┘  └──────────────────────┘  │
│                      │                                        │
│         ┌────────────▼────────────┐                          │
│         │   Entity Framework      │                          │
│         │   DbContext             │                          │
│         │                         │                          │
│         │  DbSet<User>            │                          │
│         │  DbSet<Course>          │                          │
│         │  DbSet<Lesson>          │                          │
│         │  DbSet<UserCourse>      │                          │
│         └────────────▲────────────┘                          │
│                      │                                        │
└──────────────────────┼─────────────────────────────────────────┘
                       │
                       │ (SQL Queries)
                       │
┌──────────────────────▼─────────────────────────────────────────┐
│                   DATA LAYER                                  │
│                                                               │
│  PostgreSQL 16 Database                                      │
│                                                               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│  │   Users     │ │   Courses   │ │   Lessons   │            │
│  ├─────────────┤ ├─────────────┤ ├─────────────┤            │
│  │ Id (PK)     │ │ Id (PK)     │ │ Id (PK)     │            │
│  │ Email       │ │ Title       │ │ Title       │            │
│  │ FullName    │ │ Description │ │ Content     │            │
│  │ PasswordHash│ │ TeacherId   │ │ CourseId    │            │
│  │ Role        │ │ (FK)        │ │ (FK)        │            │
│  │ IsActive    │ └─────────────┘ └─────────────┘            │
│  │ CreatedAt   │                                             │
│  │ UpdatedAt   │  ┌──────────────────┐                      │
│  └─────────────┘  │  UserCourses     │                      │
│                   ├──────────────────┤                      │
│                   │ Id               │                      │
│                   │ UserId (FK)      │                      │
│                   │ CourseId (FK)    │                      │
│                   │ Progress         │                      │
│                   │ EnrolledAt       │                      │
│                   └──────────────────┘                      │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

## Request Flow: Login

```
1. USER INTERACTION
   ┌────────────────┐
   │ Click "Login"  │
   └────────┬───────┘
            │
2. FRONTEND
   ┌────────▼─────────────────┐
   │ LoginUserCommand          │
   │ {email, password}         │
   └────────┬─────────────────┘
            │
   ┌────────▼─────────────────┐
   │ Axios POST Request        │
   │ Authorization: -          │
   │ POST /api/auth/login      │
   └────────┬─────────────────┘
            │
3. BACKEND - API LAYER
   ┌────────▼──────────────────────────┐
   │ AuthController.Login()            │
   │ Receives LoginUserCommand         │
   └────────┬──────────────────────────┘
            │
4. BACKEND - APPLICATION LAYER
   ┌────────▼──────────────────────────┐
   │ MediatR.Send(LoginUserCommand)    │
   │ Routes to Handler                 │
   └────────┬──────────────────────────┘
            │
   ┌────────▼──────────────────────────┐
   │ LoginUserCommandHandler.Handle()  │
   │ 1. Get user by email              │
   │ 2. Verify password (BCrypt)       │
   │ 3. Check if active                │
   │ 4. Generate JWT tokens            │
   └────────┬──────────────────────────┘
            │
5. BACKEND - INFRASTRUCTURE LAYER
   ┌────────▼──────────────────────────┐
   │ UserRepository.GetByEmailAsync()  │
   │ Queries database                  │
   └────────┬──────────────────────────┘
            │
   ┌────────▼──────────────────────────┐
   │ AuthService.GenerateTokensAsync() │
   │ 1. Create JWT access token        │
   │ 2. Create refresh token           │
   └────────┬──────────────────────────┘
            │
6. RESPONSE
   ┌────────▼──────────────────────────┐
   │ LoginUserResponse (200 OK)        │
   │ {                                 │
   │   accessToken: "jwt...",          │
   │   refreshToken: "jwt...",         │
   │   user: { id, email, fullName }  │
   │ }                                 │
   └────────┬──────────────────────────┘
            │
7. FRONTEND
   ┌────────▼──────────────────────────┐
   │ Save tokens to localStorage       │
   │ Update Zustand auth store         │
   │ Redirect to /dashboard            │
   └────────────────────────────────────┘
```

## Dependency Injection

```
Program.cs
│
├─ DbContext
│  └─ MonProfAIDbContext (PostgreSQL)
│
├─ Repositories
│  └─ IUserRepository
│     └─ UserRepository
│
├─ Services
│  └─ IAuthService
│     └─ AuthService
│
├─ MediatR
│  ├─ RegisterUserCommand + Handler
│  └─ LoginUserCommand + Handler
│
├─ Authentication
│  └─ JWT Bearer (OIDC)
│
└─ CORS
   └─ Allow frontend on localhost:3000
```

## Database Schema

```
Users (1) ──→ (Many) Courses (as Teacher)
      ↓
      ├─→ (Many) UserCourses
      │
      └─→ Courses (Many-to-Many via UserCourses)

Courses (1) ──→ (Many) Lessons

UserCourses (Junction Table)
├─ UserId → Users
├─ CourseId → Courses
└─ Progress (stores enrollment data)
```

## Authentication Sequence Diagram

```
┌────────┐                    ┌──────┐                 ┌────────┐
│Browser │                    │ API  │                 │   DB   │
└───┬────┘                    └──┬───┘                 └───┬────┘
    │                            │                         │
    │ POST /login                │                         │
    ├───────────────────────────►│                         │
    │                            │ SELECT * FROM users    │
    │                            │  WHERE email = ?      │
    │                            ├────────────────────────►│
    │                            │                         │
    │                            │  User record            │
    │                            │◄────────────────────────┤
    │                            │                         │
    │                            │ (Verify password hash)  │
    │                            │ (Generate JWT tokens)   │
    │                            │                         │
    │  LoginResponse             │                         │
    │ (accessToken, user)        │                         │
    │◄───────────────────────────┤                         │
    │                            │                         │
    │ Save token + localStorage  │                         │
    │ Store in Zustand           │                         │
    │ Redirect to /dashboard     │                         │
    │                            │                         │
    │ GET /dashboard             │                         │
    │ + Authorization: Bearer... │                         │
    ├───────────────────────────►│                         │
    │                            │ (Verify JWT)            │
    │                            │ (Extract claims)        │
    │  Dashboard data            │                         │
    │◄───────────────────────────┤                         │
    │                            │                         │
```

## Deployment Architecture (Future)

```
┌────────────────────────────────────────────────────────┐
│          GitHub (Source Control)                       │
│                  │                                     │
│                  ├─ Push code                          │
│                  └─ Trigger CI/CD                      │
└────────────────────────────────────────────────────────┘
                     │
┌────────────────────▼───────────────────────────────────┐
│      GitHub Actions (CI/CD Pipeline)                   │
│   - Build .NET                                         │
│   - Run tests                                          │
│   - Build Docker image                                │
│   - Push to registry                                  │
└────────────────────┬───────────────────────────────────┘
                     │
      ┌──────────────┼──────────────┐
      │              │              │
┌─────▼────┐  ┌──────▼──────┐  ┌───▼─────────┐
│ Docker   │  │  Kubernetes │  │ Cloud       │
│ Registry │  │  Cluster    │  │ Platform    │
│ (DockerHub)│ │  (K8s)      │  │ (Heroku,    │
│ or ECR  │  │             │  │  Azure)     │
└─────────┘  └─────────────┘  └─────────────┘
                  │
      ┌───────────┼──────────┐
      │           │          │
┌─────▼─────┐ ┌──▼──────┐ ┌──▼───────┐
│  Frontend │ │   API   │ │ Database │
│ (Vercel/  │ │ (Node   │ │(PostgreSQL
│ Netlify)  │ │ Service)│ │ Managed) │
└───────────┘ └─────────┘ └──────────┘
```

---

Cette architecture est scalable et maintainable pour les prochaines années de développement! 🚀
