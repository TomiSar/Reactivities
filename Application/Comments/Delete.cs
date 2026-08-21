using Application.Core;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Comments
{
    public class Delete
    {
        public class Command : IRequest<Result<int>>
        {
            public int Id { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<int>>
        {
            private readonly DataContext _context;
            private readonly IUserAccessor _userAccessor;

            public Handler(DataContext context, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _context = context;
            }

            public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
            {
                var comment = await _context.Comments
                    .Include(x => x.Author)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (comment == null) return null;

                if (comment.Author.UserName != _userAccessor.GetUsername())
                    return Result<int>.Failure("You can only delete your own comments");

                _context.Comments.Remove(comment);

                var success = await _context.SaveChangesAsync() > 0;

                if (success) return Result<int>.Success(comment.Id);

                return Result<int>.Failure("Failed to delete comment");
            }
        }
    }
}
