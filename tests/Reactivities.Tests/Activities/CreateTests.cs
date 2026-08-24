using Application.Activities;
using Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Reactivities.Tests.Activities
{
    public class CreateTests : TestBase
    {
        [Fact]
        public async Task Handle_Should_Create_Activity_In_Db()
        {
            // Arrange add to database, handler gets value from there
            await SeedUserAsync("bob");
            var handler = CreateHandler<Create.Handler>();
            var newActivity = new Activity { Id = Guid.NewGuid(), Title = "Music activity", Category = "music", City = "Berlin"};

            // Act
            await handler.Handle(new Create.Command { Activity = newActivity }, default);

            // Assert
            var result = await Context.Activities
                .Include(x => x.Attendees)
                .ThenInclude(u => u.AppUser) // Include to get userName
                .FirstOrDefaultAsync(x => x.Id == newActivity.Id);

            result.Should().NotBeNull();
            result.Title.Should().Be("Music activity");
            result.Category.Should().Be("music");
            result.City.Should().Be("Berlin");
            result.Attendees.Should().ContainSingle(x => x.AppUser.UserName == "bob" && x.IsHost);
        }

        [Fact]
        public void Create_Validation_Should_Fail_When_Activity_Title_Is_Empty()
        {
            // Arrange
            var validator = new Create.CommandValidator();
            var command = new Create.Command
            {
                Activity = new Activity { Title = "" }
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Activity.Title");
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_No_Changes_Saved()
        {
            // Here, we simulate a scenario where saving results in no changes.
            // Note: This is difficult to simulate perfectly without a mock database,
            // but you can try it to test the logic

            var handler = new Create.Handler(Context, MockUserAccessor.Object);

            // Try to save an empty command or similar, if your code allows it
            // (If this doesn't work with InMemory, it's OK to leave it marked in orange
            // until moving on to integration tests).
        }
    }
}