using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq.Mapping;
using System.Linq;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly string connectionString;
        private readonly AttributeMappingSource mappingSource;
        private readonly IMapper<Message, MessageDocument> messageDocumentMapper;

        public MessageRepository(IMapper<Message, MessageDocument> messageDocumentMapper)
        {
            this.messageDocumentMapper = messageDocumentMapper;
            mappingSource = new AttributeMappingSource();
            connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;
        }

        public void Save(Message message)
        {
            var messageDocument = messageDocumentMapper.Map(message);
            //TODO: Здесь нужно реализовать вставку сообщения в базу
            using (var db = new TweetsDataContext(connectionString))
            {
                db.GetTable<MessageDocument>().InsertOnSubmit(messageDocument);
                db.SubmitChanges();
            }
        }

        public void Like(Guid messageId, User user)
        {
            var likeDocument = new LikeDocument { MessageId = messageId, UserName = user.Name, CreateDate = DateTime.UtcNow };
            //TODO: Здесь нужно реализовать вставку одобрения в базу
            using (var db = new TweetsDataContext(connectionString))
            {
                db.GetTable<LikeDocument>().InsertOnSubmit(likeDocument);
                db.SubmitChanges();
            }
        }

        public void Dislike(Guid messageId, User user)
        {
            //TODO: Здесь нужно реализовать удаление одобрения из базы
            using (var db = new TweetsDataContext(connectionString))
            {
                var likeDocument = db.GetTable<LikeDocument>()
                    .FirstOrDefault(like => like.MessageId == messageId && like.UserName == user.Name);
                if (likeDocument != null)
                {
                    db.GetTable<LikeDocument>().DeleteOnSubmit(likeDocument);
                    db.SubmitChanges();
                }
            }
        }

        public IEnumerable<Message> GetPopularMessages()
        {
            //TODO: Здесь нужно возвращать 10 самых популярных сообщений
            using (var db = new TweetsDataContext(connectionString))
            {
                var messages = db.GetTable<MessageDocument>();
                var likes = db.GetTable<LikeDocument>();

                var result = messages.GroupJoin(likes, message => message.Id, like => like.MessageId, (message, like) => new { message, like })
                    .SelectMany(
                        like => like.like.DefaultIfEmpty(),
                        (message, like) => new { message.message, userName = (like != null) ? like.UserName : null })
                    .GroupBy(m => m.message)
                    .Select(top => new Message()
                    {
                        Id = top.Key.Id,
                        Text = top.Key.Text,
                        Likes = top.Count(n => n.userName != null),
                        User = new User() { Name = top.Key.UserName },
                        CreateDate = top.Key.CreateDate
                    })
                    .OrderByDescending(l => l.Likes)
                    .Take(10)
                    .ToArray();

                return result;
            }
        }

        public IEnumerable<UserMessage> GetMessages(User user)
        {
            //TODO: Здесь нужно получать все сообщения конкретного пользователя
            using (var db = new TweetsDataContext(connectionString))
            {
                var messages = db.GetTable<MessageDocument>();
                var likes = db.GetTable<LikeDocument>();
                var result = messages.Where(message => message.UserName == user.Name)
                    .Select(likedMessages => new { likedMessages, n = likes.Count(m => m.MessageId.Equals(likedMessages.Id)) })
                    .Select(liked => new { liked, q = likes.Count(m => m.MessageId.Equals(liked.likedMessages.Id) && m.UserName.Equals(liked.likedMessages.UserName)) > 0 })
                    .OrderByDescending(l => l.liked.likedMessages.CreateDate)
                    .Select(m => new UserMessage()
                    {
                        Id = m.liked.likedMessages.Id,
                        Text = m.liked.likedMessages.Text,
                        User = new User { Name = m.liked.likedMessages.UserName },
                        Likes = m.liked.n,
                        Liked = m.q,
                        CreateDate = m.liked.likedMessages.CreateDate
                    })
                    .ToArray();
                return result;
            }
        }
    }
}