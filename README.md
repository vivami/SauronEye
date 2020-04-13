# SauronEye
SauronEye is a search tool built to aid red teams in finding files containing specific keywords. 

**Features**:
- Search multiple (network) drives
- Search contents of files
- Search contents of Microsoft Office files (`.doc`, `.docx`, `.xls`, `.xlsx`)
- Find VBA macros in old 2003 `.xls` and `.doc` files
- Search multiple drives multi-threaded for increased performance
- Supports regular expressions in search keywords
- Compatible with Cobalt Strike's `execute-assembly`

It's also quite fast, can do 50k files, totaling 1,3 TB on a network drive in under a minute (with realistic file filters). Searches a `C:\` (on a cheap SATA SSD) in about 15 seconds.


### Usage examples

```
C:\>SauronEye.exe -d C:\Users\vincent\Desktop\ --filetypes .txt .doc .docx .xls --contents --keywords password pass* -v`

         === SauronEye ===

Directories to search: C:\Users\vincent\Desktop\
For file types: .txt, .doc, .docx, .xls
Containing: wacht, pass
Search contents: True
Search Office 2003 files for VBA: True
Max file size: 1000 KB
Search Program Files directories: False
Searching in parallel: C:\Users\vincent\Desktop\
[+] C:\Users\vincent\Desktop\test\wachtwoord - Copy (2).txt
[+] C:\Users\vincent\Desktop\test\wachtwoord - Copy (3).txt
[+] C:\Users\vincent\Desktop\test\wachtwoord - Copy.txt
[+] C:\Users\vincent\Desktop\test\wachtwoord.txt
[+] C:\Users\vincent\Desktop\pass.txt
[*] Done searching file system, now searching contents
[+] C:\Users\vincent\Desktop\pass.txt
         ...the admin password=admin123...

[+] C:\Users\vincent\Desktop\test.docx:
         ...this is a testPassword = "welkom12...


 Done. Time elapsed = 00:00:01.6656911
```

Search multiple directories, including network drives:

`SauronEye.exe --directories C:\ \\SOMENETWORKDRIVE\C$ --filetypes .txt .bat .docx .conf --contents --keywords password pass*` 

Search paths and shares containing spaces:

`SauronEye.exe -d "C:\Users\user\Path with a space" -d "\\SOME NETWORK DRIVE\C$" --filetypes .txt --keywords password pass*`

```
C:\>SauronEye.exe --help

         === SauronEye ===

Usage: SauronEye.exe [OPTIONS]+ argument
Search directories for files containing specific keywords.

Options:
  -d, --directories=VALUE    Directories to search
  -f, --filetypes=VALUE      Filetypes to search for/in
  -k, --keywords=VALUE       Keywords to search for
  -c, --contents             Search file contents
  -m, --maxfilesize=VALUE    Max file size to search contents in, in kilobytes
  -b, --beforedate=VALUE     Filter files last modified before this date,
                                format: yyyy-MM-dd
  -a, --afterdate=VALUE      Filter files last modified after this date,
                                format: yyyy-MM-dd
  -s, --systemdirs           Search in filesystem directories %APPDATA% and %
                               WINDOWS%
  -v, --vbamacrocheck        Check if 2003 Office files (*.doc and *.xls)
                               contain a VBA macro
  -h, --help                 Show help
```

### Notes
SauronEye does not search `%WINDIR%` and `%APPDATA%`. 
Use the `--systemdirs` flag to search the contents of `Program Files*`.
SauronEye relies on functionality only available from .NET 4.7.2, and so requires >= .NET 4.7.2 to run.

