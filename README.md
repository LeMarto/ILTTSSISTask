# Informatica Linear Taskflow Trigger SSIS Task
An C# MS SSIS 2012 Task used to trigger Informatica Linear Taskflows from an SSIS package.

## Projects

### ILTTSSISTask
The actual class library project. Makes use of .Net 4.5 framework.

### Setup
The Wix setup project used to deploy the task to non dev servers.

## Credits
* Joost van Rossum for the SSIS Custom Tasks [tutorial](http://microsoft-ssis.blogspot.com/2013/06/create-your-own-custom-task.html).
* Matthew Gajdosik's Wix Toolset [tutorial](https://www.tallan.com/blog/2017/02/02/creating-an-effortless-custom-ssis-object-installer-using-wix/).

## Install Instructions

### Install on the same computer you develop
By compiling the task should appear once you restart visual studio. A post compile script is in place and takes care of everything. Feel free to modify.

### Install on a non dev computer
Compile the full solution and then copy the msi as well as the cab in the bin folder of the setup project to the target machine. Then execute.

## External Libraries / Packages
* [Wix Toolset 1.0.0.4](https://wixtoolset.org/)
* [SQL Server Data Tools 14.0.61021.0](https://docs.microsoft.com/en-us/sql/ssdt/previous-releases-of-sql-server-data-tools-ssdt-and-ssdt-bi?view=sql-server-ver15#ssdt-for-visual-studio-vs-2015)

## Developed on
Microsoft Visual Studio Professional 2015 Version 14.0.25425.01 Update 3.
