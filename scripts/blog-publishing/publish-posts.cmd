@echo off
setlocal

if "%AdventuresOnTheEdgeCS%"=="" (
  echo AdventuresOnTheEdgeCS is not set.
  pause
  exit /b 1
)

if "%BlogEngineReloadKey%"=="" (
  echo BlogEngineReloadKey is not set.
  pause
  exit /b 1
)

if "%~1"=="" (
  echo No slugs provided.
  echo Usage: %~nx0 slug1 slug2 slug3 ...
  pause
  exit /b 1
)

echo Running publish for %* ...
echo.

for %%S in (%*) do (
  echo ============================================
  echo Publishing slug: %%S
  echo ============================================

  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish-BlogPostDraft.ps1" ^
    -Slug "%%S" ^
    -SqlConnectionString "%AdventuresOnTheEdgeCS%" ^
    -ReloadBaseUrl "http://localhost:64080" ^
    -ReloadKey "%BlogEngineReloadKey%"

  echo Finished slug: %%S
  echo.
)

echo All slugs processed.
pause
endlocal
