using System.IO;
using LiteDB;
using LiteDB.Engine;

namespace ChatGpt.Web.LiteDatabase
{
    public class LogLiteDatabase: LiteDB.LiteDatabase
    {
        /// <summary>
        /// Starts LiteDB database using a connection string for file system database
        /// </summary>
        public LogLiteDatabase(string connectionString, BsonMapper mapper = null) : base(connectionString, mapper)
        {
        }

        /// <summary>
        /// Starts LiteDB database using a connection string for file system database
        /// </summary>
        public LogLiteDatabase(ConnectionString connectionString, BsonMapper mapper = null) : base(connectionString, mapper)
        {
        }

        /// <summary>
        /// Starts LiteDB database using a generic Stream implementation (mostly MemoryStream).
        /// </summary>
        /// <param name="stream">DataStream reference </param>
        /// <param name="mapper">BsonMapper mapper reference</param>
        /// <param name="logStream">LogStream reference </param>
        public LogLiteDatabase(Stream stream, BsonMapper mapper = null, Stream logStream = null) : base(stream, mapper, logStream)
        {
        }

        /// <summary>
        /// Start LiteDB database using a pre-exiting engine. When LiteDatabase instance dispose engine instance will be disposed too
        /// </summary>
        public LogLiteDatabase(ILiteEngine engine, BsonMapper mapper = null, bool disposeOnClose = true) : base(engine, mapper, disposeOnClose)
        {
        }
    }
}
