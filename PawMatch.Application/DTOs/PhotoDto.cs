using System;

namespace PawMatch.Application.DTOs
{
    public class PhotoDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string GoogleDriveFileId { get; set; }
        public DateTime UploadDate { get; set; }
        public int? UserId { get; set; }
        public int? PetId { get; set; }
        
    }
} 