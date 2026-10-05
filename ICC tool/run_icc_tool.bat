@echo off
setlocal

cd /d "%~dp0"
set "PYTHONPATH=%CD%\src"

python "%CD%\src\__main__.py"

if errorlevel 1 (
    echo.
    echo ICC Tool terminated with an error.
    pause
)

endlocal