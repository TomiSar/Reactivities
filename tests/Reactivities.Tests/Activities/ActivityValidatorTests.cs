using Application.Activities;
using Domain;
using FluentValidation.TestHelper;

namespace Reactivities.Tests.Activities
{
    public class ActivityValidatorTests
    {
        private readonly ActivityValidator _validator;

        public ActivityValidatorTests()
        {
            _validator = new ActivityValidator();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Have_Error_When_Title_Is_InvalidOrNull(string title)
        {
            var activity = new Activity { Title = title};
            var result = _validator.TestValidate(activity);
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Have_Error_When_Description_Is_InvalidOrNull(string description)
        {
            var activity = new Activity { Description = description};
            var result = _validator.TestValidate(activity);
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Should_Have_Error_When_Date_Is_Null()
        {
            var activity = new Activity();
            var result = _validator.TestValidate(activity);
            result.ShouldHaveValidationErrorFor(x => x.Date);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Have_Error_When_Category_Is_InvalidOrNull(string category)
        {
            var activity = new Activity { Category = category};
            var result = _validator.TestValidate(activity);
            result.ShouldHaveValidationErrorFor(x => x.Category);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Have_Error_When_City_Is_InvalidOrNull(string city)
        {
            var activity = new Activity { City = city};
            var result = _validator.TestValidate(activity);
            result.ShouldHaveValidationErrorFor(x => x.City);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Have_Error_When_Venue_Is_InvalidOrNull(string venue)
        {
            var activity = new Activity { Venue = venue};
            var result = _validator.TestValidate(activity);
            result.ShouldHaveValidationErrorFor(x => x.Venue);
        }
    }
}
