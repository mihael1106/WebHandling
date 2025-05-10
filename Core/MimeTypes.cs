using Miki1106.WebHandling.Properties;
using System;
using System.Collections.Generic;

namespace Miki1106.WebHandling.Core
{
    internal static class MimeTypes
    {
        private static Dictionary<string, string> _mimeTypes;

        public static string GetMimeType(string fileExtension)
        {
            if (_mimeTypes == null)
            {
                _mimeTypes = new Dictionary<string, string>();
                string[] lines = Resources.mime_types.Replace("\r", "").Split('\n');
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
