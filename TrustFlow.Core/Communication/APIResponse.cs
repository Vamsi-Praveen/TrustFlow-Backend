namespace TrustFlow.Core.Communication
{
    public class APIResponse
    {
        public bool Success {get; set; }
        public string Message {get; set; }
        public object? Data { get; set; }


        public APIResponse(bool success,string message,object? data=null) {
            Success = success;
            Message = message;
            Data = data;
        }


    }
}
