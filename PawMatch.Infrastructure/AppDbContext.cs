using Microsoft.EntityFrameworkCore;
using PawMatch.Domain;
using BCrypt.Net;

namespace PawMatch.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<UserSwipe> UserSwipes { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSwipe>()
                .HasOne(us => us.Swiper)
                .WithMany()
                .HasForeignKey(us => us.SwiperId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            modelBuilder.Entity<UserSwipe>()
                .HasOne(us => us.SwipedUser)
                .WithMany()
                .HasForeignKey(us => us.SwipedUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Other existing configurations...
        }

        public static class AppDbContextSeed
        {
            public static void Seed(AppDbContext db)
            {
                if (!db.Users.Any())
                {
                    var user1 = new User { Name = "Berkan Mandacı", Email = "berkan_mandaci@hotmail.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("mandaci12"), Bio = "Kedisever", HasPet = true };
                    var user2 = new User { Name = "Zeynep", Email = "test2@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("mandaci12"), Bio = "Köpeksever", HasPet = false };
                    db.Users.AddRange(user1, user2);
                    db.SaveChanges();

                    var pet1 = new Pet { Name = "Boncuk", Type = "Kedi", Age = 2, UserId = user1.Id };
                    db.Pets.Add(pet1);
                    db.SaveChanges();

                    var photo1 = new Photo { FileName = "ali1.jpg", GoogleDriveFileId = "dummy-file-id-1", UploadDate = DateTime.UtcNow, UserId = user1.Id };
                    var photo2 = new Photo { FileName = "boncuk1.jpg", GoogleDriveFileId = "dummy-file-id-2", UploadDate = DateTime.UtcNow, PetId = pet1.Id };
                    
                    var user1Photo = new Photo { FileName = "testphoto.jpg", GoogleDriveFileId = "user1-test-photo-id", UploadDate = DateTime.UtcNow, UserId = user1.Id };
                    var user2Photo = new Photo { FileName = "testphoto.jpg", GoogleDriveFileId = "user2-test-photo-id", UploadDate = DateTime.UtcNow, UserId = user2.Id };

                    db.Photos.AddRange(photo1, photo2, user1Photo, user2Photo);
                    db.SaveChanges();
                }
            }
        }
    }
} 