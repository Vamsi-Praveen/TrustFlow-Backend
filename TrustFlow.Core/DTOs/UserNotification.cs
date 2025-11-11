namespace TrustFlow.Core.DTOs
{
    public class UserNotification
    {

        public string DefaultNotificationMethod { get; set; }

        public bool NotifyOnAssignedBug { get; set; } = true;

        public bool NotifyOnStatusChange { get; set; } = true;

        public bool NotifyOnNewComment { get; set; } = true;
    }
}
