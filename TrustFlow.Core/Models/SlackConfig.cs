using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class SlackConfig : BaseEntity
    {
        [BsonElement("SlackWebhookURL")]
        public string SlackWebhookURL { get; set; }

        [BsonElement("SlackAppName")]
        public string SlackAppName { get; set; }

        [BsonElement("SlackChannelName")]
        public string SlackChannelName { get; set; }

        [BsonElement("SlackBotToken")]
        public string SlackBotToken { get; set; }

        [BsonElement("SlackBotName")]
        public string SlackBotName { get; set; }

        [BsonElement("SlackBaseAddress")]
        public string SlackBaseAddress { get; set; }

        [BsonElement("IsActive")]
        public bool IsActive { get; set; }
    }
}
