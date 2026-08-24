using Application.Core;
using Application.Interfaces;
using AutoMapper;
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

            MockUserAccessor = new Mock<IUserAccessor>();
            MockUserAccessor.Setup(x => x.GetUsername()).Returns("bob");
        }

        public void Dispose()
        {
            Context.Database.EnsureDeleted();
            Context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}