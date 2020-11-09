  
@echo off

REM Vars
set "SLNDIR=%~dp0"

REM Restore + Build
dotnet build "%SLNDIR%\Compiler.Application" --nologo || exit /b

REM Run
dotnet run -p "%SLNDIR%\Compiler.Application" --no-build -- %*