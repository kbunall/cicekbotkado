using Metin2Bot.Domain.Models;

namespace Metin2Bot.Application.Interfaces
{
    public interface IVisionService
    {
        MatchResult? FindTemplate(string templatePath, Region searchRegion, double threshold = 0.8);
    }

    public struct MatchResult
    {
        public Location Location { get; set; }
        public double Confidence { get; set; }
    }
}
