﻿using API.DTO;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface IMessageRepository
{
  void AddMessage(Message message);
  void DeleteMEssage(Message message);

  Task<Message> GetMessage(int id);
  Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams);
  Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string recipientUserName);
  Task<Boolean> SaveAllAsync();
}