namespace PawMatch.Domain
{
    public class Match
    {
        public int Id { get; set; }
        public int User1Id { get; set; }
        public User User1 { get; set; }
        public int User2Id { get; set; }
        public User User2 { get; set; }
        public bool Confirmed { get; set; }
    }
} 