using Application.Activities;
using Domain;
using FluentAssertions;

namespace Reactivities.Tests.Activities
{
    public class ListTests : TestBase
    {
        [Fact]
        public async Task Handle_Should_Return_List_Of_Activities()
        {
            // Arrange
            await SeedActivityAsync(title: "Activity 1", date: DateTime.Now.AddDays(1));
            await SeedActivityAsync(title: "Activity 2", date: DateTime.Now.AddDays(2));
            var handler = CreateHandler<List.Handler>();

            // Act
            var result = await handler.Handle(new List.Query { Params = new ActivityParams() }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Count.Should().Be(2);
        }

        [Fact]
        public async Task Handle_Should_Filter_By_IsGoing()
        {
            // Arrange
            var user = await SeedUserAsync("bob");
            var activity = await SeedActivityAsync(title: "I am going", date: DateTime.Now.AddDays(1));

            // Lisätään osallistuminen (tätä varten voisi tehdä myöhemmin SeedAttendance-metodin)
            activity.Attendees.Add(new ActivityAttendee { AppUser = user, IsHost = false });
            await Context.SaveChangesAsync();

            await SeedActivityAsync(title: "I am not going", date: DateTime.Now.AddDays(2));

            var handler = CreateHandler<List.Handler>();

            // Act
            var result = await handler.Handle(new List.Query { Params = new ActivityParams { IsGoing = true, IsHost = false } }, CancellationToken.None);

            // Assert
            result.Value.Count.Should().Be(1);
            result.Value[0].Title.Should().Be("I am going");
        }

        [Fact]
        public async Task Handle_Should_Filter_By_IsHost()
        {
            // Arrange
            var user = await SeedUserAsync("bob");
            var activity = await SeedActivityAsync(title: "Bob birthday party", date: DateTime.Now.AddDays(1));

            activity.Attendees.Add(new ActivityAttendee { AppUser = user, IsHost = true });
            await Context.SaveChangesAsync();

            await SeedActivityAsync(title: "Random party", date: DateTime.Now.AddDays(2));
            var handler = CreateHandler<List.Handler>();

            // Act
            var result = await handler.Handle(new List.Query { Params = new ActivityParams { IsHost = true } }, CancellationToken.None);

            // Assert
            result.Value.Count.Should().Be(1);
            result.Value[0].Title.Should().Be("Bob birthday party");
        }
    }
}