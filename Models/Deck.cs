using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MnemoProject.Models
{
    public class Deck
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public string Front { get; set; }
        
        [Required]
        public string Back { get; set; }
        
        [Required]
        public Guid FlashcardId { get; set; }
        
        [ForeignKey("FlashcardId")]
        public virtual Flashcard Flashcard { get; set; }
        
        // SM-2 Algorithm fields
        public double EaseFactor { get; set; } = 2.5; // Initial ease factor
        
        public int Interval { get; set; } = 0; // Current interval in days
        
        public int RepetitionCount { get; set; } = 0; // Number of successful repetitions
        
        public DateTime? LastReviewDate { get; set; } // When the card was last reviewed
        
        public DateTime? NextReviewDate { get; set; } // When the card is due for review
        
        // Returns true if the card is due for review
        [NotMapped]
        public bool IsDue => NextReviewDate == null || NextReviewDate.Value.Date <= DateTime.Today;
    }
} 