# SauronEye
SauronEye is a search tool built to aid red teams in finding files containing specific keywords. 

**Features**:
- Search multiple (network) drives
- Search contents of files
- Search contents of Microsoft Office files (`.doc`, `.docx`, `.xls`, `.xlsx`)
- Search multiple drives multi-threaded for increased performance
- Supports regular expressions in search keywords
- Compatible with Cobalt Strike's `execute-assembly`

It's also quite fast, can do 50k files, totaling 1,3 TB on a network drive in under a minute (with realistic file filters). Searches a `C:\` (on a cheap SATA SSD) in about 15 seconds.


### Usage

`SauronEye.exe -Dirs C:\, \\SOMENETWORKDRIVE\C$ -FileTypes .txt,.bat,.docx, .conf -Contents -Keywords password,pass* -SystemDirs` 

```
C:\>SauronEye.exe -Dirs C:\Users\vincent\Desktop\ -Keywords wacht, pass -Filetypes .txt, .doc, .docx, .xls -Contents

	=== SauronEye ===

Directories to search: c:\users\vincent\desktop\
For file types: .txt, .doc, .docx, .xls
Containing: wacht, pass
Search contents: True
Search Program Files directories: False

Searching in parallel: c:\users\vincent\desktop\
[+] c:\users\vincent\desktop\test\wachtwoord - Copy (2).txt
[+] c:\users\vincent\desktop\test\wachtwoord - Copy (3).txt
[+] c:\users\vincent\desktop\test\wachtwoord - Copy.txt
[+] c:\users\vincent\desktop\test\wachtwoord.txt
[+] c:\users\vincent\desktop\pass.pdf
[+] c:\users\vincent\desktop\pass.txt
[+] c:\users\vincent\desktop\pass.xls
[*] Done searching file system, now searching contents

[+] c:\users\vincent\desktop\test.docx:
         is a testPassword = "Welcome123"


 Done. Time elapsed = 00:00:00.3114729
```

### Notes
SauronEye does not search `%WINDIR%` and `%APPDATA%`. 
Use the `-SystemDirs` flag to search the contents of `Program Files*`.
SauronEye relies on functionality only available from .NET 4.7.2, and so requires >= .NET 4.7.2 to run.
