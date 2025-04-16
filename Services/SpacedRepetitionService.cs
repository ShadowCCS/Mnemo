using MnemoProject.Models;
using System;

namespace MnemoProject.Services
{
    public static class SpacedRepetitionService
    {
        /// <summary>
        /// Applies the SuperMemo-2 algorithm to a flashcard deck based on the user's response.
        /// </summary>
        /// <param name="deck">The deck to update</param>
        /// <param name="quality">Quality of the response: 0=Again, 1=Hard, 3=Good, 5=Easy</param>
        public static void ApplySM2Algorithm(Deck deck, int quality)
        {
            // Ensure quality is between 0 and 5
            quality = Math.Max(0, Math.Min(5, quality));
            
            // Update ease factor
            // EF' = EF + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02))
            double newEaseFactor = deck.EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
            
            // Enforce minimum ease factor
            deck.EaseFactor = Math.Max(1.3, newEaseFactor);
            
            // Set today as the last review date
            deck.LastReviewDate = DateTime.Today;
            
            // Calculate the next interval based on the quality
            if (quality < 3)
            {
                // If quality is less than 3, reset the repetition count and interval
                deck.RepetitionCount = 0;
                deck.Interval = 1; // Review again tomorrow
            }
            else
            {
                // Successful review, calculate new interval
                deck.RepetitionCount++;
                
                if (deck.RepetitionCount == 1)
                {
                    deck.Interval = 1; // First successful review = 1 day
                }
                else if (deck.RepetitionCount == 2)
                {
                    deck.Interval = 6; // Second successful review = 6 days
                }
                else
                {
                    // For subsequent successful reviews, multiply the interval by the ease factor
                    deck.Interval = (int)Math.Round(deck.Interval * deck.EaseFactor);
                }
            }
            
            // Set the next review date
            deck.NextReviewDate = DateTime.Today.AddDays(deck.Interval);
        }
        
        /// <summary>
        /// Maps string difficulty ratings to SM-2 quality values
        /// </summary>
        public static int GetQualityFromDifficulty(string difficulty)
        {
            return difficulty.ToLower() switch
            {
                "again" => 0, // Complete blackout, repeat immediately
                "hard" => 1,  // Incorrect response, but upon seeing the answer it felt familiar
                "good" => 3,  // Correct response, with some difficulty
                "easy" => 5,  // Perfect response
                _ => 3        // Default to "Good"
            };
        }
    }
} 