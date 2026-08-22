using Application.Core;
using Application.Interfaces;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Comments
{
    public class Edit
    {
        public class Command : IRequest<Result<CommentDto>>
        {
            public int Id { get; set; }
            public string Body { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Body).NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Command, Result<CommentDto>>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;
            private readonly IUserAccessor _userAccessor;

            public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _mapper = mapper;
                _context = context;
            }

            public async Task<Result<CommentDto>> Handle(Command request, CancellationToken cancellationToken)
            {
                var comment = await _context.Comments
                    .Include(x => x.Author)
                    .ThenInclude(p => p.Photos)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (comment == null) return Result<CommentDto>.Failure("Comment not found");

                if (comment.Author.UserName != _userAccessor.GetUsername())
                    return Result<CommentDto>.Failure("You can only edit your own comments");

                comment.Body = request.Body;
                comment.UpdatedAt = DateTime.UtcNow;

                var success = await _context.SaveChangesAsync() > 0;

                if (success) return Result<CommentDto>.Success(_mapper.Map<CommentDto>(comment));

                return Result<CommentDto>.Failure("Failed to update comment");
            }
        }
    }
}
