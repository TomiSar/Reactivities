using Application.Comments;
using FluentAssertions;

namespace Reactivities.Tests.Comments
{
    public class ListTests : TestBase
    {
        [Fact]
        public async Task Handle_Should_Return_Comments_Belonging_To_Activity_Ordered_By_Newest()
        {
            // Arrange
            var targetActivity = await SeedActivityAsync();
            var randomActivity = await SeedActivityAsync();

            // Seed entries linked to our target activity, adding slight delays to test the timestamp sorting logic
            var commentOld = await SeedCommentAsync("Old comment", activity: targetActivity);
            commentOld.CreatedAt = DateTime.UtcNow.AddMinutes(-30);

            var commentNew = await SeedCommentAsync("Newest comment", activity: targetActivity);
            commentNew.CreatedAt = DateTime.UtcNow;

            // Seed noise record attached to a completely different activity profile
            await SeedCommentAsync("Unrelated conversation", activity: randomActivity);

            var handler = CreateHandler<List.Handler>();

            // Act
            var result = await handler.Handle(new List.Query { ActivityId = targetActivity.Id }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Count.Should().Be(2);

            // Verify structural index position sorting parameters (Newest first)
            result.Value[0].Body.Should().Be("Newest comment");
            result.Value[1].Body.Should().Be("Old comment");
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_List_When_Activity_Has_No_Comments()
        {
            // Arrange
            var cleanActivity = await SeedActivityAsync();
            var handler = CreateHandler<List.Handler>();

            // Act
            var result = await handler.Handle(new List.Query { ActivityId = cleanActivity.Id }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }
    }
}
