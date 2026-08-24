using Application.Activities;
using Domain;
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
            Context.Activities.Add(new Activity { Id = activityId, Title = "New activity" });
            await Context.SaveChangesAsync();

            var handler = new Details.Handler(Context, Mapper, MockUserAccessor.Object);

            // Act
            var result = await handler.Handle(new Details.Query { Id = activityId }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("New activity");
            result.Value.Id.Should().Be(activityId);
        }

        [Fact]
        public async Task Handle_Should_Return_Null_Value_When_Id_Does_Not_Exist()
        {
            // Arrange
            var handler = new Details.Handler(Context, Mapper, MockUserAccessor.Object);

            // Act
            var result = await handler.Handle(new Details.Query { Id = Guid.NewGuid() }, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().BeNull();
            result.IsSuccess.Should().BeTrue();
        }
    }
}