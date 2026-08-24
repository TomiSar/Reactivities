using Application.Activities;
using Domain;
using FluentAssertions;

namespace Reactivities.Tests.Activities
{
    public class DeleteTests : TestBase
    {
        [Fact]
        public async Task Handle_Should_Delete_Activity_From_Db_When_Id_Is_Valid()
        {
            // Arrange Use iherited Context
            var activityId = Guid.NewGuid();
            Context.Activities.Add(new Activity { Id = activityId, Title = "Removed" });
            await Context.SaveChangesAsync();

            var handler = new Delete.Handler(Context);

            // Act
            var result = await handler.Handle(new Delete.Command { Id = activityId }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            Context.Activities.Count().Should().Be(0);
            Context.Activities.Find(activityId).Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Return_Null_When_Activity_Does_Not_Exist()
        {
            // Arrange
            var handler = new Delete.Handler(Context);

            // Act
            var result = await handler.Handle(new Delete.Command { Id = Guid.NewGuid() }, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}
