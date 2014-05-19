using System;
using ServiceStack.Redis;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly RedisClient redisClient;
        private readonly IMapper<User, UserDocument> userDocumentMapper;
        private readonly IMapper<UserDocument, User> userMapper;

        public UserRepository(RedisClient redisClient, IMapper<User, UserDocument> userDocumentMapper, IMapper<UserDocument, User> userMapper)
        {
            this.redisClient = redisClient;
            this.userDocumentMapper = userDocumentMapper;
            this.userMapper = userMapper;
        }

        public void Save(User user)
        {
            //TODO: Здесь нужно реализовать сохранение пользователя в Redis
            redisClient.As<UserDocument>();
            var usrDoc = userDocumentMapper.Map(user);
            redisClient.Set(usrDoc.Id, usrDoc);
            redisClient.Save();
        }

        public User Get(string userName)
        {
            //TODO: Здесь нужно доставать пользователя из Redis
            var user = redisClient.Get<UserDocument>(userName);
            return user == null ? null : userMapper.Map(user);
        }
    }
}