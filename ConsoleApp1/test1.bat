cd \AnySqlParser\ConsoleApp1||exit /b
msbuild /p:Configuration=Debug /p:Platform="Any CPU"||exit /b
echo off
"C:\AnySqlParser\ConsoleApp1\bin\Any CPU\Debug\net7.0\ConsoleApp1.exe" "\AnySqlParser\TestProject1\sql-server\cities.sql"
