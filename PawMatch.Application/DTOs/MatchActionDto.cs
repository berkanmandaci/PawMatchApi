namespace PawMatch.Application.DTOs
{
    public class MatchActionDto
    {
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public bool Liked { get; set; }
    }

    public class MatchResultDto
    {
        public int MatchId { get; set; }
        public bool Confirmed { get; set; }
    }
} 