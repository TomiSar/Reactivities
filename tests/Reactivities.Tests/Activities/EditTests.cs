using Application.Activities;
using Domain;
using FluentAssertions;

namespace Reactivities.Tests.Activities
{
    public class EditTests : TestBase
    {
        [Fact]
        public async Task Handle_Should_Update_Activity_When_Id_Is_Valid()
        {
            // Arrange
            var activity = await SeedActivityAsync();
            var handler = CreateHandler<Edit.Handler>();

            var updatedActivity = new Activity { Id = activity.Id, Title = "Updated title", Description = "Updated description" };

            // Act
            var result = await handler.Handle(new Edit.Command { Activity = updatedActivity}, CancellationToken.None);

            // Assert
            var activityInDb = await Context.Activities.FindAsync(activity.Id);
            activityInDb?.Title.Should().Be("Updated title");
            activityInDb?.Description.Should().Be("Updated description");
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Edit_Validation_Should_Fail_When_Activity_Description_Is_Empty()
        {
            // Arrange
            var validator = new Edit.CommandValidator();
            var command = new Edit.Command
            {
                Activity = new Activity { Description = "" }
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Activity.Description");
        }

        [Fact]
        public async Task Handle_Should_Return_Null_When_Id_Is_Invalid()
        {
            // Arrange
            var handler = CreateHandler<Edit.Handler>();

            // Act
            var result = await handler.Handle(new Edit.Command { Activity = new Activity { Id = Guid.NewGuid() }}, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}
