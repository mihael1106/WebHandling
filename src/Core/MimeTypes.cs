using Miki1106.WebHandling.Properties;
using System;
using System.Collections.Generic;

namespace Miki1106.WebHandling.Core
{
    public static class MimeTypes
    {
        private static Dictionary<string, string> _mimeTypes;

        /// <summary>
        /// Loads the default MIME types
        /// </summary>
        public static void LoadMimeTypes()
        {
            LoadMimeTypes(Resources.mime_types);
        }

        /// <summary>
        /// Loads MIME types from a string.
        /// Separated by new lines, each line contains the MIME type and its file extensions separated by a comma, and multiple file extensions are separated by a space.
        /// For eg: video/mp4,mp4 mp4v mpg4
        /// This means that file extensions .mp4, .mp4v and .mpg4 have a MIME type of video/mp4.
        /// a '#' sign at the beginning of a file indicates a comment.
        /// </summary>
        /// <param name="mimeTypes">MIME types and their file extensions</param>
        public static void LoadMimeTypes(string mimeTypes)
        {
            if(mimeTypes == null)
            {
                throw new ArgumentNullException("MIME types cant be null");
            }

            if (_mimeTypes == null)
            {
                _mimeTypes = new Dictionary<string, string>();
            }

            string[] lines = mimeTypes.Replace("\r", "").Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("#"))
                    continue;
                string[] data = line.Split(',');
                if (data.Length != 2 && data.Length != 0)
                {
                    if (WebHandler.debug)
                        Console.WriteLine("Error with " + data[0]);
                    continue;
                }

                foreach (string extension in data[1].Split(' '))
                {
                    if (!_mimeTypes.ContainsKey(extension))
                    {
                        _mimeTypes.Add(extension, data[0]);
                    }
                    else if (WebHandler.debug)
                    {
                        Console.WriteLine("Double file extension detected: " + extension);
                    }
                }
            }
        }

        public static string GetMimeType(string fileExtension)
        {
            if (_mimeTypes == null)
                LoadMimeTypes();

            if (!_mimeTypes.TryGetValue(fileExtension.ToLower().TrimStart('.'), out string mimeType))
            {
                mimeType = "application/octet-stream";
                if (WebHandler.debug)
                    Console.WriteLine($"Couldnt find mime type for \"{fileExtension}\". Defaulting to application/octet-stream");
            }
            return mimeType;
        }
    }
}
