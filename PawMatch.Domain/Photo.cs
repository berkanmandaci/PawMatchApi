using System;

namespace PawMatch.Domain
{
    public class Photo
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string? GoogleDriveFileId { get; set; }
        public DateTime UploadDate { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public int? PetId { get; set; }
        public Pet? Pet { get; set; }
    }
} 