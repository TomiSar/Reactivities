using Application.Followers;

namespace Reactivities.Tests.Followers
{
    public class ListTests : TestBase
    {
        [Fact]
        public async Task List_Followers_ReturnsCorrectProfiles()
        {
            // Arrange: Jane seuraa Bobia. Bobilla on siis 1 seuraaja (Jane).
            var bob = await SeedUserAsync("bob", "Bob");
            var jane = await SeedUserAsync("jane", "Jane");
            await SeedFollowingAsync(jane, bob);

            var handler = CreateHandler<List.Handler>();

            // Act: Listataan Bobin seuraajat
            var result = await handler.Handle(
                new List.Query { Username = "bob", Predicate = "followers" },
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal("jane", result.Value[0].Username);
        }

        [Fact]
        public async Task List_Following_ReturnsCorrectProfiles()
        {
            // Arrange: Bob seuraa Janea. Bobilla on siis 1 seurattava (Jane).
            var bob = await SeedUserAsync("bob", "Bob");
            var jane = await SeedUserAsync("jane", "Jane");
            await SeedFollowingAsync(bob, jane);

            var handler = CreateHandler<List.Handler>();

            // Act: Listataan ketä Bob seuraa
            var result = await handler.Handle(
                new List.Query { Username = "bob", Predicate = "following" },
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal("jane", result.Value[0].Username);
        }

        [Fact]
        public async Task List_InvalidPredicate_ReturnsEmptyList()
        {
            // Arrange
            await SeedUserAsync("bob", "Bob");
            var handler = CreateHandler<List.Handler>();

            // Act
            var result = await handler.Handle(
                new List.Query { Username = "bob", Predicate = "invalid" },
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }
    }
}