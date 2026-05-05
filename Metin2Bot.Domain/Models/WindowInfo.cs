namespace Metin2Bot.Domain.Models
{
    public class WindowInfo
    {
        public nint Handle { get; set; }
        public string Title { get; set; } = string.Empty;

        public override string ToString() => Title;
    }
}
