namespace TrustFlow.Core.Communication
{
    public class ServiceResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public object Result { get; set; }

        public ServiceResult(bool success,string message,object? result=null) {
            Success = success;
            Message = message;
            Result = result;
        }
        public ServiceResult(bool success, string message) { Success = success; Message = message; }
    }
}
