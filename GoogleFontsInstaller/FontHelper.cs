using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace GoogleFontsInstaller
{
    public static class FontHelper
    {
        public static string GetFontName(string fontDestination)
        {
            var fontCol = new PrivateFontCollection();
            fontCol.AddFontFile(fontDestination);

            var actualFontName = Path.GetFileName(fontDestination);

            if (fontCol.Families.Count() > 0)
            {
                actualFontName = fontCol.Families[0].Name;

                string extension = Path.GetExtension(fontDestination);

                switch (extension.ToLower())
                {
                    case ".ttf":
                    case ".ttc":
                        actualFontName = string.Format("{0} (TrueType)", actualFontName);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Console.WriteLine("Unable to retrive the font name from '{0}'. Using the file name instead.", fontDestination);
            }

            return actualFontName;
        }
    }
}
