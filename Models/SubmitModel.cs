using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppController.Models
{
    [BsonIgnoreExtraElements]
    public class Submit
    {
        public Guid SubmitId, UserId;
        public string ProblemId;
        public string Compiler;
        public string SourceCode;
        public string Status;
        class TestCaseStatus
        {
            string Status;
            ulong TimeInMicrosec;
            ulong MemoryUsageInByte;
            string AdditionalInfo;
        }
    }

    public class SubmitModel
    {
        private readonly IMongoCollection<Submit> table;

        public SubmitModel(IMongoDatabase db) => table = db.GetCollection<Submit>("submits");

        public bool Exist(Guid submitId) => table.Count(s => s.SubmitId == submitId) > 0;

        public void InsertSubmit(Submit submit) => table.InsertOne(submit);

        public Submit GetSubmit(Guid submitId) => table.Find(s => s.SubmitId == submitId).FirstOrDefault();

        public void UpdateSubmitStatus(Guid submitId, string newStatus)
            => table.FindOneAndUpdate(s => s.SubmitId == submitId, Builders<Submit>.Update.Set(s => s.Status, newStatus));
    }
}
