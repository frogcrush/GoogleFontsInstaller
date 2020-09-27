using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace GoogleFontsInstaller
{
    public class WindowsFontInstaller : IPlatformFontInstaller
    {
        public (bool, string) CheckDependanciesInstalled() => (true, null); //no dependancies on Windows
        public bool InstallDependancies() => true; //no dependancies on Windows


        private const string FONT_REG_PATH = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";

        private const int WM_FONTCHANGE = 0x1D;
        private const int HWND_BROADCAST = 0xffff;

        #region Imports
        [DllImport("gdi32.dll")]
        private static extern int AddFontResource(string lpszFilename);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveFontResource(string lpszFilename);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendNotifyMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        #endregion

        /// <summary>
        /// Install a single font
        /// </summary>
        /// <param name="path">Path to font file</param>
        /// <returns>True if font installed successfully</returns>
        public FontInstallResult InstallFont(string path)
        {
            var fontName = Path.GetFileName(path);

            // Creates the full path where your font will be installed
            var fontDestination = Path.Combine(Environment.GetFolderPath
                                          (Environment.SpecialFolder.Fonts), fontName);

            if (File.Exists(fontDestination))
                return FontInstallResult.FontAlreadyExists;

            try
            {
                Console.WriteLine("\tCopying font...");
                File.Copy(path, fontDestination);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error copying font: " + ex.Message);
                return FontInstallResult.Failure;
            }

            Console.WriteLine("\tUpdating registry...");
            var actualFontName = FontHelper.GetFontName(fontDestination);
            Registry.SetValue(string.Format(@"HKEY_LOCAL_MACHINE\{0}", FONT_REG_PATH), actualFontName, fontName, RegistryValueKind.String);

            Console.WriteLine("\tNotifying system...");
            try
            {
                AddFontResource(fontDestination);
                SendNotifyMessage(new IntPtr(HWND_BROADCAST), WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
            } catch (Exception ex) when (Debugger.IsAttached)
            {
                Console.WriteLine("\tNotify system failed: " + ex.Message);                
            }

            return FontInstallResult.InstallSuccessful;
        }

        /// <summary>
        /// Check if a font is already installed on the system.
        /// </summary>
        /// <param name="path">Full path to font file</param>
        /// <returns>True if font is already installed; otherwise false.</returns>
        public bool IsFontInstalled(string path) => File.Exists(Path.Combine(Environment.GetFolderPath
                                          (Environment.SpecialFolder.Fonts), Path.GetFileName(path)));
        
    }
}
