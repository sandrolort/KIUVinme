using KiuWho.Services;
using Microsoft.AspNetCore.SignalR;

namespace KiuWho.Hub;

public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly ChatServices _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ChatServices chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.ConnectionId;
        _logger.LogInformation($"User connected: {userId}");
        
        _chatService.AddUserToPool(userId);
        
        await Clients.Caller.SendAsync("WaitingForMatch", "Looking for someone to chat with...");
        
        await TryMatchUser(userId);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.ConnectionId;
        _logger.LogInformation($"User disconnected: {userId}");
        
        // Handle user disconnect - notify chat partner if in active chat
        await _chatService.HandleDisconnect(userId);
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string message)
    {
        var senderId = Context.ConnectionId;
        var recipientId = _chatService.GetChatPartner(senderId);
        
        _logger.LogInformation($"SendMessage - Sender: {senderId}, Recipient: {recipientId}, Message: {message}");
        
        if (!string.IsNullOrEmpty(recipientId))
        {
            var messageData = new
            {
                type = "text",
                content = message, 
                timestamp = DateTime.UtcNow
            };
            
            _logger.LogInformation($"Sending message to recipient {recipientId}: {System.Text.Json.JsonSerializer.Serialize(messageData)}");
            
            // Send message to recipient
            await Clients.Client(recipientId).SendAsync("ReceiveMessage", messageData);
            
            _logger.LogInformation($"Message sent successfully to {recipientId}");
        }
        else
        {
            _logger.LogWarning($"No chat partner found for sender {senderId}");
            
            // Notify sender that they don't have a chat partner
            await Clients.Caller.SendAsync("ChatEnded", "No one is connected to chat with you right now.");
        }
    }
    

    public async Task SendSticker(string stickerId, string stickerUrl, string stickerName)
    {
        var senderId = Context.ConnectionId;
        var recipientId = _chatService.GetChatPartner(senderId);
        
        _logger.LogInformation($"SendSticker - Sender: {senderId}, Recipient: {recipientId}, Sticker: {stickerName}");
        
        if (!string.IsNullOrEmpty(recipientId))
        {
            var stickerData = new
            {
                type = "sticker", 
                content = stickerName, 
                stickerId = stickerId, 
                stickerUrl = stickerUrl, 
                timestamp = DateTime.UtcNow
            };
            
            // Send sticker to recipient
            await Clients.Client(recipientId).SendAsync("ReceiveMessage", stickerData);
            
            _logger.LogInformation($"Sticker sent successfully to {recipientId}");
        }
        else
        {
            _logger.LogWarning($"No chat partner found for sender {senderId}");
            await Clients.Caller.SendAsync("ChatEnded", "No one is connected to chat with you right now.");
        }
    }

    public async Task NextChat()
    {
        var userId = Context.ConnectionId;
        _logger.LogInformation($"NextChat requested by user: {userId}");
        
        await _chatService.EndCurrentChat(userId);
        
        // Add user back to pool
        _chatService.AddUserToPool(userId);
        
        // Inform client they're waiting
        await Clients.Caller.SendAsync("WaitingForMatch", "Looking for someone new to chat with...");
        
        await TryMatchUser(userId);
    }

    private async Task TryMatchUser(string userId)
    {
        var match = _chatService.FindMatch(userId);
        
        _logger.LogInformation($"TryMatchUser - User: {userId}, Match found: {match}");
        
        if (!string.IsNullOrEmpty(match))
        {
            // Create a chat session
            _chatService.CreateChatSession(userId, match);
            
            _logger.LogInformation($"Chat session created between {userId} and {match}");
            
            // Notify both users they've been matched
            await Clients.Client(userId).SendAsync("Matched", "You've been connected with someone!");
            await Clients.Client(match).SendAsync("Matched", "You've been connected with someone!");
        }
        else
        {
            _logger.LogInformation($"No match found for user {userId}");
        }
    }
}