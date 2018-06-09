using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AppController.Utils
{
    public class AuthLib
    {
        static IKeyValueProvider<string, string> KV = new InMemoryKVProvider<string, string>();
        IMongoDatabase db;
        public AuthLib(IMongoDatabase db) => this.db = db;
        public bool Exists(string username)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("username", username);
            var count = db.GetCollection<BsonDocument>("users").Count(filter);
            return count > 0;
        }
        public bool AddUser(string username, string password)
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
            var dbItem = new BsonDocument
            {
                { "username" , username },
                { "secret", secret }
            };
            db.GetCollection<BsonDocument>("users").InsertOne(dbItem);
            return true;
        }
        public string AuthUser(string username, string password)
        {
            if (!ValidateUserName(username)) return default;
            var filter = Builders<BsonDocument>.Filter.Eq("username", username);
            var document = db.GetCollection<BsonDocument>("users").Find(filter).FirstAsync();
            var passwordHash1 = Crypto.SHA512Hash(Encoding.UTF8.GetBytes(password));
            string[] secretParts = document.Result["secret"].AsString.Split(':');
            var saltBytes = Convert.FromBase64String(secretParts[3]);
            if (passwordHash1.Length != saltBytes.Length) return default;
            var passwordHash2 = Crypto.SHA512Hash(passwordHash1.Zip(saltBytes, (b1, b2) => (byte)(b1 ^ b2)).ToArray());
            if (Convert.ToBase64String(passwordHash2) != secretParts[4]) return default;
            var sessionKey = Convert.ToBase64String(Crypto.SHA512Hash(Guid.NewGuid().ToByteArray()));
            KV.Set(sessionKey, username);
            return sessionKey;
        }
        public bool AuthUser(string sessionKey)
        {
            var ret = KV.Get(sessionKey);
            return ret != default && Exists(ret);
        }

        public static bool ValidateUserName(string username)
        {
            if (username.Length > 15 || username.Length < 3)
                return false;
            foreach (var ch in username)
                if (!Char.IsLetterOrDigit(ch))
                    return false;
            return true;
        }

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
