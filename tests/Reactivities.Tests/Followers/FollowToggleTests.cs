using Application.Followers;
using Microsoft.EntityFrameworkCore;

namespace Reactivities.Tests.Followers
{
    public class FollowToggleTests : TestBase
    {
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
    }
}