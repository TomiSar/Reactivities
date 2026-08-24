using Application.Activities;
using FluentAssertions;

namespace Reactivities.Tests.Activities
{
    public class DetailsTests : TestBase
    {
        [Fact]
        public async Task Handle_Should_Return_Activity_When_Id_Is_Valid()
        {
            // Arrange
            var activityId = Guid.NewGuid();
            await SeedActivityAsync(id: activityId, title: "New activity");
            var handler = CreateHandler<Details.Handler>();
            // Act
            var result = await handler.Handle(new Details.Query { Id = activityId }, CancellationToken.None);

            // Assert
            result.Value.Title.Should().Be("New activity");
            result.Value.Id.Should().Be(activityId);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Return_Null_Value_When_Id_Does_Not_Exist()
        {
            // Arrange
            var handler = CreateHandler<Details.Handler>();

            // Act
            var result = await handler.Handle(new Details.Query { Id = Guid.NewGuid() }, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().BeNull();
            result.IsSuccess.Should().BeTrue();
        }
    }
}