namespace TrustFlow.Core.DTOs
{
    public class CreateChatRequest
    {
        public string ChatType { get; set; }
        public ChatMember[] Members { get; set; }
    }
}
