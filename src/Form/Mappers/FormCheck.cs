namespace Miki1106.WebHandling.Form
{
    public struct FormCheck
    {
        public bool Checked {  get; set; }
        public string Content { get; set; }

        public FormCheck(bool Checked, string Content)
        {
            this.Checked = Checked;
            this.Content = Content;
        }
    }
}
