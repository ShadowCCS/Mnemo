using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MnemoProject.Models
{
    public class LearningPath
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public List<Unit> Units { get; set; } = new();
    }


}
