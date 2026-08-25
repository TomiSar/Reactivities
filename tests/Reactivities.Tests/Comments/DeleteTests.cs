using Application.Comments;
using Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Reactivities.Tests.Comments
{
    public class DeleteTests : TestBase
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
        public async Task Handle_Should_Delete_Comment_When_User_Is_Owner()
        {
            // Arrange
            var user = await SeedUserAsync("bob"); // Owner
            var comment = await SeedCommentAsync("My precious comment", author: user);
            var handler = CreateHandler<Delete.Handler>();

            // Act
            var result = await handler.Handle(new Delete.Command { Id = comment.Id }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(comment.Id);

            // Verify destruction from DB context
            var commentInDb = await Context.Comments.FindAsync(comment.Id);
            commentInDb.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Fail_When_User_Is_Not_Owner()
        {
            // Arrange
            var otherUser = await SeedUserAsync("alice", "Alice"); // Created as someone else
            var comment = await SeedCommentAsync("Alice's thoughts", author: otherUser);

            SetCurrentUser("bob"); // Current token identity acting on command is bob
            var handler = CreateHandler<Delete.Handler>();

            // Act
            var result = await handler.Handle(new Delete.Command { Id = comment.Id }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can only delete your own comments");

            // Make sure the data is still safely in the database
            var commentInDb = await Context.Comments.FindAsync(comment.Id);
            commentInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_Should_Fail_When_Comment_Does_Not_Exist()
        {
            // Arrange
            var handler = CreateHandler<Delete.Handler>();

            // Act
            var result = await handler.Handle(new Delete.Command { Id = 9999 }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Comment not found");
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Database_Delete_Fails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<Persistence.DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            using var failingContext = new FailingDataContext(options);

            // Init data (ShouldFail = false)
            var user = new AppUser { UserName = "bob" };
            var comment = new Comment { Id = 1, Body = "To be deleted", Author = user, CreatedAt = DateTime.UtcNow };
            failingContext.Users.Add(user);
            failingContext.Comments.Add(comment);
            await failingContext.SaveChangesAsync();

            // Init data (ShouldFail = false)
            failingContext.ShouldFail = true;
            var handler = new Delete.Handler(failingContext, MockUserAccessor.Object);

            // Act
            var result = await handler.Handle(new Delete.Command { Id = comment.Id }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Failed to delete comment");
        }

    }
}
