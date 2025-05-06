namespace Miki1106.WebHandling.Form
{
    public class LongMapping : IFormMapper<long>
    {
        public long Parse(ParserInfo info)
        {
            if (info.fields.TryGetValue(info.fieldName, out _))
                return long.Parse(new StringMapping().Parse(info));
            return default;
        }
    }
}
