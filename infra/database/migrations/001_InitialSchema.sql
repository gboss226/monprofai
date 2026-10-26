-- Users table
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY NOT NULL,
    "Email" VARCHAR(256) UNIQUE NOT NULL,
    "FullName" VARCHAR(256) NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "Role" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE
);

-- Courses table
CREATE TABLE "Courses" (
    "Id" UUID PRIMARY KEY NOT NULL,
    "Title" VARCHAR(512) NOT NULL,
    "Description" TEXT NOT NULL,
    "TeacherId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    FOREIGN KEY ("TeacherId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- Lessons table
CREATE TABLE "Lessons" (
    "Id" UUID PRIMARY KEY NOT NULL,
    "Title" VARCHAR(512) NOT NULL,
    "Content" TEXT NOT NULL,
    "Order" INTEGER NOT NULL,
    "CourseId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    FOREIGN KEY ("CourseId") REFERENCES "Courses"("Id") ON DELETE CASCADE
);

-- UserCourses table (enrollment)
CREATE TABLE "UserCourses" (
    "Id" UUID PRIMARY KEY NOT NULL,
    "UserId" UUID NOT NULL,
    "CourseId" UUID NOT NULL,
    "Progress" REAL NOT NULL DEFAULT 0.0,
    "EnrolledAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE("UserId", "CourseId"),
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("CourseId") REFERENCES "Courses"("Id") ON DELETE CASCADE
);

-- Indexes
CREATE INDEX "IX_Courses_TeacherId" ON "Courses"("TeacherId");
CREATE INDEX "IX_Lessons_CourseId" ON "Lessons"("CourseId");
CREATE INDEX "IX_UserCourses_UserId" ON "UserCourses"("UserId");
CREATE INDEX "IX_UserCourses_CourseId" ON "UserCourses"("CourseId");
