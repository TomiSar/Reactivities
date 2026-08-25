using Application.Comments;
using Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Reactivities.Tests.Comments
{
    public class CreateTests : TestBase
    {
        // Improved FailingDataContext: allows initialization but prevents saving upon request.
        private class FailingDataContext : Persistence.DataContext
        {
            public bool ShouldFail { get; set; } = false;
            public FailingDataContext(DbContextOptions<Persistence.DataContext> options) : base(options) {}
            public override Task<int> SaveChangesAsync(CancellationToken ct = default)
                => ShouldFail ? Task.FromResult(0) : base.SaveChangesAsync(ct);
        }

        [Fact]
        public async Task Handle_Should_Create_Comment_When_Data_Is_Valid()
        {
            // Arrange
            await SeedUserAsync("bob");
            var activity = await SeedActivityAsync();
            var handler = CreateHandler<Create.Handler>();

            var command = new Create.Command
            {
                Body = "Fantastic activity!",
                ActivityId = activity.Id
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Body.Should().Be("Fantastic activity!");
            result.Value.Username.Should().Be("bob");

            // Double check In-Memory persistence
            var activityInDb = await Context.Activities.FindAsync(activity.Id);
            activityInDb?.Comments.Count.Should().Be(1);
            activityInDb?.Comments.First().Body.Should().Be("Fantastic activity!");
        }

        [Fact]
        public async Task Handle_Should_Return_Null_When_Activity_Does_Not_Exist()
        {
            // Arrange
            await SeedUserAsync("bob");
            var handler = CreateHandler<Create.Handler>();
            var command = new Create.Command { Body = "Ghost comment", ActivityId = Guid.NewGuid() };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeNull(); // Aligns with 'if (activity == null) return null;'
        }

        [Fact]
        public void Create_Validation_Should_Fail_When_Body_Is_Empty()
        {
            // Arrange
            var validator = new Create.CommandValidator();
            var command = new Create.Command { Body = "", ActivityId = Guid.NewGuid() };

            // Act
            var result = validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Body");
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Database_Save_Fails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<Persistence.DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            using var failingContext = new FailingDataContext(options);

            // Init data (ShouldFail = false)
            var user = new AppUser { UserName = "bob" };
            var activity = new Activity { Id = Guid.NewGuid(), Title = "Test" };
            failingContext.Users.Add(user);
            failingContext.Activities.Add(activity);
            await failingContext.SaveChangesAsync();

            // Activate error for save and create handler
            failingContext.ShouldFail = true;
            var handler = new Create.Handler(failingContext, Mapper, MockUserAccessor.Object);
            var command = new Create.Command { Body = "Fail me", ActivityId = activity.Id };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Failed to add comment");
        }
    }
}
