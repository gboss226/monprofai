# MonProfAI - Roadmap & TODO

## Phase 1: MVP - Authentification ✅ (COMPLÈTE)

- [x] Setup Clean Architecture backend
- [x] JWT authentication
- [x] User registration
- [x] User login
- [x] React frontend with auth
- [x] Protected routes
- [x] PostgreSQL database
- [x] Docker setup
- [x] Documentation

## Phase 2: Courses (Prochaine)

### Backend
- [ ] Create `CourseRepository` with CRUD operations
- [ ] Create `GetCoursesQuery` + `GetCoursesQueryHandler`
- [ ] Create `CreateCourseCommand` + `CreateCourseCommandHandler`
- [ ] Create `UpdateCourseCommand` handler
- [ ] Create `DeleteCourseCommand` handler
- [ ] Add `CoursesController` with endpoints:
  - `GET /api/courses` - List all courses
  - `GET /api/courses/{id}` - Get course details
  - `POST /api/courses` - Create (teacher only)
  - `PUT /api/courses/{id}` - Update (teacher only)
  - `DELETE /api/courses/{id}` - Delete (teacher only)
- [ ] Add course enrollment endpoints
  - `POST /api/courses/{id}/enroll` - Student enrolls in course
  - `DELETE /api/courses/{id}/unenroll` - Student leaves course
  - `GET /api/students/courses` - List student's courses

### Frontend
- [ ] Create `src/pages/CoursesPage.tsx`
- [ ] Create `src/pages/CourseDetailPage.tsx`
- [ ] Create `src/pages/CreateCoursePage.tsx` (teacher)
- [ ] Create course cards component
- [ ] Add routes for courses
- [ ] Create API client for courses (`src/api/coursesClient.ts`)
- [ ] Implement course listing with pagination
- [ ] Implement course search/filter

### Database
- [ ] Verify Course and Lesson entities (already created)
- [ ] Create seed data for testing

## Phase 3: Lessons/Content

### Backend
- [ ] Create `LessonRepository`
- [ ] Create lesson CRUD commands and queries
- [ ] Add `LessonsController`
- [ ] Lesson ordering/sequencing

### Frontend
- [ ] Lesson viewer page
- [ ] Lesson editor (for teachers)
- [ ] Lesson progress tracking

## Phase 4: Quiz System

### Backend
- [ ] Create `Quiz` and `QuizQuestion` entities
- [ ] Quiz repository and CRUD
- [ ] Quiz submission handling
- [ ] Auto-grading logic

### Frontend
- [ ] Quiz interface
- [ ] Quiz results display

## Phase 5: Progress Tracking

### Backend
- [ ] Enhance `UserCourse` with progress calculation
- [ ] Create progress update commands
- [ ] Analytics queries

### Frontend
- [ ] Progress dashboard
- [ ] Learning statistics
- [ ] Streak counter

## Phase 6: Notifications

### Backend
- [ ] Create `Notification` entity
- [ ] Notification service
- [ ] Email notifications (SendGrid/SMTP)
- [ ] In-app notifications

### Frontend
- [ ] Notification bell icon
- [ ] Notification dropdown
- [ ] Real-time updates (WebSockets - future)

## Phase 7: Teacher Dashboard

### Backend
- [ ] Teacher-specific endpoints
- [ ] Analytics for teachers
- [ ] Student management

### Frontend
- [ ] Teacher dashboard layout
- [ ] Course analytics
- [ ] Student list view
- [ ] Performance reports

## Phase 8: AI Integration

### Backend
- [ ] OpenAI API integration
- [ ] Prompt engineering for lessons
- [ ] Exercise generation
- [ ] Student personalization

### Frontend
- [ ] AI assistant chat
- [ ] Personalized recommendations

## Phase 9: Mobile App (React Native)

- [ ] Setup React Native project
- [ ] Auth screens
- [ ] Course listing
- [ ] Lesson viewer
- [ ] Push notifications

## Technical Debt & Improvements

### Code Quality
- [ ] Add unit tests (xUnit + Moq)
- [ ] Add integration tests
- [ ] Add E2E tests (Playwright)
- [ ] Code coverage >80%
- [ ] ESLint + Prettier setup for frontend
- [ ] GitHub Actions: auto-format + lint

### Security
- [ ] Rate limiting middleware
- [ ] HTTPS enforcement
- [ ] CSRF protection
- [ ] Input validation improvements
- [ ] SQL injection testing
- [ ] XSS testing
- [ ] Audit logging

### Performance
- [ ] Database query optimization
- [ ] Caching layer (Redis)
- [ ] API response compression
- [ ] Frontend lazy loading
- [ ] Image optimization
- [ ] CDN setup

### Infrastructure
- [ ] Kubernetes deployment config
- [ ] Database backup strategy
- [ ] Monitoring/logging (ELK, Datadog)
- [ ] Error tracking (Sentry)
- [ ] APM (Application Performance Monitoring)

### Frontend Polish
- [ ] Dark mode support
- [ ] Accessibility improvements (a11y)
- [ ] i18n (Internationalization)
- [ ] Loading states
- [ ] Error boundaries
- [ ] Toast notifications

## Current Team

- [ ] Backend Lead: TBD
- [ ] Frontend Lead: TBD
- [ ] DevOps Lead: TBD
- [ ] QA Lead: TBD

## Timeline Estimate

| Phase | Duration | Start | End |
|-------|----------|-------|-----|
| Phase 1 (Auth) | 1 week | ✅ | ✅ |
| Phase 2 (Courses) | 2 weeks | TBD | TBD |
| Phase 3 (Lessons) | 1.5 weeks | TBD | TBD |
| Phase 4 (Quiz) | 2 weeks | TBD | TBD |
| Phase 5 (Progress) | 1 week | TBD | TBD |
| Phase 6 (Notifications) | 1.5 weeks | TBD | TBD |
| Phase 7 (Teacher Dashboard) | 2 weeks | TBD | TBD |
| Phase 8 (AI Integration) | 3 weeks | TBD | TBD |
| Phase 9 (Mobile) | 4 weeks | TBD | TBD |

**Total MVP (Phases 1-5):** ~7.5 weeks

## Known Issues & Limitations

- No refresh token rotation yet
- No email verification
- No password reset
- No 2FA
- No user roles enforcement (teacher vs student)
- No concurrent request handling
- No request logging

## Dependencies to Watch

- Update .NET 9 when patches released
- Update React 18 when 19 stable released
- Monitor Tailwind CSS updates
- Monitor security advisories for dependencies

---

Last Updated: 2026-10-26
Maintained by: Development Team
