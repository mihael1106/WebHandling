using System;
using System.IO;
using System.Text;

namespace Miki1106.WebHandling.Form
{
    public class StringMapping : IFormMapper<string>
    {
        public string Parse(ParserInfo info)
        {
            if (info.fields.TryGetValue(info.fieldName, out FormField formInfo))
            {
                byte[] buff = new byte[formInfo.DataEnd[0] - formInfo.DataStart[0]];
                MemoryStream stream = new MemoryStream(info.fieldData)
                {
                    Position = formInfo.DataStart[0]
                };
                stream.Read(buff, 0, buff.Length);
                return Encoding.UTF8.GetString(buff);
            }
            else
            {
                return default;
            }
        }
    }
}
