cd \AnySqlParser\Benchmark||exit /b
msbuild /p:Configuration=Debug /p:Platform="Any CPU"||exit /b
"C:\AnySqlParser\Benchmark\bin\Any CPU\Debug\net7.0\Benchmark.exe" %1
