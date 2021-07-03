# make-backup 

![](https://raw.githubusercontent.com/Yuukari/make-backup/main/images/header-image.png)

make-backup is a simple CLI utility for creating backups on MEGA drive. It creates a 7Zip archive and upload it on your MEGA drive in "Backups" folder.

## Requirements

This utility requires MEGAcmd client and 7Zip archiver installed. The 7Zip archiver binary "7z.exe" must be added in PATH environment variable. Also, .NET framework 4.6.1+ need to be installed

## Usage

+ First, authorize in your MEGA account:

  ```
  > make-backup --auth someone@example.com password
  ```
  
+ Now you can easily make backup any folder with:
 
  ```
  > make-backup --backup C:\path\to\your\project
  ```
  You can also specify the project name that be used in backup archive name:
  ```
  > make-backup --backup C:\path\to\your\project DifferentProjectName
  ```
  Your backup archive will be named like "DifferentProjectName-03_07_2021-18_21_05.7z"
  
+ You can also log out from your account if you want:

  ```
  > make-backup --logout
  ```
+ Or print help, if you forget something:
  ```
  > make-backup --help
  ```
