using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using MongoDB.Driver;
using Tweets.ModelBuilding;
using Tweets.Models;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Tweets.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IMapper<Message, MessageDocument> messageDocumentMapper;
        private readonly MongoCollection<MessageDocument> messagesCollection;

        public MessageRepository(IMapper<Message, MessageDocument> messageDocumentMapper)
        {
            this.messageDocumentMapper = messageDocumentMapper;
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDb"].ConnectionString;
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            messagesCollection =
                new MongoClient(connectionString).GetServer().GetDatabase(databaseName).GetCollection<MessageDocument>(MessageDocument.CollectionName);
        }

        public void Save(Message message)
        {
            var messageDocument = messageDocumentMapper.Map(message);
            //TODO: Здесь нужно реализовать вставку сообщения в базу
            messagesCollection.Insert(messageDocument);
        }

        public void Like(Guid messageId, User user)
        {
            var likeDocument = new LikeDocument { UserName = user.Name, CreateDate = DateTime.UtcNow };
            //TODO: Здесь нужно реализовать вставку одобрения в базу
            var likeNotExist = Query.Not(Query<MessageDocument>.ElemMatch(md => md.Likes, x => Query<LikeDocument>.EQ(u => u.UserName, user.Name)));

            messagesCollection.Update(Query.And(
                Query<MessageDocument>.EQ(m => m.Id, messageId),
                likeNotExist),
                Update<MessageDocument>.Push(l => l.Likes, likeDocument)
                );
        }

        public void Dislike(Guid messageId, User user)
        {
            //TODO: Здесь нужно реализовать удаление одобрения из базы
            messagesCollection.Update(Query<MessageDocument>.EQ(m => m.Id, messageId),
                Update<MessageDocument>.Pull(md => md.Likes, m => m.EQ(u => u.UserName, user.Name)));
        }

        public IEnumerable<Message> GetPopularMessages()
        {
            //TODO: Здесь нужно возвращать 10 самых популярных сообщений
            //TODO: Важно сортировку выполнять на сервере
            //TODO: Тут будет полезен AggregationFramework
            var lim = new BsonDocument("$limit", 10);
            var unwind = new BsonDocument("$unwind", "$likes");
            var sort = new BsonDocument("$sort", new BsonDocument("likesNumber", -1));
            var project = new BsonDocument("$project", new BsonDocument {
                {"likes", new BsonDocument("$ifNull", new BsonArray(new BsonValue[] {"$likes", new BsonArray {BsonNull.Value}}))},
                {"userName", 1},
                {"text", 1},
                {"createDate", 1}

            });
            var group = new BsonDocument("$group", new BsonDocument
            {
                {"likesNumber", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray 
                {
                    new BsonDocument("$eq", new BsonArray{"$likes", BsonNull.Value}), 0, 1
                }))},
                {
                    "_id", new BsonDocument
                    {
                        {"userName", "$userName"},
                        {"_id", "$_id"},
                        {"text", "$text"},
                        {"createDate", "$createDate"}
                    }
                }
            });
            var result = messagesCollection.Aggregate(project, unwind, group, sort, lim).ResultDocuments.Select(
                doc =>
                {
                    var msg = BsonSerializer.Deserialize<MessageDocument>((BsonDocument)doc["_id"]);
                    var likeNum = (int)doc["likesNumber"];
                    return new Message
                    {
                        Id = msg.Id,
                        User = new User {Name = msg.UserName},
                        Text = msg.Text,
                        Likes = likeNum,
                        CreateDate = msg.CreateDate,
                    };
                });
            return result;
        }

        public IEnumerable<UserMessage> GetMessages(User user)
        {
            //TODO: Здесь нужно получать все сообщения конкретного пользователя
            return messagesCollection.Find(Query<MessageDocument>.EQ(u => u.UserName, user.Name))
                .Select(u => new UserMessage
                {
                    User = user,
                    Id = u.Id,
                    Text = u.Text,
                    CreateDate = u.CreateDate,
                    Likes = u.Likes == null ? 0 : u.Likes.Count(),
                    Liked = u.Likes != null && u.Likes.Select(usr => usr.UserName).Contains(user.Name)
                }).OrderByDescending(date => date.CreateDate);
        }
    }
}