@echo off
setlocal

rem Keep this wrapper policy-neutral: the user's PowerShell execution policy applies.
powershell.exe -NoLogo -NoProfile -File "%~dp0scripts\Start-Loren.ps1" %*
set "exitCode=%ERRORLEVEL%"
set "noPause="
for %%A in (%*) do if /I "%%~A"=="-NoPause" set "noPause=1"
if not "%exitCode%"=="0" if not defined noPause pause
endlocal & exit /b %exitCode%
