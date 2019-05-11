using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatingApp.API.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsMain { get; set; }
        
        [Column("PuublicId")]
        public string PublicId { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
    }
}