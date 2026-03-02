using System.Collections.Generic;

namespace Miki1106.WebHandling.Form
{
    public struct FormField
    {
        public List<int> DataStart;
        public List<int> DataEnd;
        public List<Dictionary<string, string>> Fields;
    }
}
