namespace Miki1106.WebHandling.Form
{
    public class FormFileInfo
    {
        public string FileName { get; set; }
        public byte[] Data { get; set; }

        public FormFileInfo(string filename, byte[] data)
        {
            FileName = filename;
            Data = data;
        }
    }
}
