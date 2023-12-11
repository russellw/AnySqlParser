cd \AnySqlParser\RoundTrip||exit /b
msbuild /p:Configuration=Debug /p:Platform="Any CPU"||exit /b
"C:\AnySqlParser\RoundTrip\bin\Any CPU\Debug\net7.0\RoundTrip.exe" %*
