using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleFontsInstaller
{
    public interface IPlatformFontInstaller
    {
        public FontInstallResult InstallFont(string path);
        public (bool, string) CheckDependanciesInstalled();

        public bool InstallDependancies();

        public bool IsFontInstalled(string path);
    }
}
