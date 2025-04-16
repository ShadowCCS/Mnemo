using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MnemoProject.Models
{
    public class Flashcard
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public int CardCount { get; set; }
        
        public virtual ICollection<Deck> Decks { get; set; } = new List<Deck>();
    }
} 