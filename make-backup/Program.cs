using System;
using System.Diagnostics;
using System.IO;

namespace MakeBackup
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("make-backup 1.0.0 by Yuukari - CLI utility for creating backups on MEGA drive");

            if (args.Length < 1) 
            {
                Console.WriteLine("Use: make-backup [--auth|--backup|--logout|--help]");
                return 0;
            }

            if (!megaCLIInstalled())
                return 0x10;

            if (!_7ZipInstalled())
                return 0x11;

            string action = args[0];
            int returnCode = 0;

            switch (action)
            {
                case "--auth": returnCode = auth(args); break;
                case "--backup": returnCode = backup(args); break;
                case "--logout": returnCode = logout(); break;
                case "--help": help(); break;
                default: Console.WriteLine("Use: make-backup [--auth|--backup|--logout|--help]"); break;
            }

            return returnCode;
        }

        static bool megaCLIInstalled()
        {
            Process megaCLI = getMEGACLI("help");

            try
            {
                megaCLI.Start();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode == 2)
                    Console.WriteLine("Seems like MEGAcmd client is not installed, or its directory not set in PATH environment variables");
                else
                    Console.WriteLine("Failed to start MEGAcmd client. " + ex.Message);

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start MEGAcmd client. " + ex.Message);
                return false;
            }

            megaCLI.WaitForExit();

            if (megaCLI.ExitCode != 0) {
                Console.WriteLine("Failed to start MEGAcmd client. Process exit with error code " + megaCLI.ExitCode);
                return false;
            } 
            else
            {
                return true;
            }
        }

        static bool _7ZipInstalled()
        {
            Process _7z = get7Zip();

            try
            {
                _7z.Start();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode == 2)
                    Console.WriteLine("Seems like 7Zip is not installed, or its directory not set in PATH environment variables");
                else
                    Console.WriteLine("Failed to start 7Zip. " + ex.Message);

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start 7Zip. " + ex.Message);
                return false;
            }

            _7z.WaitForExit();

            if (_7z.ExitCode != 0)
            {
                Console.WriteLine("Failed to start 7Zip. Process exit with error code " + _7z.ExitCode);
                return false;
            }
            else
            {
                return true;
            }
        }

        static Process getMEGACLI(string arguments = null, bool createWindow = false, bool redirectOutput = false)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/MEGAcmd/MEGAclient.exe";

            if (!Directory.Exists(path))
                path = "MEGAclient";

            Process megaCLI = new Process();
            megaCLI.StartInfo = new ProcessStartInfo()
            {
                FileName = path,
                Arguments = arguments,
                CreateNoWindow = !createWindow,
                UseShellExecute = false,
                RedirectStandardOutput = redirectOutput
            };

            return megaCLI;
        }

        static Process get7Zip(string arguments = null, bool createWindow = false, bool redirectOutput = false)
        {
            Process _7z = new Process();
            _7z.StartInfo = new ProcessStartInfo()
            {
                FileName = "7z",
                Arguments = arguments,
                CreateNoWindow = !createWindow,
                UseShellExecute = false,
                RedirectStandardOutput = redirectOutput
            };

            return _7z;
        }

        static bool isAuthorized()
        {
            Process megaCLI = getMEGACLI("whoami");
            megaCLI.Start();
            megaCLI.WaitForExit();

            return megaCLI.ExitCode == 0;
        }

        static int auth(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Too few arguments. Use: make-backup --auth [username] [password]");
                return 0x01;
            }

            string username = args[1];
            string password = args[2];

            Process megaCLI = getMEGACLI("login " + username + " " + password);
            megaCLI.Start();
            megaCLI.WaitForExit();

            if (megaCLI.ExitCode == 9)
            {
                Console.WriteLine("Auth failed: Wrong username or password");
                return 0x20;
            }

            if (megaCLI.ExitCode == 54)
            {
                Console.WriteLine("You already logged in");
                return 0;
            }

            if (megaCLI.ExitCode != 0)
            {
                Console.WriteLine("Auth failed: Unknown reason. MEGAcmd process exit with code " + megaCLI.ExitCode);
                return 0x21;
            } 
            else
            {
                Console.WriteLine("Auth success!");
                return 0;
            }
        }

        static int backup(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Too few arguments. Use: make-backup --backup [project directory path] {project name}");
                return 0x01;
            }

            string projectDirectory = args[1];
            string projectName = null;
            string projectFilename = null;

            if (args.Length == 3)
                projectName = args[2];

            if (!Directory.Exists(projectDirectory))
            {
                Console.WriteLine("Directory \"" + projectDirectory + "\" not exists");
                return 0x02;
            }

            if (!isAuthorized())
            {
                Console.WriteLine("You're not authorized in your MEGA account. Authorize first: make-backup --auth [username] [password]");
                return 0x03;
            }

            Console.WriteLine("\r\n-------------------------------------------------- Creating archive --------------------------------------------------");
            
            if (projectName == null)
                projectName = Path.GetFileName(projectDirectory).Replace(" ", "-");

            projectFilename = projectName + DateTime.Now.ToString("-dd_MM_yyyy-HH_mm_ss") + ".7z";

            string backupPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Temp\\" + projectFilename;

            Process _7z = get7Zip("a -xr!.next -xr!node_modules " + backupPath + " " + projectDirectory, true);
            _7z.Start();
            _7z.WaitForExit();

            if (_7z.ExitCode != 0)
            {
                Console.WriteLine("\r\n--------------------------------------------------- Backup failed! ---------------------------------------------------\r\n");
                Console.WriteLine("7Zip process exit with code " + _7z.ExitCode);
                return 0x40;
            }

            Console.WriteLine("\r\n----------------------------------------------------- Uploading ------------------------------------------------------\r\n");

            Process megaCLI = getMEGACLI("put -c " + backupPath + " /Backups/" + projectName + "/" + projectFilename, true);
            megaCLI.Start();
            megaCLI.WaitForExit();

            if (megaCLI.ExitCode != 0)
            {
                Console.WriteLine("\r\n--------------------------------------------------- Backup failed! ---------------------------------------------------\r\n");
                Console.WriteLine("MEGAcmd process exit with code " + megaCLI.ExitCode);
                return 0x41;
            }

            Console.WriteLine("\r\n---------------------------------------------------- Backup done! ----------------------------------------------------\r\n");
            Console.WriteLine("Backup saved as \"" + projectFilename + "\"");

            File.Delete(backupPath);
            return 0;
        }

        static int logout()
        {
            Process megaCLI = getMEGACLI("logout");
            megaCLI.Start();
            megaCLI.WaitForExit();

            if (megaCLI.ExitCode != 0)
            {
                Console.WriteLine("Failed to log out. MEGAcmd process exit with code " + megaCLI.ExitCode);
                return 0x40;
            }
            else 
            { 
                Console.WriteLine("Logged out sucessfully");
                return 0;
            }
        }

        static void help()
        {
            Console.WriteLine("\r\nThis utility requires MEGAcmd client and 7Zip archiver installed on your computer");
            Console.WriteLine("The 7Zip archiver binary \"7z.exe\" must be added in PATH environment variable");
            Console.WriteLine("\r\nUsage: ");
            Console.WriteLine("> make-backup --auth [username] [password] - Authorize in your MEGA account");
            Console.WriteLine("> make-backup --backup [project directory path] {project name} - Backup your project on MEGA drive. Backups saved in \"Backups\" folder in root on your drive");
            Console.WriteLine("> make-backup --logout - Logout from your MEGA account");
            Console.WriteLine("> make-backup --help - Print this help");
        }
    }
}
