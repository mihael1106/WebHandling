namespace Miki1106.WebHandling.Form
{
    public class IntMapping : IFormMapper<int>
    {
        public int Parse(ParserInfo info)
        {
            if (info.fields.TryGetValue(info.fieldName, out _))
                return int.Parse(new StringMapping().Parse(info));
            return default;
        }
    }
}
