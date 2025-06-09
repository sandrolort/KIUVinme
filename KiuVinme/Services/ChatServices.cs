using System.Collections.Concurrent;
using KiuWho.Hub;
using Microsoft.AspNetCore.SignalR;

namespace KiuWho.Services;

public class ChatServices
{
    private readonly IHubContext<ChatHub> _context;
    
    private readonly ConcurrentBag<string> _waitingUsers = new ConcurrentBag<string>();
    private readonly ConcurrentDictionary<string, string> _activeSession = new ConcurrentDictionary<string, string>();
    
    private readonly ConcurrentDictionary<string, HashSet<string>> _recentChatPartners = new ConcurrentDictionary<string, HashSet<string>>();

    public ChatServices(IHubContext<ChatHub> chatContext)
    {
        this._context = chatContext;
    }
    
    public void AddUserToPool(string userId)
    {
        if (!_activeSession.ContainsKey(userId))
        {
            _waitingUsers.Add(userId);
        }
    }

    public string? FindMatch(string userId)
    {
        var availableUsers = _waitingUsers.Where(u => u != userId).ToList();

        if (!availableUsers.Any()) return null;

        var recentPartners = _recentChatPartners.GetValueOrDefault(userId, new HashSet<string>());
        var preferredUsers = availableUsers.Where(u => !recentPartners.Contains(u)).ToList();
        
        var candidateUsers = preferredUsers.Any() ? preferredUsers : availableUsers;
        
        var match = candidateUsers[new Random().Next(candidateUsers.Count)];
            
        RemoveFromWaitingList(match);
        RemoveFromWaitingList(userId);

        return match;
    }

    public void CreateChatSession(string user1, string user2)
    {
        _activeSession[user1] = user2;
        _activeSession[user2] = user1;
        
        Console.WriteLine($"CreateChatSession - User1: {user1}, User2: {user2}");
        Console.WriteLine($"Active sessions after creation: {_activeSession.Count}");
        
        AddToRecentPartners(user1, user2);
        AddToRecentPartners(user2, user1);
    }

    public string? GetChatPartner(string userId)
    {
        var hasPartner = _activeSession.TryGetValue(userId, out string? partnerId);
        
        Console.WriteLine($"GetChatPartner - UserId: {userId}, HasPartner: {hasPartner}, PartnerId: {partnerId}");
        Console.WriteLine($"Active sessions count: {_activeSession.Count}");
        
        return partnerId;
    }

    public async Task EndCurrentChat(string userId)
    {
        if (_activeSession.TryRemove(userId, out string? partnerId))
        {
            _activeSession.TryRemove(partnerId, out _);

            await _context.Clients.Client(partnerId)
                .SendAsync("ChatEnded", "Your chat partner has left. Click 'Next' to find someone new.");
            
        }
    }

    public async Task HandleDisconnect(string userId)
    {
        await EndCurrentChat(userId);
        RemoveFromWaitingList(userId);
        
        _recentChatPartners.TryRemove(userId, out _);
    }

    private void AddToRecentPartners(string userId, string partnerId)
    {
        _recentChatPartners.AddOrUpdate(userId, 
            new HashSet<string> { partnerId },
            (key, existingSet) =>
            {
                existingSet.Add(partnerId);
                
                if (existingSet.Count > 3)
                {
                    var oldest = existingSet.First();
                    existingSet.Remove(oldest);
                }
                
                return existingSet;
            });
    }

    private void RemoveFromWaitingList(string userId)
    {
        var remainingUsers = _waitingUsers.Where(u => u != userId).ToList();
        
        // Clear the original bag
        while (_waitingUsers.TryTake(out _)) { }
        
        // Add back the remaining users
        foreach (var user in remainingUsers)
        {
            _waitingUsers.Add(user);
        }
    }

    public int GetActiveConnectionsCount()
    {
        return _activeSession.Count / 2; 
    }

    public int GetWaitingUsersCount()
    {
        return _waitingUsers.Count;
    }
}