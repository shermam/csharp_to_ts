using System;
using System.Collections.Generic;
using System.Text;

namespace Connect.Services.Notification.ViewModel
{
    public enum NotificationDirection
    {
        auto,
        ltr,
        rtl
    };

    public partial class NotificationSaveModel
    {
        public string title { get; set; }
        private NotificationDirection dirEnum = NotificationDirection.auto;
        public string dir { get { return dirEnum.ToString(); } set { dirEnum = (NotificationDirection)Enum.Parse(typeof(NotificationDirection), value); } }
        public string lang { get; set; } = "";
        public string body { get; set; } = "";
        public string tag { get; set; } = "";
        public string image { get; set; }
        public string icon { get; set; }
        public string badge { get; set; }
        public int[] vibrate { get; set; }
        public long timestamp { get; set; }
        public bool renotify { get; set; } = false;
        public bool silent { get; set; } = false;
        public bool requireInteraction { get; set; } = false;
        public object data { get; set; } = null;
        public NotificationAction[] actions { get; set; } = new NotificationAction[] { };
    }


}
