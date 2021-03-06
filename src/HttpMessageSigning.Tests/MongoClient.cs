﻿﻿using MongoDB.Driver;

 namespace Dalion.HttpMessageSigning {
    public class MongoClient {
        public static IMongoClient Create(string connectionString) {
            var mongoUrl = new MongoUrl(connectionString);
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
            return new MongoDB.Driver.MongoClient(mongoClientSettings);
        }
    }
}