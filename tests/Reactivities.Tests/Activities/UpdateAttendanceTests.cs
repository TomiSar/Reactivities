using Application.Activities;
using Domain;
using FluentAssertions;

namespace Reactivities.Tests.Activities
{
    public class UpdateAttendanceTests : TestBase
    {
        private readonly UpdateAttendance.Handler _handler;

        public UpdateAttendanceTests()
        {
            _handler = new UpdateAttendance.Handler(Context, MockUserAccessor.Object);
        }

        [Fact]
        public async Task Handle_Should_Add_Attendance_If_Not_Already_Attending()
        {
            // Arrange
            var activity = new Activity { Id = Guid.NewGuid(), Title = "Test" };
            Context.Activities.Add(activity);
            var user = new AppUser { UserName = "bob" }; // TestBase default "bob"
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new UpdateAttendance.Command { Id = activity.Id }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            activity.Attendees.Count.Should().Be(1);
            activity.Attendees.Any(x => x.AppUser.UserName == "bob").Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Remove_Attendance_If_Already_Attending_And_Not_Host()
        {
            // Arrange
            var user = new AppUser { UserName = "bob" };
            var host = new AppUser { UserName = "jane" };
            Context.Users.AddRange(user, host);

            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                Attendees = new List<ActivityAttendee>
                {
                    new ActivityAttendee { AppUser = host, IsHost = true },
                    new ActivityAttendee { AppUser = user, IsHost = false }
                }
            };
            Context.Activities.Add(activity);
            await Context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new UpdateAttendance.Command { Id = activity.Id }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            activity.Attendees.Count.Should().Be(1);
            activity.Attendees.Any(x => x.AppUser.UserName == "bob").Should().BeFalse();
            activity.Attendees.Any(x => x.AppUser.UserName == "jane").Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Toggle_Cancelled_If_User_Is_Host()
        {
            // Arrange
            var user = new AppUser { UserName = "bob" };
            Context.Users.Add(user);

            var activity = new Activity { Id = Guid.NewGuid(), Title = "Test", IsCancelled = false };
            activity.Attendees = new List<ActivityAttendee>
            {
                new ActivityAttendee { AppUser = user, IsHost = true }
            };
            Context.Activities.Add(activity);
            await Context.SaveChangesAsync();

            // Act
            await _handler.Handle(new UpdateAttendance.Command { Id = activity.Id }, CancellationToken.None);

            // Assert
            activity.IsCancelled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Return_Null_If_Activity_Missing()
        {
            var result = await _handler.Handle(new UpdateAttendance.Command { Id = Guid.NewGuid() }, CancellationToken.None);
            result.Should().BeNull();
        }
    }
}