using System.IO;

namespace Miki1106.WebHandling.Form
{
    public class FilesMapping : IFormMapper<FormFileInfo[]>
    {
        public FormFileInfo[] Parse(ParserInfo info)
        {
            if (info.fields.TryGetValue(info.fieldName, out FormField formInfo))
            {
                FormFileInfo[] files = new FormFileInfo[formInfo.Fields.Count];
                for (int i = 0; i < formInfo.Fields.Count; i++)
                {
                    string fileName = "file.bin";
                    if (formInfo.Fields[i].TryGetValue("Content-Disposition", out string value))
                    {
                        fileName = FormParser.GetField("filename", value);
                    }

                    byte[] file = new byte[formInfo.DataEnd[i] - formInfo.DataStart[i]];
                    MemoryStream stream = new MemoryStream(info.fieldData)
                    {
                        Position = formInfo.DataStart[i]
                    };
                    stream.Read(file, 0, file.Length);
                    files[i] = new FormFileInfo(fileName, file);
                }
                return files;
            }
            return default;
        }
    }
}
