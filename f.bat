@echo off
clang-format -i --style=file AnySqlParser\*.cs||exit /b
clang-format -i --style=file AnySqlParserTest\*.cs||exit /b
git diff
