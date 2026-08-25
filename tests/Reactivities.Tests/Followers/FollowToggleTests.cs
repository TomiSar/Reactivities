using Application.Followers;
using Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Reactivities.Tests.Followers
{
    public class FollowToggleTests : TestBase
    {
        private class FailingDataContext : Persistence.DataContext
        {
            public bool ShouldFail { get; set; } = false;
            public FailingDataContext(DbContextOptions<Persistence.DataContext> options) : base(options) { }
            public override Task<int> SaveChangesAsync(CancellationToken ct = default)
                => ShouldFail ? Task.FromResult(0) : base.SaveChangesAsync(ct);
        }

        [Fact]
        public async Task FollowToggle_AddFollowing_Success()
        {
            // Arrange
            await SeedUserAsync("bob", "Bob");
            var target = await SeedUserAsync("jane", "Jane");
            var handler = CreateHandler<FollowToggle.Handler>();

            // Act
            var result = await handler.Handle(
                new FollowToggle.Command { TargetUsername = "jane" },
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            var following = await Context.UserFollowings
                .FirstOrDefaultAsync(x => x.Observer.UserName == "bob" && x.Target.UserName == "jane");
            Assert.NotNull(following);
        }

        [Fact]
        public async Task FollowToggle_RemoveFollowing_Success()
        {
            // Arrange
            var observer = await SeedUserAsync("bob", "Bob");
            var target = await SeedUserAsync("jane", "Jane");
            await SeedFollowingAsync(observer, target); // Following

            var handler = CreateHandler<FollowToggle.Handler>();

            // Act
            var result = await handler.Handle(
                new FollowToggle.Command { TargetUsername = "jane" },
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            var following = await Context.UserFollowings
                .FirstOrDefaultAsync(x => x.Observer.UserName == "bob" && x.Target.UserName == "jane");
            Assert.Null(following); // Following should be removed
        }

        [Fact]
        public async Task FollowToggle_TargetNotFound_ReturnsNull()
        {
            // Arrange
            await SeedUserAsync("bob", "Bob");
            var handler = CreateHandler<FollowToggle.Handler>();

            // Act
            var result = await handler.Handle(
                new FollowToggle.Command { TargetUsername = "nonexistent" },
                CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Database_Save_Fails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<Persistence.DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            using var failingContext = new FailingDataContext(options);

            // Luodaan käyttäjät kantaan (ShouldFail = false)
            var bob = new AppUser { Id = "1", UserName = "bob", DisplayName = "Bob" };
            var jane = new AppUser { Id = "2", UserName = "jane", DisplayName = "Jane" };
            failingContext.Users.AddRange(bob, jane);
            await failingContext.SaveChangesAsync();

            // Aktivoidaan virhe tallennukseen
            failingContext.ShouldFail = true;

            // Varmistetaan että MockUserAccessor palauttaa bobin
            MockUserAccessor.Setup(x => x.GetUsername()).Returns("bob");

            var handler = new FollowToggle.Handler(failingContext, MockUserAccessor.Object);
            var command = new FollowToggle.Command { TargetUsername = "jane" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Failed to update following");
        }
    }
}