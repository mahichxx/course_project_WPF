using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPF_FitnessClub.Models
{
    [Table("CoachClients")]
    public class CoachClient
    {
        [Key, Column(Order = 0)]
        [Required]
        public int CoachId { get; set; }

        [Key, Column(Order = 1)]
        [Required]
        public int ClientId { get; set; }

        [ForeignKey("CoachId")]
        public virtual User Coach { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;
    }
} 