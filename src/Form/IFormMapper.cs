namespace Miki1106.WebHandling.Form
{
    public interface IFormMapper<T>
    {
        T Parse(ParserInfo info);
    }
}
