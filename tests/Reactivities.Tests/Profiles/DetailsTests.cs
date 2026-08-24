using Application.Profiles;
using Domain;
using FluentAssertions;

namespace Reactivities.Tests.Profiles
{
    public class DetailsTests : TestBase
    {
        [Fact]
        public async Task Handle_Should_Return_Profile_When_Username_Is_Valid()
        {
            // Arrange
            var username = "bob";
            var mockPhotos = new List<Photo> { new Photo { Id = "123abc", Url = "photo.jpg", IsMain = true }};

            await SeedUserAsync(username, "Bob the man", "Lonesome cowboy bob", mockPhotos);
            var handler = CreateHandler<Details.Handler>();

            // Act
            var result = await handler.Handle(new Details.Query { Username = username }, CancellationToken.None);

            // Assert
            result.Value.Should().NotBeNull();
            result.Value.Username.Should().Be(username);
            result.Value.DisplayName.Should().Be("Bob the man");
            result.Value.Bio.Should().Be("Lonesome cowboy bob");
            result.Value.Image.Should().Be("photo.jpg");
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Return_Null_Value_When_Username_Does_Not_Exist()
        {
            // Arrange
            var handler = CreateHandler<Details.Handler>();

            // Act
            var result = await handler.Handle(new Details.Query { Username = "" }, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().BeNull();
            result.IsSuccess.Should().BeTrue();
        }
    }
}