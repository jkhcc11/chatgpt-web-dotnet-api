using MongoDB.Driver;

namespace ChatGpt.Web.MongoDB
{
    public class GptWebMongodbContext
    {
        public readonly IMongoDatabase Database;

        public GptWebMongodbContext(IMongoClient mongoClient, string database)
        {
            Database = mongoClient.GetDatabase(database);
        }
    }
}
