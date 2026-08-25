
using Application.Comments;
using Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Reactivities.Tests.Comments
{
    public class EditTests : TestBase
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
        public async Task Handle_Should_Update_Comment_When_User_Is_Owner()
        {
            // Arrange
            var user = await SeedUserAsync("bob"); // Owner matching current token context
            var comment = await SeedCommentAsync("Original message", author: user);
            var handler = CreateHandler<Edit.Handler>();

            var command = new Edit.Command { Id = comment.Id, Body = "Updated message" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Body.Should().Be("Updated message");

            // Verify persistence mutation inside database state
            var commentInDb = await Context.Comments.FindAsync(comment.Id);
            commentInDb?.Body.Should().Be("Updated message");
            commentInDb?.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_Should_Fail_When_User_Is_Not_Owner()
        {
            // Arrange
            var otherUser = await SeedUserAsync("jane", "Jane");
            var comment = await SeedCommentAsync("Jane original message", author: otherUser);

            SetCurrentUser("bob"); // Logged in user is bob, attempting to edit Jane's record
            var handler = CreateHandler<Edit.Handler>();

            var command = new Edit.Command { Id = comment.Id, Body = "Hacked body message" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can only edit your own comments");

            // Guarantee the database was not updated
            var commentInDb = await Context.Comments.FindAsync(comment.Id);
            commentInDb?.Body.Should().Be("Jane original message");
        }

        [Fact]
        public async Task Handle_Should_Fail_When_Comment_Does_Not_Exist()
        {
            // Arrange
            var handler = CreateHandler<Edit.Handler>();
            var command = new Edit.Command { Id = 99999, Body = "Valid body" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Comment not found");
        }

        [Fact]
        public void Edit_Validation_Should_Fail_When_Body_Is_Empty()
        {
            // Arrange
            var validator = new Edit.CommandValidator();
            var command = new Edit.Command { Id = 1, Body = "" };

            // Act
            var result = validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Body");
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Database_Update_Fails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<Persistence.DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            using var failingContext = new FailingDataContext(options);

            // Init data (ShouldFail = false)
            var user = new AppUser { UserName = "bob" };
            var comment = new Comment { Id = 1, Body = "Original Message", Author = user, CreatedAt = DateTime.UtcNow };
            failingContext.Users.Add(user);
            failingContext.Comments.Add(comment);
            await failingContext.SaveChangesAsync();

            // Activate error for save and create handler
            failingContext.ShouldFail = true;
            var handler = new Edit.Handler(failingContext, Mapper, MockUserAccessor.Object);
            var command = new Edit.Command { Id = comment.Id, Body = "New message change" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Failed to update comment"); // 💡 Tässä nyt oikea virheviesti
        }
    }
}
