namespace PawMatch.Application.DTOs
{
    public class MatchDto
    {
        public int MatchId { get; set; }
        public bool Confirmed { get; set; }
        public UserPublicDto User { get; set; } // Karşı taraf
    }
} 