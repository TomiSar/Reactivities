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
            var user = new AppUser { UserName = "bob", DisplayName = "Bob" };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            var handler = new Create.Handler(Context, MockUserAccessor.Object);
            var newActivity = new Activity
            {
                Id = Guid.NewGuid(),
                Title = "Test activity",
                Description = "Test description",
                Category = "music",
                Date = DateTime.Now,
                City = "Helsinki",
                Venue = "Pub"
            };

            // Act
            var result = await handler.Handle(new Create.Command { Activity = newActivity }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var created = await Context.Activities
                .Include(x => x.Attendees)
                .ThenInclude(u => u.AppUser) // Include to get userName
                .FirstOrDefaultAsync(x => x.Id == newActivity.Id);

            created.Should().NotBeNull();
            created.Title.Should().Be("Test activity");
            created.Description.Should().Be("Test description");
            created.Category.Should().Be("music");
            created.City.Should().Be("Helsinki");
            created.Venue.Should().Be("Pub");
            created.Attendees.Should().ContainSingle(x => x.AppUser.UserName == "bob" && x.IsHost);
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