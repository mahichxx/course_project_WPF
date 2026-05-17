using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPF_FitnessClub.Models
{
    [Table("WorkoutPlans")]
    public class WorkoutPlan
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ClientId { get; set; }
        
        [Required]
        public int CoachId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Title { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; }
        
        [Required]
        public DateTime UpdatedDate { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public bool IsCompleted { get; set; }
        
        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }
        
        [ForeignKey("CoachId")]
        public virtual User Coach { get; set; }
        
        [NotMapped]
        public int UserId 
        { 
            get => ClientId; 
            set => ClientId = value; 
        }
        
        [NotMapped]
        public int TrainerId 
        {
            get => CoachId;
            set => CoachId = value;
        }
        
        [NotMapped]
        public User User
        {
            get => Client;
            set => Client = value;
        }
        
        [NotMapped]
        public User Trainer
        {
            get => Coach;
            set => Coach = value;
        }
        
        [NotMapped]
        public string Status
        {
            get
            {
                if (IsCompleted)
                {
                    return "Выполнен";
                }
                
                if (DateTime.Now < StartDate)
                {
                    return "В ожидании";
                }
                
                if (DateTime.Now > EndDate)
                {
                    return "Истек";
                }
                
                return "В процессе";
            }
        }
        
        public WorkoutPlan()
        {
            CreatedDate = DateTime.Now;
            UpdatedDate = DateTime.Now;
            StartDate = DateTime.Now;
            EndDate = DateTime.Now.AddMonths(1);
            IsCompleted = false;
        }
    }
} 