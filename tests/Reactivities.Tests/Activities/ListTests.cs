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
            Context.Activities.Add(new Activity { Title = "Activity 1", Date = DateTime.Now.AddDays(1) });
            Context.Activities.Add(new Activity { Title = "Activity 2", Date = DateTime.Now.AddDays(2) });
            await Context.SaveChangesAsync();

            var handler = new List.Handler(Context, Mapper, MockUserAccessor.Object);

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
            var user = new AppUser { UserName = "bob", DisplayName = "Bob" };
            Context.Users.Add(user);

            // Going and not Host
            var activityGoing = new Activity
            {
                Id = new Guid(),
                Title = "I am going",
                Date = DateTime.Now.AddDays(1),
                Attendees = new List<ActivityAttendee> { new ActivityAttendee { AppUser = user, IsHost = false } }
            };

            // Not Going
            var activityNotGoing = new Activity
            {
                Id = new Guid(),
                Title = "I am not going",
                Date = DateTime.Now.AddDays(2),
            };

            Context.Activities.AddRange(activityGoing, activityNotGoing);
            await Context.SaveChangesAsync();

            var handler = new List.Handler(Context, Mapper, MockUserAccessor.Object);

            // Act IsGoing = true
            var queryParams = new ActivityParams { IsGoing = true, IsHost = false };
            var result = await handler.Handle(new List.Query { Params = queryParams }, CancellationToken.None);

            // Assert
            result.Value.Count.Should().Be(1);
            result.Value[0].Title.Should().Be("I am going");
        }

        [Fact]
        public async Task Handle_Should_Filter_By_IsHost()
        {
            // Arrange
            var user = new AppUser { UserName = "bob", DisplayName = "Bob" };
            Context.Users.Add(user);

            var activity = new Activity
            {
                Title = "Bob birthday party",
                Date = DateTime.Now.AddDays(1),
                Attendees = new List<ActivityAttendee> { new ActivityAttendee { AppUser = user, IsHost = true } }
            };
            Context.Activities.Add(activity);
            Context.Activities.Add(new Activity { Title = "Random party", Date = DateTime.Now.AddDays(1) });
            await Context.SaveChangesAsync();

            var handler = new List.Handler(Context, Mapper, MockUserAccessor.Object);

            // Act
            var result = await handler.Handle(new List.Query { Params = new ActivityParams { IsHost = true } }, CancellationToken.None);

            // Assert
            result.Value.Count.Should().Be(1);
            result.Value[0].Title.Should().Be("Bob birthday party");
        }
    }
}