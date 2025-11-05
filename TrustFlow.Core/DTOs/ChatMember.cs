using System.Text.Json.Serialization;

namespace TrustFlow.Core.DTOs
{
    public class ChatMember
    {
        [JsonPropertyName("@odata.type")]
        public string ODataType { get; set; }

        public string[] Roles { get; set; }

        [JsonPropertyName("user@odata.bind")]
        public string UserBind { get; set; }
    }
}
