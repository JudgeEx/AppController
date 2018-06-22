using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppController.Utils
{
    public static class Misc
    {
        public static IMongoDatabase GetDatabase(IConfiguration configuration) => new MongoClient(configuration["dbConnectionString"]).GetDatabase(configuration["dbName"]);
        public static string Stringify(this object obj, string defaultValue = "") => obj == default ? defaultValue : obj.ToString();
    }
}
