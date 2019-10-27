# SauronEye
SauronEye is a search tool built to aid red teams in finding files containing specific keywords. 

**Features**:
- Search mulitple (network) drives
- Search contents of files
- Search contents of Microsoft Office files (`.doc`, `.docx`, `.xls`, `.xlsx`)
- Search multiple drives multi-threaded for increased performance
- Compatible with Cobalt Strike's `execute-assembly`

It's also quite fast, can do 50k files, totaling 1,3 TB on a network drive in under a minute (with realistic file filters). Searches a `C:\` (on a cheap SATA SSD) in about 15 seconds.


### Usage

`SauronEye.exe -Dirs C:\, \\SOMENETWORKDRIVE\C$ -FileTypes .txt,.bat,.docx, .conf -Contents -Keywords password,pass -SystemDirs` 

```
C:\>SauronEye.exe -Dirs C:\, -FileTypes .docx -Contents -Keywords password

         === SauronEye ===

Directories to search: c:\
For file types:.docx
Containing:password
Search contents: True
Searching dir: c:\

[!] Done searching file system, now searching contents
[+] c:\Users\vincent\Desktop\test.docx:
  this is a  testPassword = "welcome123"

Time elapsed=00:00:06.4837851


```

Note: SauronEye does not search `%WINDIR%` and `%APPDATA%`. Use the `-SystemDirs` flag to search the contents of `Program Files*`.