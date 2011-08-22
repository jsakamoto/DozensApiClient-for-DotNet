@echo off
pushd %0\..
%windir%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe Dozens.dll /codebase
pause

