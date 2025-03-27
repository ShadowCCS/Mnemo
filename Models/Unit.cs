using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MnemoProject.Models
{
    public class Unit
    {
        [Key]
        public Guid Id { get; set; }
        [ForeignKey("LearningPath")]
        public Guid LearningPathId { get; set; }
        public int UnitNumber { get; set; }
        public string Title { get; set; }
        public string TheoryContent { get; set; }

        public string UnitContent { get; set; } = string.Empty;
        public LearningPath LearningPath { get; set; }
    }



}
