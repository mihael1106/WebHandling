namespace Miki1106.WebHandling.Form
{
    public class CheckMapping : IFormMapper<FormCheck>
    {
        public FormCheck Parse(ParserInfo info)
        {
            if (info.fields.ContainsKey(info.fieldName))
            {
                return new FormCheck(true, new StringMapping().Parse(info));
            }
            return new FormCheck(false, "");
        }
    }
}
