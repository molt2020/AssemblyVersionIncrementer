@echo off
echo.
echo.Updating AssemblyVersionIncrementer as dotnet tool
echo.
rem dotnet tool install --global --add-source AssemblyVersionIncrementer.nupkg  AssemblyVersionIncrementer
dotnet tool update -g AssemblyVersionIncrementer --add-source AssemblyVersionIncrementer.1.0.3.nupkg