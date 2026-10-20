@echo off
cd /d %~dp0
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /unsafe /win32icon:%~dp0..\..\assets\app.ico /out:%~dp0..\..\bin\ArabicSnipper.exe *.cs
