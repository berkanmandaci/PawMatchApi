namespace PawMatch.Application.DTOs
{
    public class DiscoverUserPetDto
    {
        public UserPublicDto User { get; set; }
        public DiscoverPetDto Pet { get; set; } // null olabilir
    }

    public class DiscoverPetDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Age { get; set; }
        public List<DiscoverPhotoDto> Photos { get; set; }
    }

    public class DiscoverPhotoDto
    {
        public string? GoogleDriveFileId { get; set; }
    }
} 