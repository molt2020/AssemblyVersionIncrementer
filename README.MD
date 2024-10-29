# AssemblyIncrementer
**AssemblyIncrementer** increments **Assembly Version** and/or **File Version** in C#/.Net projects. 
Alternatively, AssemblyIncrementer can set the version of assembly and file as provided by command line parameters.

This utility is aimed at automated builds where you want your assembly or file versions to be incremented by 1 or set to a specific value to avoid installation clashes.

The app uses command line parameters and supports both older style of AssemblyInfo.cs file 
as well as the SDK-style (.NET Core, NET 5+).csproject files.

## **Command Line Parameters**
* `--help|-?|-h` Basic command line help
* `--file <file to process>` File to process, e.g. **--file MyProject\AssemblyInfo.cs** or **--file MyProject\myproject.csproj** 

* `--backup` If specified, a copy of the file specified with `--file` will be retained with extension `.bak`, e.g. AssemblyInfo.cs.bak
* `--assemblyV <version>` If specified, sets the AssemblyVersion to the set value, e.g. **--assemblyV 1.0.0.0**
* `--fileV <version>` If specified, sets the AssemblyVersion to the set value, e.g. **--fileV 1.0.0.0**

## **Examples**

## **Exit codes**

When invoked from the command line, exit codes are returned to indicate success of operation.
The exit codes returned are as follows


| Exit Code | Meaning    |
|-----------|------------|
| 0         | Success    |
| 1         | Error(s)   |
| 78        | Warning(s) |


# Global Tool Installation
You can install this package as a global tool for ease of use. To do this you can do something like:
`dotnet tool update -g AssemblyVersionIncrementer --add-source AssemblyVersionIncrementer.1.0.6.nupkg`

