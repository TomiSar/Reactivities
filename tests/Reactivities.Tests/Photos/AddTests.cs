using System.Text;
using Application.Interfaces;
using Application.Photos;
using Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Reactivities.Tests.Photos
{
    public class AddTests : TestBase
    {
        private class FailingDataContext : Persistence.DataContext
        {
            public bool ShouldFail { get; set; } = false;
            public FailingDataContext(DbContextOptions<Persistence.DataContext> options) : base(options) {}
            public override Task<int> SaveChangesAsync(CancellationToken ct = default)
                => ShouldFail ? Task.FromResult(0) : base.SaveChangesAsync(ct);
        }

        // Helper method to simulate an IFormFile object (a mock image file in memory)
        private IFormFile CreateMockFormFile()
        {
            var content = "fake image content";
            var fileName = "test.png";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            return new FormFile(stream, 0, stream.Length, "File", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

        [Fact]
        public async Task Handle_Should_Add_First_Photo_As_Main_Photo()
        {
            // Arrange
            var user = await SeedUserAsync("bob"); // Empty photo list default
            var mockFile = CreateMockFormFile();

            // Configure the Cloudinary mock to return a successful response
            MockPhotoAccessor.Setup(x => x.AddPhoto(It.IsAny<IFormFile>()))
                .ReturnsAsync(new PhotoUploadResult { PublicId = "cloudinary_id_123_abc", Url = "http://testphoto.com" });

            var handler = CreateHandler<Add.Handler>();
            var command = new Add.Command { File = mockFile };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be("cloudinary_id_123_abc");
            result.Value.Url.Should().Be("http://testphoto.com");
            result.Value.IsMain.Should().BeTrue(); // Firts photo IsMain = true

            // Assert
            var userInDb = await Context.Users.Include(p => p.Photos).FirstOrDefaultAsync(x => x.UserName == "bob");
            userInDb?.Photos.Count.Should().Be(1);
            userInDb?.Photos.First().IsMain.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Add_Subsequent_Photo_As_Not_Main_Photo()
        {
            // Arrange
            // Luodaan käyttäjä, jolla on jo valmiiksi yksi pääprofiilikuva
            var existingPhotos = new List<Photo> { new Photo { Id = "old_id", Url = "old.png", IsMain = true } };
            await SeedUserAsync("bob", "Bob", "Bio", existingPhotos);

            var mockFile = CreateMockFormFile();
            MockPhotoAccessor.Setup(x => x.AddPhoto(It.IsAny<IFormFile>()))
                .ReturnsAsync(new PhotoUploadResult { PublicId = "new_id", Url = "http://test.com" });

            var handler = CreateHandler<Add.Handler>();
            var command = new Add.Command { File = mockFile };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.IsMain.Should().BeFalse(); // Koska vanha pääkuva oli olemassa, uusi saa arvon false

            var userInDb = await Context.Users.Include(p => p.Photos).FirstOrDefaultAsync(x => x.UserName == "bob");
            userInDb?.Photos.Count.Should().Be(2);
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Database_Save_Fails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<Persistence.DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            using var failingContext = new FailingDataContext(options);
            failingContext.Database.EnsureCreated();

            // Init data (ShouldFail = false)
            var user = new AppUser { UserName = "bob", Photos = new List<Photo>() };
            failingContext.Users.Add(user);
            await failingContext.SaveChangesAsync();

            var mockFile = CreateMockFormFile();
            var mockPhotoAccessorLocal = new Mock<IPhotoAccessor>();
            mockPhotoAccessorLocal.Setup(x => x.AddPhoto(It.IsAny<IFormFile>()))
                .ReturnsAsync(new PhotoUploadResult { PublicId = "fail_id", Url = "fail.png" });

            // Activate error for save and create handler
            failingContext.ShouldFail = true;
            var handler = new Add.Handler(failingContext, mockPhotoAccessorLocal.Object, MockUserAccessor.Object);
            var command = new Add.Command { File = mockFile };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Problem adding photo");
        }

    }
}