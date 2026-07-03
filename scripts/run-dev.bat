@echo off
REM LitXus Systems - build + run the full dev stack, then open the browser.
REM Windows-specific: uses a local SQL Server instance via the committed appsettings.Development.json
REM (no Docker/connection-string override needed - see scripts/run-dev.command for macOS).

setlocal

set "PROJECT_ROOT=%~dp0.."
set "BACKEND_DIR=%PROJECT_ROOT%\backend"
set "FRONTEND_DIR=%PROJECT_ROOT%\frontend"
set "API_PORT=5018"
set "FRONTEND_PORT=5173"

echo == LitXus Systems - dev build ^& run ==
echo.

echo Freeing ports %API_PORT% and %FRONTEND_PORT% (killing any process already listening)...
for %%P in (%API_PORT% %FRONTEND_PORT%) do (
    for /f "tokens=5" %%A in ('netstat -ano ^| findstr ":%%P " ^| findstr "LISTENING"') do (
        echo   Killing PID %%A on port %%P
        taskkill /F /PID %%A >nul 2>&1
    )
)

echo.
echo Applying database migrations (SQL Server)...
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
echo Waiting for frontend dev server to be ready...
set "RETRIES=0"
:waitloop
curl -s -o nul --max-time 1 "http://localhost:%FRONTEND_PORT%"
if not errorlevel 1 goto :ready
set /a RETRIES+=1
if %RETRIES% GEQ 60 (
    echo Frontend did not respond within 60 seconds - opening browser anyway.
    goto :ready
)
timeout /t 1 /nobreak >nul
goto :waitloop

:ready
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
