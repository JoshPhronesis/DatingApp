using System;

namespace DatingApp.API.Dtos
{
    public class MessageToReturnDto
    {
         public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderKnowAs { get; set; }
        public string SenderPhotoUrl { get; set; }
        public int ReceipientId { get; set; }
        public string ReceipientKnowAs { get; set; }
        public string ReceipientPhotoUrl { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime?  DateRead { get; set; }
        public DateTime MessageSent { get; set; }

    }
}