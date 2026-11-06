@echo off
setlocal
cd /d "%~dp0"

if not exist node_modules (
    echo Installing dependencies...
    call npm install
    if errorlevel 1 goto :fail
)

echo.
echo === Typechecking (tsc) ===
call npx tsc --noEmit
if errorlevel 1 goto :fail

echo.
echo === Headless engine self-test ===
call npx tsx selftest.ts
if errorlevel 1 goto :fail

echo.
echo ================================
echo   QUICK TEST PASSED
echo ================================
goto :end

:fail
echo.
echo ================================
echo   QUICK TEST FAILED
echo ================================
exit /b 1

:end
pause
