dotnet pack -c Release

dotnet tool uninstall --global ProjectUpdaterTool

dotnet tool install --global --add-source ProjectUpdaterTool\bin\Release ProjectUpdaterTool --version 1.6.7

pause