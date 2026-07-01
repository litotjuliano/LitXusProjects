#!/bin/bash
# LitXus Systems — build + run the full dev stack, then open the browser.
# macOS-specific: uses a Dockerized SQL Server (LocalDB isn't available on macOS).
# Double-click this file in Finder, or run: ./scripts/run-dev.command

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_DIR="$PROJECT_ROOT/backend"
FRONTEND_DIR="$PROJECT_ROOT/frontend"

SQL_CONTAINER=litxus-dev-sql
SQL_PORT=14330
SQL_PASSWORD='LitXusDev!2026'
API_PORT=5018
FRONTEND_PORT=5173
API_LOG=/tmp/litxus-api.log
FRONTEND_LOG=/tmp/litxus-frontend.log

echo "== LitXus Systems — dev build & run =="
echo

# 1. Docker + SQL Server
if ! docker info >/dev/null 2>&1; then
  echo "Starting Docker Desktop..."
  open -a Docker
  echo "Waiting for Docker daemon..."
  until docker info >/dev/null 2>&1; do sleep 2; done
fi

wait_for_sql() {
  echo "Waiting for SQL Server to accept connections..."
  for i in $(seq 1 30); do
    docker exec "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SQL_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1 && return 0
    sleep 2
  done
  echo "SQL Server did not become ready in time." >&2
  exit 1
}

if [ -z "$(docker ps -aq -f name=^${SQL_CONTAINER}$)" ]; then
  echo "Creating SQL Server container ($SQL_CONTAINER)..."
  docker run -d --name "$SQL_CONTAINER" -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="$SQL_PASSWORD" \
    -p ${SQL_PORT}:1433 mcr.microsoft.com/mssql/server:2022-latest >/dev/null
  wait_for_sql
elif [ -z "$(docker ps -q -f name=^${SQL_CONTAINER}$)" ]; then
  echo "Starting existing SQL Server container..."
  docker start "$SQL_CONTAINER" >/dev/null
  # A restarted container needs the same readiness poll as a fresh one — SQL Server's own
  # listener takes longer to come up than a fixed short sleep reliably covers, which was
  # causing pre-login-handshake connection failures on the very first migration attempt.
  wait_for_sql
else
  echo "SQL Server container already running."
fi

CONNECTION_STRING="Server=localhost,${SQL_PORT};Database=LitXusSystems;User Id=sa;Password=${SQL_PASSWORD};TrustServerCertificate=True"
JWT_SIGNING_KEY="dev-only-signing-key-not-for-production-use-replace-me-1234567890"

# 2. Apply migrations
echo
echo "Applying database migrations..."
(cd "$BACKEND_DIR" && dotnet dotnet-ef database update \
  --project src/LitXus.Infrastructure --startup-project src/LitXus.Api \
  --connection "$CONNECTION_STRING")

# 3. Build backend
echo
echo "Building backend..."
(cd "$BACKEND_DIR" && dotnet build)

# 4. Start backend API
echo
echo "Starting backend API on port ${API_PORT}..."
pkill -f "dotnet.*LitXus.Api" 2>/dev/null || true
(cd "$BACKEND_DIR" && \
  ConnectionStrings__Default="$CONNECTION_STRING" \
  Jwt__SigningKey="$JWT_SIGNING_KEY" \
  nohup dotnet run --project src/LitXus.Api --launch-profile http > "$API_LOG" 2>&1 &)

echo "Waiting for backend to become healthy..."
for i in $(seq 1 30); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:${API_PORT}/health" 2>/dev/null)" = "200" ] && break
  sleep 1
done

# 5. Build + start frontend
echo
echo "Building frontend..."
(cd "$FRONTEND_DIR" && npm run build)

echo
echo "Starting frontend dev server on port ${FRONTEND_PORT}..."
pkill -f "vite --host" 2>/dev/null || true
(cd "$FRONTEND_DIR" && nohup npm run dev > "$FRONTEND_LOG" 2>&1 &)

echo "Waiting for frontend to become ready..."
for i in $(seq 1 30); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:${FRONTEND_PORT}/" 2>/dev/null)" = "200" ] && break
  sleep 1
done

echo
echo "Opening browser..."
open "http://localhost:${FRONTEND_PORT}/auth/login"

echo
echo "== Ready =="
echo "Frontend:  http://localhost:${FRONTEND_PORT}"
echo "Backend:   http://localhost:${API_PORT}/swagger"
echo "API log:      $API_LOG"
echo "Frontend log: $FRONTEND_LOG"
echo
echo "To stop: pkill -f 'dotnet.*LitXus.Api' ; pkill -f 'vite --host' ; docker stop $SQL_CONTAINER"
