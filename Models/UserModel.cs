using AppController.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace AppController.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        public Guid UserId;
        public string UserName;
        public string DisplayName;
        public string Secret;
        public string Motto;
    }

    public class UserModel
    {
        private readonly IMongoCollection<User> table;
        static IKeyValueProvider<(UInt64, UInt64), Guid> KV = new InMemoryKVProvider<(UInt64, UInt64), Guid>();

        public UserModel(IMongoDatabase db) => this.table = db.GetCollection<User>("users");

        public bool Exists(Guid userId) => table.Count(u => u.UserId == userId) > 0;

        public bool Exists(string username) => table.Count(u => u.UserName == username) > 0;

        public User AddUser(string username, string password)
        {
            var passwordHash1 = Crypto.SHA512Hash(Encoding.UTF8.GetBytes(password));
            var saltBytes = Crypto.GetRandomBytes(passwordHash1.Length);
            var passwordHash2 = Crypto.SHA512Hash(passwordHash1.Zip(saltBytes, (b1, b2) => (byte)(b1 ^ b2)).ToArray());
            var secret = String.Join(':', new string[]
            {
                "v1",
                "xor+sha512",
                username,
                Convert.ToBase64String(saltBytes),
                Convert.ToBase64String(passwordHash2)
            });
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                UserName = username,
                DisplayName = username,
                Secret = secret
            };
            table.InsertOne(newUser);
            return newUser;
        }

        public byte[] AuthUser(string username, string password)
        {
            if (!(ValidateUserName(username) && Exists(username))) return default;
            var document = table.Find(u => u.UserName == username).FirstAsync();
            var passwordHash1 = Crypto.SHA512Hash(Encoding.UTF8.GetBytes(password));
            string[] secretParts = document.Result.Secret.Split(':');
            var saltBytes = Convert.FromBase64String(secretParts[3]);
            if (passwordHash1.Length != saltBytes.Length) return default;
            var passwordHash2 = Crypto.SHA512Hash(passwordHash1.Zip(saltBytes, (b1, b2) => (byte)(b1 ^ b2)).ToArray());
            if (Convert.ToBase64String(passwordHash2) != secretParts[4]) return default;
            var sessionKeyBytes = Guid.NewGuid().ToByteArray();
            var sessionKey = (BitConverter.ToUInt64(sessionKeyBytes, 0), BitConverter.ToUInt64(sessionKeyBytes, 8));
            KV.Set(sessionKey, document.Result.UserId);
            return sessionKeyBytes;
        }

        public Guid AuthUser(byte[] sessionKey)
        {
            if (sessionKey.Length != 16) return default;
            var ret = KV.Get((BitConverter.ToUInt64(sessionKey, 0), BitConverter.ToUInt64(sessionKey, 8)));
            return (ret != default && Exists(ret)) ? ret : default;
        }

        public bool DeauthUser(byte[] sessionKey) => KV.Remove((BitConverter.ToUInt64(sessionKey, 0), BitConverter.ToUInt64(sessionKey, 8)));

        public User GetUserById(Guid guid) => table.Find(u => u.UserId == guid).First();

        public static bool ValidateUserName(string username)
        {
            if (username.Length < 4 || username.Length > 16)
                return false;
            foreach (var ch in username)
                if (!Char.IsLetterOrDigit(ch))
                    return false;
            return true;
        }

        public static bool ValidatePassword(string password) => password.Length >= 8 && password.Length <= 64;

        public static bool ValidateMailLiterally(string email)
        {
            try
            {
                MailAddress m = new MailAddress(email);
                return true;
            }
            catch { return false; }
        }
    }
}
