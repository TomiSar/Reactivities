using Application.Core;
using Application.Interfaces;
using AutoMapper;
using Domain;
using Microsoft.EntityFrameworkCore;
using Moq;
using Persistence;

namespace Reactivities.Tests
{
    public class TestBase : IDisposable
    {
        protected readonly DataContext Context;
        protected readonly IMapper Mapper;
        protected readonly Mock<IUserAccessor> MockUserAccessor;
        protected readonly Mock<IPhotoAccessor> MockPhotoAccessor;

        public TestBase()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Context = new DataContext(options);
            Context.Database.EnsureCreated();

            var mockMapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfiles());
            });
            Mapper = mockMapper.CreateMapper();

            // Setup default globally logged-in user to avoid repeating it
            MockUserAccessor = new Mock<IUserAccessor>();
            MockPhotoAccessor = new Mock<IPhotoAccessor>();
            SetCurrentUser("bob");
        }

        // --- HELPERS: IDENTITY --- //
        protected void SetCurrentUser(string username)
        {
            MockUserAccessor.Setup(x => x.GetUsername()).Returns(username);
        }

        // --- ENHANCED DATA SEEDERS WITH OPTIONAL PARAMETERS --- //

        /// <summary>
        /// Seeds an AppUser into the In-Memory DB. Supports nested photo collection seeding.
        /// </summary>
        protected async Task<AppUser> SeedUserAsync(string userName = "bob", string displayName = "Bob", string bio = "Bio", List<Photo>? photos = null)
        {
            var user = new AppUser
            {
                UserName = userName,
                DisplayName = displayName,
                Bio = bio,
                Photos = photos ?? new List<Photo>() // null, defaults to empty list
            };

             Context.Users.Add(user);
             await Context.SaveChangesAsync();
             return user;
        }

        /// <summary>
        /// Seeds an Activity into the In-Memory DB.
        /// </summary>
        protected async Task<Activity> SeedActivityAsync(Guid? id = null , string title = "Test Activity",
            string description = "Activity description", DateTime? date = null)
        {
            var activity = new Activity
            {
                Id = id ?? Guid.NewGuid(),
                Title = title,
                Description = description,
                Date = date ?? DateTime.UtcNow,
                Category = "culture",
                City = "Helsinki",
                Venue = "Pub"
            };

             Context.Activities.Add(activity);
             await Context.SaveChangesAsync();
             return activity;
        }

        /// <summary>
        /// Seeds a Comment linked to an AppUser and an Activity into the In-Memory DB.
        /// </summary>
        protected async Task<Comment> SeedCommentAsync(string body = "Test comment body", AppUser? author = null,
            Activity? activity = null)
        {
            // Ensure parent entities exist to maintain valid relationships
            var commentAuthor = author ?? await SeedUserAsync("commenter", "Commenter");
            var commentActivity = activity ?? await SeedActivityAsync();

            var comment = new Comment
            {
                Body = body,
                Author = commentAuthor,
                Activity = commentActivity,
                CreatedAt = DateTime.UtcNow
            };

            Context.Comments.Add(comment);
            await Context.SaveChangesAsync();
            return comment;
        }
        protected async Task SeedFollowingAsync(AppUser observer, AppUser target)
        {
            Context.UserFollowings.Add(new UserFollowing
            {
                Observer = observer,
                Target = target
            });
            await Context.SaveChangesAsync();
        }

        // --- AUTOMATED REFLECTIVE HANDLER INSTANTIATION ---
        /// <summary>
        /// Dynamically resolves constructor parameters using current TestBase protected utilities.
        /// </summary>
        protected THandler CreateHandler<THandler>() where THandler : class
        {
            var constructor = typeof(THandler).GetConstructors().First();
            var parameters = constructor.GetParameters();
            var args = new List<object>();

            foreach (var param in parameters)
            {
                if (param.ParameterType == typeof(DataContext)) args.Add(Context);
                else if (param.ParameterType == typeof(IMapper)) args.Add(Mapper);
                else if (param.ParameterType == typeof(IUserAccessor)) args.Add(MockUserAccessor.Object);
                else if (param.ParameterType == typeof(IPhotoAccessor)) args.Add(MockPhotoAccessor.Object);
                else throw new InvalidOperationException($"TestBase missing: {param.ParameterType.Name}");
            }

            return (THandler)Activator.CreateInstance(typeof(THandler), args.ToArray())!;
        }

        public void Dispose()
        {
            Context.Database.EnsureDeleted();
            Context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}