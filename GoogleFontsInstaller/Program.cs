using LibGit2Sharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

//TODO: allow force install where existing fonts are overwritten
//TODO: count how many failed and succeeded

namespace GoogleFontsInstaller
{
    class Program
    {
        private static string CurrentDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private static string DefaultFontsDirectory => Path.Combine(CurrentDirectory, "fonts");

        public static string FontsDirectory { get; set; }

        /// <summary>
        /// Main entry point into the application.
        /// </summary>
        /// <param name="skipPull">Skip running a pull in the Github repo. This will speed things up if you've already done so.</param>
        /// <param name="apache">Install fonts with the Apache license</param>
        /// <param name="ofl">Install fonts with the Open Font License</param>
        /// <param name="ufl">Install fonts with the Ubuntu Font License</param>
        public static async Task<int> Main(bool skipPull = false, bool apache = true, bool ofl = true, bool ufl = true, bool installDepsAutomatically = false, string manualFontFolder = @"F:\Programming\Resources\fonts", bool autoInstall = false)
        {
            RootHelper.RequireAdministrator();

            //First let's grab our platform and make sure we have dependancies

            FontsDirectory = manualFontFolder == null ? DefaultFontsDirectory : manualFontFolder;

            if (!Directory.Exists(FontsDirectory) && manualFontFolder != null)
            {
                Console.WriteLine("Supplied font folder does not exist. Create? (Y/N)");
                var input = Console.ReadLine();
                if (input.ToLower() == "y" || input.ToLower() == "yes")
                {
                    //Continue
                }
                else
                {
                    Console.Error.WriteLine("Supplied font folder does not exist.");
                    return 1;
                }
            }
            

            IPlatformFontInstaller installer = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                installer = new WindowsFontInstaller();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new NotImplementedException();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new NotImplementedException();

            if (installer == null)
            {
                Console.Error.WriteLine("Sorry, this platform has no supported installer.");
                return 1;
            }

            var (dependanciesAvailable, message) = installer.CheckDependanciesInstalled();
            if (!dependanciesAvailable)
            {
                if (!installDepsAutomatically)
                {
                    Console.WriteLine(message);
                    Console.WriteLine("You are missing dependancies. Would you like to install them? (Y/N)");
                    var input = Console.ReadLine();
                    if (input.ToLower() == "y" || input.ToLower() == "yes")
                    {
                        //Continue
                    }
                    else
                    {
                        Console.Error.WriteLine("Dependancies are not installed; cannot continue");
                        return 1;
                    }
                }

                bool result = installer.InstallDependancies();
                if (result)
                {
                    Console.WriteLine("Dependancies installed successfullly.");
                }
                else
                {
                    return 1;
                }
            }


            if (!skipPull)
            {
                if (!SyncWithRepo())
                    return 1;
            }
            else
            {
                if (!Directory.Exists(FontsDirectory))
                {
                    Console.Error.WriteLine("Fonts directory not found; cannot install fonts.");
                    return 1;
                }
            }

            var fontInstallList = new List<string>();

            if (apache)
            {
                ScanFolderForFonts(ref fontInstallList, Path.Combine(FontsDirectory, "apache"));
            }

            if (ofl)
            {
                ScanFolderForFonts(ref fontInstallList, Path.Combine(FontsDirectory, "ofl"));
            }

            if (ufl)
            {
                ScanFolderForFonts(ref fontInstallList, Path.Combine(FontsDirectory, "ufl"));
            }

            //Remove already-installed fonts.
            int total = fontInstallList.Count;
            Console.WriteLine($"{total} total fonts found in Google Font Library under selected licenses.");

            fontInstallList = fontInstallList.Where(x => !installer.IsFontInstalled(x)).ToList();
            Console.WriteLine($"{total - fontInstallList.Count} fonts already installed and skipped.");

            //Now update count for install
            total = fontInstallList.Count;
            int current = 1;

            if (!autoInstall)
            {
                Console.WriteLine($"{total} fonts will be installed. Continue? (Y/N)");
                var input = Console.ReadLine();
                if (input.ToLower() == "y" || input.ToLower() == "yes")
                {
                    //Continue
                }
                else
                {
                    Console.Error.WriteLine("Cancelled by user.");
                    return 1;
                }
            }

            //now install
            foreach (var font in fontInstallList)
            {
                Console.WriteLine($"Installing font {current} of {total}...");
                var result = installer.InstallFont(font);

                if (result == FontInstallResult.InstallSuccessful)
                    Console.WriteLine("Install successful.");
                else
                    Console.WriteLine("Install failed.");
                current++;
            }

            return 0;
        }

        private static void ScanFolderForFonts(ref List<string> fontList, string path)
        {
            fontList.AddRange(new DirectoryInfo(path).GetFiles("*.ttf", SearchOption.AllDirectories).Select(f => f.FullName));
        }

        private static bool SyncWithRepo()
        {
            Repository repo = null;

            //Synchronizing local font cache with Github...
            if (Directory.Exists(FontsDirectory))
            {
                Console.WriteLine("Pulling from Github...");
                //pull
                repo = new Repository(FontsDirectory);

                var result = Commands.Pull(repo, new Signature(new Identity("noone", "noone@noone.com"), DateTimeOffset.Now), new PullOptions());
                if (result.Status == MergeStatus.Conflicts)
                {
                    Console.WriteLine("Pull failed due to conflicts.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Cloning Github repo...");
                repo = new Repository(Repository.Clone("https://github.com/google/fonts.git", CurrentDirectory));
            }
            return true;
        }
    }
}
