@echo off
rem Builds a single self-contained NiceMail.exe in .\publish (no .NET install needed to run it).
dotnet publish src\NiceMail\NiceMail.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
