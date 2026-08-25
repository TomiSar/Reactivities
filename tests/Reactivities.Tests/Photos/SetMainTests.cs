using Application.Photos;
using Domain;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Reactivities.Tests.Photos
{
    public class SetMainTests : TestBase
    {
                private class FailingDataContext : Persistence.DataContext
        {
            public bool ShouldFail { get; set; } = false;
            public FailingDataContext(DbContextOptions<Persistence.DataContext> options) : base(options) {}
            public override Task<int> SaveChangesAsync(CancellationToken ct = default)
                => ShouldFail ? Task.FromResult(0) : base.SaveChangesAsync(ct);
        }

        [Fact]
        public async Task Handle_Should_Set_New_Main_Photo_And_Demote_Old_Main_Photo()
        {
            // Arrange
            var photos = new List<Photo>
            {
                new Photo { Id = "old_main", Url = "old.png", IsMain = true },
                new Photo { Id = "new_main", Url = "new.png", IsMain = false }
            };
            await SeedUserAsync("bob", "Bob", "Bio", photos);

            var handler = CreateHandler<SetMain.Handler>();

            // Act
            var result = await handler.Handle(new SetMain.Command { Id = "new_main" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(Unit.Value);

            // Verify in the database that the roles were switched correctly.
            var userInDb = await Context.Users.Include(p => p.Photos).FirstOrDefaultAsync(x => x.UserName == "bob");
            var oldMainPhoto = userInDb?.Photos.FirstOrDefault(x => x.Id == "old_main");
            var newMainPhoto = userInDb?.Photos.FirstOrDefault(x => x.Id == "new_main");

            oldMainPhoto?.IsMain.Should().BeFalse(); // Old one lost the main image status
            newMainPhoto?.IsMain.Should().BeTrue();  // New one got the main image status
        }

        [Fact]
        public async Task Handle_Should_Return_Null_When_Photo_Does_Not_Exist()
        {
            // Arrange
            await SeedUserAsync("bob");
            var handler = CreateHandler<SetMain.Handler>();

            // Act
            var result = await handler.Handle(new SetMain.Command { Id = "ghost_id" }, CancellationToken.None);

            // Assert
            result.Should().BeNull(); // Aligns with 'if (photo == null) return null;'
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
            var user = new AppUser
            {
                UserName = "bob",
                Photos = new List<Photo>
                {
                    new Photo { Id = "old_main", IsMain = true },
                    new Photo { Id = "new_main", IsMain = false }
                }
            };
            failingContext.Users.Add(user);
            await failingContext.SaveChangesAsync();

            // Activate error for save and create handler
            failingContext.ShouldFail = true;
            var handler = new SetMain.Handler(failingContext, MockUserAccessor.Object);

            // Act
            var result = await handler.Handle(new SetMain.Command { Id = "new_main" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Problem setting main photo");
        }
    }
}