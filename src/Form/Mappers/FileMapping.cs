using System.IO;

namespace Miki1106.WebHandling.Form
{
    public class FileMapping : IFormMapper<FormFileInfo>
    {
        public FormFileInfo Parse(ParserInfo info)
        {
            if (info.fields.TryGetValue(info.fieldName, out FormField formInfo))
            {
                string fileName = "file.bin";
                if (formInfo.Fields[0].TryGetValue("Content-Disposition", out string value))
                {
                    fileName = FormParser.GetField("filename", value);
                }

                byte[] file = new byte[formInfo.DataEnd[0] - formInfo.DataStart[0]];
                MemoryStream stream = new MemoryStream(info.fieldData)
                {
                    Position = formInfo.DataStart[0]
                };
                stream.Read(file, 0, file.Length);
                return new FormFileInfo(fileName, file);
            }
            return default;
        }
    }
}
