using System;

namespace Miki1106.WebHandling.Form
{
    public class WeekMapping : IFormMapper<DateTime>
    {
        public DateTime Parse(ParserInfo info)
        {
            if (info.fields.TryGetValue(info.fieldName, out _))
            {
                string[] parts = new StringMapping().Parse(info).Split(new[] { "-W" }, StringSplitOptions.None);

                DateTime jan4 = new DateTime(int.Parse(parts[0]), 1, 4);
                int daysOffset = DayOfWeek.Thursday - jan4.DayOfWeek;

                return jan4.AddDays(daysOffset).AddDays(-3).AddDays((int.Parse(parts[1]) - 1) * 7);
            }
            return default;
        }
    }
}
