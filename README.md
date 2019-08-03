# SauronEye
SauronEye is a search tool built to aid red teams in finding files containing specific keywords. 

**Features**:
- Search mulitple locations
- Search contents of files
- Search contents of Office files
- TODO: do this really fast and multithreaded

### Usage

`SauronEye.exe -Dirs C:\, \\SOMENETWORKDRIVE\C$ -FileTypes .txt,.bat,.docx, .conf -Contents -Keywords password,pass` 

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

