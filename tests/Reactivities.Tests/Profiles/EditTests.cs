using Application.Profiles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Reactivities.Tests.Profiles
{
    public class EditTests : TestBase
    {
        [Fact]
        public async Task Handle_Should_Update_Profile_When_Id_Is_Valid()
        {
            // Arrange
            await SeedUserAsync("bob", "Old Bob", "Old bio");
            var handler = CreateHandler<Edit.Handler>();

            var command = new Edit.Command { DisplayName = "New Bob", Bio = "New Bob bio" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var userInDb = await Context.Users.FirstOrDefaultAsync(x => x.UserName == "bob");
            userInDb.Should().NotBeNull();
            userInDb.DisplayName.Should().Be("New Bob");
            userInDb.Bio.Should().Be("New Bob bio");
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Edit_Validation_Should_Fail_When_DisplayName_Is_Empty()
        {
            // Arrange
            var validator = new Edit.CommandValidator();
            var command = new Edit.Command
            {
                DisplayName = "",
                Bio = "Random bio"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "DisplayName");
        }

                [Fact]
        public async Task Handle_Should_Return_Failure_When_No_Changes_Made()
        {
            // Arrange
            await SeedUserAsync("bob", "Bob", "Bio");
            var handler = CreateHandler<Edit.Handler>();
            var command = new Edit.Command { DisplayName = "Bob", Bio = "Bio" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Problem updating profile");
        }
    }
}
