using Application.Interfaces;
using Application.Photos;
using Domain;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Reactivities.Tests.Photos
{
    public class DeleteTests : TestBase
    {
        private class FailingDataContext : Persistence.DataContext
        {
            public bool ShouldFail { get; set; } = false;
            public FailingDataContext(DbContextOptions<Persistence.DataContext> options) : base(options) {}
            public override Task<int> SaveChangesAsync(CancellationToken ct = default)
                => ShouldFail ? Task.FromResult(0) : base.SaveChangesAsync(ct);
        }

        [Fact]
        public async Task Handle_Should_Delete_Main_Photo_Successfully()
        {
            // Arrange
            var photos = new List<Photo>
            {
                new Photo { Id = "main_id", Url = "main.png", IsMain = true },
                new Photo { Id = "target_id", Url = "target.png", IsMain = false }
            };
            await SeedUserAsync("bob", "Bob", "Bio", photos);

            // Cloudinary deletion returns a successful text response (not null)
            MockPhotoAccessor.Setup(x => x.DeletePhoto("target_id")).ReturnsAsync("ok");

            var handler = CreateHandler<Delete.Handler>();

            // Act
            var result = await handler.Handle(new Delete.Command { Id = "target_id" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(Unit.Value);

            // Check database - photo removed and main photo remains unchanged
            var userInDb = await Context.Users.Include(p => p.Photos).FirstOrDefaultAsync(x => x.UserName == "bob");
            userInDb?.Photos.Count.Should().Be(1);
            userInDb?.Photos.First().Id.Should().Be("main_id");
        }

        [Fact]
        public async Task Handle_Should_Promote_Next_Photo_To_Main_When_Main_Photo_Is_Deleted()
        {
            // Arrange
            var photos = new List<Photo>
            {
                new Photo { Id = "main_id", Url = "main.png", IsMain = true },
                new Photo { Id = "next_id", Url = "next.png", IsMain = false }
            };
            await SeedUserAsync("bob", "Bob", "Bio", photos);

            MockPhotoAccessor.Setup(x => x.DeletePhoto("main_id")).ReturnsAsync("ok");
            var handler = CreateHandler<Delete.Handler>();

            // Act - remove main photo
            var result = await handler.Handle(new Delete.Command { Id = "main_id" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Confirm database – the second image automatically became the main photo
            var userInDb = await Context.Users.Include(p => p.Photos).FirstOrDefaultAsync(x => x.UserName == "bob");
            userInDb?.Photos.Count.Should().Be(1);
            userInDb?.Photos.First().Id.Should().Be("next_id");
            userInDb?.Photos.First().IsMain.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Return_Null_When_Photo_Does_Not_Exist()
        {
            // Arrange
            await SeedUserAsync("bob");
            var handler = CreateHandler<Delete.Handler>();

            // Act
            var result = await handler.Handle(new Delete.Command { Id = "ghost_id" }, CancellationToken.None);

            // Assert
            result.Should().BeNull(); // Aligns with 'if (photo == null) return null;'
        }

        [Fact]
        public async Task Handle_Should_Fail_When_Cloudinary_Deletion_Returns_Null()
        {
            // Arrange
            var photos = new List<Photo> { new Photo { Id = "target_id", Url = "img.png", IsMain = false } };
            await SeedUserAsync("bob", "Bob", "Bio", photos);

            // Cloudinary-API returs null as error
            MockPhotoAccessor.Setup(x => x.DeletePhoto("target_id")).ReturnsAsync((string)null!);

            var handler = CreateHandler<Delete.Handler>();

            // Act
            var result = await handler.Handle(new Delete.Command { Id = "target_id" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Problem deleting photo from Cloudinary");
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Database_Delete_Fails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<Persistence.DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            using var failingContext = new FailingDataContext(options);
            failingContext.Database.EnsureCreated();

            // Init data (ShouldFail = false)
            var user = new AppUser { UserName = "bob", Photos = new List<Photo> { new Photo { Id = "target_id", IsMain = false } } };
            failingContext.Users.Add(user);
            await failingContext.SaveChangesAsync();

            var mockPhotoAccessorLocal = new Mock<IPhotoAccessor>();
            mockPhotoAccessorLocal.Setup(x => x.DeletePhoto("target_id")).ReturnsAsync("ok");

            // Activate error for save and create handler
            failingContext.ShouldFail = true;
            var handler = new Delete.Handler(failingContext, MockUserAccessor.Object, mockPhotoAccessorLocal.Object);

            // Act
            var result = await handler.Handle(new Delete.Command { Id = "target_id" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Problem deleting photo");
        }
    }
}