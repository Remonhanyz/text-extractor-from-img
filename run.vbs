Set WshShell = CreateObject("WScript.Shell")
WshShell.CurrentDirectory = "C:\Users\remon.hany\arabic-text-extractor"
WshShell.Run "ArabicSnipper.exe", 0, False
