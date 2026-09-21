@echo off
rem Double-click to build the portable MazureTools.exe into .\dist (needs the .NET SDK on this PC only).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish.ps1" %*
echo.
pause
