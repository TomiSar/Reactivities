using Application.Comments;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class ChatHub : Hub
    {
        private readonly IMediator _mediator;

        public ChatHub(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task SendComment(Create.Command command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                await Clients.Group(command.ActivityId.ToString()).SendAsync("ReceiveComment", result.Value);
            }
            else if (!string.IsNullOrEmpty(result.Error))
            {
                throw new HubException(result.Error);
            }
        }

        public async Task EditComment(Edit.Command command)
        {
            var httpContext = Context.GetHttpContext();
            var activityId = httpContext.Request.Query["activityId"];

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                await Clients.Group(activityId.ToString()).SendAsync("EditComment", result.Value);
            }
            else if (!string.IsNullOrEmpty(result.Error))
            {
                throw new HubException(result.Error);
            }
        }

        public async Task DeleteComment(Delete.Command command)
        {
            var httpContext = Context.GetHttpContext();
            var activityId = httpContext.Request.Query["activityId"];

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                await Clients.Group(activityId.ToString()).SendAsync("DeleteComment", result.Value);
            }
            else if (!string.IsNullOrEmpty(result.Error))
            {
                throw new HubException(result.Error);
            }
        }


        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var activityId = httpContext.Request.Query["activityId"];
            await Groups.AddToGroupAsync(Context.ConnectionId, activityId);
            var result = await _mediator.Send(new List.Query { ActivityId = Guid.Parse(activityId) });
            await Clients.Caller.SendAsync("LoadComments", result.Value);
        }
    }
}
