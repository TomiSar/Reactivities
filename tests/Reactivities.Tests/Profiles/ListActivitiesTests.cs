using Application.Profiles;
using Domain;
using FluentAssertions;

namespace Reactivities.Tests.Profiles
{
    public class ListActivitiesTests : TestBase
    {
        private readonly ListActivities.Handler _handler;

        public ListActivitiesTests()
        {
            _handler = new ListActivities.Handler(Context, Mapper);
        }

        private async Task SeedData(string username)
        {
            var user = await SeedUserAsync(username);

            var pastAct = await SeedActivityAsync(title: "Past Activity", date: DateTime.UtcNow.AddMonths(-1));
            pastAct.Attendees.Add(new ActivityAttendee { AppUser = user, IsHost = false });

            var hostAct = await SeedActivityAsync(title: "Future Hosting", date: DateTime.UtcNow.AddDays(15));
            hostAct.Attendees.Add(new ActivityAttendee { AppUser = user, IsHost = true });

            var futureAct = await SeedActivityAsync(title: "Future Attending", date: DateTime.UtcNow.AddMonths(1));
            futureAct.Attendees.Add(new ActivityAttendee { AppUser = user, IsHost = false });

            await Context.SaveChangesAsync();
        }

        [Fact]
        public async Task Handle_Should_Return_Future_Activities_By_Default()
        {
            // Arrange
            await SeedData("bob");
            var handler = CreateHandler<ListActivities.Handler>();

            // Act
            var result = await handler.Handle(new ListActivities.Query { Username = "bob", Predicate = "future" }, CancellationToken.None);

            // Assert
            result.Value.Count.Should().Be(2);
            result.Value.All(x => x.Date >= DateTime.UtcNow.AddMinutes(-1)).Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Return_Past_Activities()
        {
            // Arrange
            await SeedData("bob");
            var handler = CreateHandler<ListActivities.Handler>();

            // Act
            var result = await handler.Handle(new ListActivities.Query { Username = "bob", Predicate = "past" }, CancellationToken.None);

            // Assert
            result.Value.Count.Should().Be(1);
            result.Value[0].Title.Should().Be("Past Activity");
        }

        [Fact]
        public async Task Handle_Should_Return_Hosted_Activities()
        {
            // Arrange
            await SeedData("bob");
            var handler = CreateHandler<ListActivities.Handler>();

            // Act
            var result = await handler.Handle(new ListActivities.Query { Username = "bob", Predicate = "hosting" }, CancellationToken.None);

            // Assert
            result.Value.Count.Should().Be(1);
            result.Value[0].Title.Should().Be("Future Hosting");
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_List_If_User_Has_No_Activities()
        {
            // Act
            var handler = CreateHandler<ListActivities.Handler>();
            var result = await handler.Handle(new ListActivities.Query { Username = "nobody", Predicate = "future" }, CancellationToken.None);

            // Assert
            result.Value.Should().BeEmpty();
            result.IsSuccess.Should().BeTrue();
        }
    }
}