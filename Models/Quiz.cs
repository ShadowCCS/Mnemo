using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MnemoProject.Models
{
    public class Quiz
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public List<Question> Questions { get; set; } = new();
    }

    public class Question
    {
        [Key]
        public Guid Id { get; set; }
        [ForeignKey("Quiz")]
        public Guid QuizId { get; set; }
        public string QuestionText { get; set; }
        public string CorrectAnswer { get; set; }
        public List<string> Options { get; set; } = new();
        public Quiz Quiz { get; set; }
    }
} 