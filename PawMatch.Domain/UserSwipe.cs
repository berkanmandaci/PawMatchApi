namespace PawMatch.Domain
{
    public class UserSwipe
    {
        public int Id { get; set; }
        public int SwiperId { get; set; }
        public User Swiper { get; set; } // Navigation property
        public int SwipedUserId { get; set; }
        public User SwipedUser { get; set; } // Navigation property
        public bool IsLiked { get; set; }
        public DateTime SwipeDate { get; set; }
    }
} 