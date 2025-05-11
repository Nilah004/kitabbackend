using System;

namespace Try_application.Model
{
    public class BannerDto
    {
        public string Title { get; set; } = string.Empty;
        public string? SubTitle { get; set; }
        public string? LinkUrl { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
    }
}
