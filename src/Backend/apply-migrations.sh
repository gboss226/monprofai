#!/bin/bash
# Apply EF Core migrations to PostgreSQL

set -e

echo "🔄 Applying database migrations..."
echo ""

cd "$(dirname "$0")"

# Navigate to API project
cd MonProfAI.Api

# Apply migrations
echo "Running: dotnet ef database update"
dotnet ef database update -p ../MonProfAI.Infrastructure

echo ""
echo "✅ Migrations applied successfully!"
echo ""
