using System;
using System.Reflection;
using CorrugatedIron;
using Tweets.Attributes;
using Tweets.ModelBuilding;
using Tweets.Models;
using CorrugatedIron.Models;

namespace Tweets.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string bucketName;
        private readonly IRiakClient riakClient;
        private readonly IMapper<User, UserDocument> userDocumentMapper;
        private readonly IMapper<UserDocument, User> userMapper;

        public UserRepository(IRiakClient riakClient, IMapper<User, UserDocument> userDocumentMapper, IMapper<UserDocument, User> userMapper)
        {
            this.riakClient = riakClient;
            this.userDocumentMapper = userDocumentMapper;
            this.userMapper = userMapper;
            bucketName = typeof (UserDocument).GetCustomAttribute<BucketNameAttribute>().BucketName;
        }

        public void Save(User user)
        {
            //TODO: Здесь нужно реализовать сохранение пользователя в Riak
            var riakUser = userDocumentMapper.Map(user);
            riakClient.Put(new RiakObject(bucketName, riakUser.Id, riakUser));
        }

        public User Get(string userName)
        {
            //TODO: Здесь нужно доставать пользователя из Riak
            var riakUser = riakClient.Get(bucketName, userName);
            if (riakUser.IsSuccess)
            {
                return userMapper.Map(riakUser.Value.GetObject<UserDocument>());
            }
            
            return null;
            
        }
    }
}