using System.Collections.Generic;

namespace Miki1106.WebHandling.Form
{
    public class ParserInfo
    {
        public readonly string fieldName;
        public readonly Dictionary<string, FormField> fields;
        public readonly byte[] fieldData;
        public readonly byte[] header;

        public ParserInfo(string fieldName, Dictionary<string, FormField> fields, byte[] fieldData, byte[] header)
        {
            this.fieldName = fieldName;
            this.fields = fields;
            this.fieldData = fieldData;
            this.header = header;
        }
    }
}
