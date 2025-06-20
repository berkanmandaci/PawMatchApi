using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawMatch.Infrastructure;
using PawMatch.Api; // PawMatch.Api projesini import edin
using Moq; // Add this using statement
using PawMatch.Infrastructure.Interfaces; // Add this using statement
using System.Linq; // Already there, but ensure it is
using System.Collections.Concurrent; // Add for ConcurrentDictionary
using Microsoft.Extensions.Hosting; // Required for IHostEnvironment
using Microsoft.Extensions.Hosting.Internal; // Required for HostingEnvironment
using Microsoft.Extensions.Configuration;

namespace PawMatch.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    // In-memory storage for mocked files
    private readonly ConcurrentDictionary<string, (string FileName, Stream Content)> _mockedFiles = new ConcurrentDictionary<string, (string, Stream)>();
    private readonly string _databaseName;

    public CustomWebApplicationFactory()
    {
        _databaseName = Guid.NewGuid().ToString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContextOptions<AppDbContext> registrations
            var dbContextOptionsDescriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList();
            foreach (var descriptor in dbContextOptionsDescriptors)
            {
                services.Remove(descriptor);
            }

            // Remove all existing AppDbContext registrations
            var appDbContextDescriptors = services.Where(
                d => d.ServiceType == typeof(AppDbContext)).ToList();
            foreach (var descriptor in appDbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Remove the real IStorageProvider registration if it exists
            var storageProviderDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStorageProvider));
            if (storageProviderDescriptor != null)
            {
                services.Remove(storageProviderDescriptor);
            }

            // Remove existing IHostEnvironment registration if any
            var hostEnvironmentDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostEnvironment));
            if (hostEnvironmentDescriptor != null)
            {
                services.Remove(hostEnvironmentDescriptor);
            }

            // Add a custom IHostEnvironment for testing to ensure it's Production
            services.AddSingleton<IHostEnvironment>(new HostingEnvironment
            {
                EnvironmentName = "Testing",
                ApplicationName = "PawMatch.Api"
            });

            // Add a DbContext using an in-memory database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Register a mocked IStorageProvider
            var mockStorageProvider = new Mock<IStorageProvider>();
            // Configure mock behavior for UploadAsync
            mockStorageProvider.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                               .ReturnsAsync((Stream stream, string fileName, string contentType) =>
                               {
                                   var fileId = Guid.NewGuid().ToString();
                                   var memoryStream = new MemoryStream();
                                   stream.CopyTo(memoryStream);
                                   memoryStream.Position = 0; // Reset position for future reads
                                   _mockedFiles[fileId] = (fileName, memoryStream);
                                   return fileId;
                               });

            // Configure mock behavior for DeleteAsync
            mockStorageProvider.Setup(s => s.DeleteAsync(It.IsAny<string>()))
                               .Returns((string fileId) =>
                               {
                                   _mockedFiles.TryRemove(fileId, out _);
                                   return Task.CompletedTask;
                               });

            // Configure mock behavior for DownloadAsync
            mockStorageProvider.Setup(s => s.DownloadAsync(It.IsAny<string>()))
                               .ReturnsAsync((string fileId) =>
                               {
                                   if (_mockedFiles.TryGetValue(fileId, out var fileData))
                                   {
                                       var newMemoryStream = new MemoryStream();
                                       fileData.Content.CopyTo(newMemoryStream);
                                       newMemoryStream.Position = 0;
                                       return newMemoryStream;
                                   }
                                   // Return an empty stream for not found files (or throw specific exception)
                                   return new MemoryStream(); 
                               });

            services.AddSingleton(mockStorageProvider.Object);

            // Build the service provider.
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database contexts
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();

                // Ensure the database is created and is clean for each test.
                db.Database.EnsureDeleted(); // Clear the database before creating
                db.Database.EnsureCreated();

                // Seed the database with some test data.
                // We can add simple data here for tests, or use the existing seed method if suitable.
                PawMatch.Infrastructure.AppDbContext.AppDbContextSeed.Seed(db);
            }
        });
    }
} 