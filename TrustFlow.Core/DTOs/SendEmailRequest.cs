using System.ComponentModel.DataAnnotations;

namespace TrustFlow.Core.DTOs
{
    public class SendEmailRequest
    {
        [Required(ErrorMessage = "A 'To' email address is required.")]
        [EmailAddress(ErrorMessage = "The 'To' field must be a valid email address.")]
        public string To { get; set; } = string.Empty;

        [Required(ErrorMessage = "A subject is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Subject must be between 1 and 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "The email body is required.")]
        public string Body { get; set; } = string.Empty;

        public bool IsHtmlBody { get; set; } = true;
        public List<string>? Cc { get; set; } = new List<string>();

        public List<string>? Bcc { get; set; } = new List<string>();
        public List<EmailAttachment>? Attachments { get; set; } = new List<EmailAttachment>();
    }

    public class EmailAttachment
    {
        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
