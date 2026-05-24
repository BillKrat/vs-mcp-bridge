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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish-BlogPostDraft.ps1" ^
  -Slug "vs-mcp-bridge-publish-create-trial" ^
  -SqlConnectionString "%AdventuresOnTheEdgeCS%" ^
  -ReloadBaseUrl "http://localhost:64080" ^
  -ReloadKey "%BlogEngineReloadKey%"

pause
endlocal