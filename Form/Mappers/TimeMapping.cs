using System;

namespace Miki1106.WebHandling.Form
{
    public class TimeMapping : IFormMapper<DateTime>
    {
        public DateTime Parse(ParserInfo info)
        {
            if (info.fields.TryGetValue(info.fieldName, out _))
                return DateTime.Parse(new StringMapping().Parse(info));
            return default;
        }
    }
}
