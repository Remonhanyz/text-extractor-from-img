Set WshShell = CreateObject("WScript.Shell")
WshShell.CurrentDirectory = "C:\Users\remon.hany\arabic-text-extractor"
WshShell.Run "bin\ArabicSnipper.exe", 0, False
