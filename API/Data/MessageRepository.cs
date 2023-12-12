using API.DTO;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MessageRepository : IMessageRepository
{
  private readonly DataContext _context;
  private readonly IMapper _mapper;

  public MessageRepository(DataContext context, IMapper mapper)
  {
    _context = context;
    _mapper = mapper;
  }

  public void AddMessage(Message message)
  {
    _context.Messages.Add(message);
  }

  public void DeleteMEssage(Message message)
  {
    _context.Messages.Remove(message);
  }

  public async Task<Message> GetMessage(int id)
  {
    return await _context.Messages.FindAsync(id);
  }

  public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
  {
    var query = _context.Messages.OrderByDescending(x => x.MessageSent).AsQueryable();
    query = messageParams.Container switch
    {
      "Inbox" => query.Where(u => u.RecipientUserName == messageParams.UserName && u.RecipientDeleted == false),
      "Outbox" => query.Where(u => u.SenderUserName == messageParams.UserName && u.SenderDeleted == false),
      _ => query.Where(u => u.RecipientUserName == messageParams.UserName && u.DateRead == null && u.RecipientDeleted == false)
    };

    var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

    return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
  }

  public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string recipientUserName)
  {
    var messages = await _context.Messages
      .Include(s => s.Sender).ThenInclude(u => u.Photos)
      .Include(s => s.Recipient).ThenInclude(u => u.Photos)
      .Where(m => m.RecipientUserName == currentUserName
            && m.RecipientDeleted == false
            && m.SenderUserName == recipientUserName
          || m.RecipientUserName == recipientUserName
            && m.SenderDeleted == false
            && m.SenderUserName == currentUserName)
      .OrderBy(m => m.MessageSent)
      .ToListAsync();

    var unreadMessages = messages.Where(m => m.DateRead == null && m.RecipientUserName == currentUserName).ToList();

    if (unreadMessages.Any())
    {
      foreach (var message in unreadMessages)
      {
        message.DateRead = DateTime.UtcNow;
      }
      await _context.SaveChangesAsync();
    }

    return _mapper.Map<IEnumerable<MessageDto>>(messages);
  }

  public async Task<bool> SaveAllAsync()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}
