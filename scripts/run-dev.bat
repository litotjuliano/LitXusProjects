@echo off
REM LitXus Systems - build + run the full dev stack, then open the browser.
REM Windows-specific: uses LocalDB via the committed appsettings.Development.json
REM (no Docker/connection-string override needed - see scripts/run-dev.command for macOS).

setlocal

set "PROJECT_ROOT=%~dp0.."
set "BACKEND_DIR=%PROJECT_ROOT%\backend"
set "FRONTEND_DIR=%PROJECT_ROOT%\frontend"
set "API_PORT=5018"
set "FRONTEND_PORT=5173"

echo == LitXus Systems - dev build ^& run ==
echo.

echo Applying database migrations (LocalDB)...
pushd "%BACKEND_DIR%"
dotnet dotnet-ef database update --project src\LitXus.Infrastructure --startup-project src\LitXus.Api
if errorlevel 1 goto :error

echo.
echo Building backend...
dotnet build
if errorlevel 1 goto :error

echo.
echo Starting backend API on port %API_PORT% (new window)...
start "LitXus API" cmd /k "dotnet run --project src\LitXus.Api --launch-profile http"
popd

echo.
echo Building frontend...
pushd "%FRONTEND_DIR%"
call npm run build
if errorlevel 1 goto :error

echo.
echo Starting frontend dev server on port %FRONTEND_PORT% (new window)...
start "LitXus Frontend" cmd /k "npm run dev"
popd

echo.
echo Waiting for servers to start...
timeout /t 8 /nobreak >nul

echo.
echo Opening browser...
start "" "http://localhost:%FRONTEND_PORT%/auth/login"

echo.
echo == Ready ==
echo Frontend: http://localhost:%FRONTEND_PORT%
echo Backend:  http://localhost:%API_PORT%/swagger
echo.
echo (Backend and frontend are running in their own windows - close those windows to stop them.)
goto :eof

:error
echo.
echo Build failed - see errors above.
pause
exit /b 1
