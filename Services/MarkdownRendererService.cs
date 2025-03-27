using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MnemoProject.Services
{
    public class MarkdownRendererService
    {
        private readonly FontFamily _customFont = (FontFamily)Avalonia.Application.Current.Resources["LexendFont"];

        public Control RenderMarkdown(string markdown)
        {
            var stackPanel = new StackPanel { Spacing = 0, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top};

            string[] lines = markdown.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToArray();

            bool inCodeBlock = false;
            var codeBlockLines = new List<string>();

            // For handling list items (we group consecutive list items)
            var listBuffer = new List<string>();
            bool inOrderedList = false;
            bool inUnorderedList = false;

            void FlushListBuffer()
            {
                if (listBuffer.Any())
                {
                    // Create a vertical StackPanel for list items.
                    var listPanel = new StackPanel { Margin = new Avalonia.Thickness(20, 0, 0, 1) };
                    for (int i = 0; i < listBuffer.Count; i++)
                    {
                        string listItem = listBuffer[i];
                        string prefix;
                        if (inOrderedList)
                        {
                            prefix = $"{i + 1}. ";
                        }
                        else // unordered
                        {
                            prefix = "• ";
                        }
                        listPanel.Children.Add(CreateFormattedParagraph(prefix + listItem));
                    }
                    stackPanel.Children.Add(listPanel);
                    listBuffer.Clear();
                    inOrderedList = inUnorderedList = false;
                }
            }

            foreach (var line in lines)
            {
                // Skip empty lines (but you could count them if you want double-space handling)
                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushListBuffer();
                    continue;
                }

                // Code Block detection (unchanged)
                if (line.StartsWith("```"))
                {
                    FlushListBuffer();
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlockLines.Clear();
                    }
                    else
                    {
                        inCodeBlock = false;
                        stackPanel.Children.Add(CreateCodeBlock(codeBlockLines));
                    }
                    continue;
                }
                if (inCodeBlock)
                {
                    codeBlockLines.Add(line);
                    continue;
                }

                // Header detection
                if (Regex.IsMatch(line, @"^#{1,6}\s"))
                {
                    FlushListBuffer();
                    int level = line.TakeWhile(c => c == '#').Count();
                    string headerText = line.Substring(level).Trim();
                    stackPanel.Children.Add(CreateHeader(headerText, level));
                }
                // Horizontal rule
                else if (Regex.IsMatch(line, @"^(-{3,}|\*{3,})$"))
                {
                    FlushListBuffer();
                    stackPanel.Children.Add(new Separator { Margin = new Avalonia.Thickness(0, 4, 0, 4) });
                }
                // Blockquote
                else if (line.StartsWith("> "))
                {
                    FlushListBuffer();
                    string quote = line.Substring(2);
                    var quoteBlock = CreateFormattedParagraph(quote);
                    quoteBlock.Background = Brushes.LightGray;
                    quoteBlock.Padding = new Avalonia.Thickness(10);
                    quoteBlock.Margin = new Avalonia.Thickness(10, 0, 10, 4);
                    stackPanel.Children.Add(quoteBlock);
                }
                // Unordered list with * or -
                else if (Regex.IsMatch(line, @"^(\*|-)\s"))
                {
                    inUnorderedList = true;
                    listBuffer.Add(line.Substring(2));
                }
                // Ordered list
                else if (Regex.IsMatch(line, @"^\d+\.\s"))
                {
                    inOrderedList = true;
                    string content = Regex.Replace(line, @"^\d+\.\s", "");
                    listBuffer.Add(content);
                }
                else
                {
                    FlushListBuffer();
                    stackPanel.Children.Add(CreateFormattedParagraph(line));
                }
            }


            // Flush at the end in case the markdown ends with a list
            FlushListBuffer();

            return new ScrollViewer { Content = stackPanel };
        }

        private TextBlock CreateHeader(string text, int level)
        {
            double fontSize = level switch
            {
                1 => 32,
                2 => 28,
                3 => 24,
                4 => 20,
                5 => 18,
                _ => 16
            };

            return new TextBlock
            {
                Text = text,
                FontFamily = _customFont,
                FontSize = fontSize,
                FontWeight = FontWeight.Bold,
                Margin = new Avalonia.Thickness(0, 2, 0, 1),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private TextBlock CreateCodeBlock(List<string> codeLines)
        {
            var codeBlock = new TextBlock
            {
                FontFamily = new FontFamily("Courier New"),
                Background = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 0, 0, 2),
                Padding = new Avalonia.Thickness(10)
            };

            codeBlock.Text = string.Join(Environment.NewLine, codeLines);
            return codeBlock;
        }

        private TextBlock CreateFormattedParagraph(string text)
        {
            var textBlock = new TextBlock
            {
                FontFamily = _customFont,
                FontSize = 18,
                FontWeight = FontWeight.Regular,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 34,
                Margin = new Avalonia.Thickness(0, 0, 0, 1)
            };

            ParseInline(text, textBlock);
            return textBlock;
        }

        /// <summary>
        /// Parses inline markdown for escapes, inline code, bold, italic, and links.
        /// </summary>
        private void ParseInline(string text, TextBlock textBlock)
        {
            // The regex covers:
            // - Escaped characters: \*
            // - Inline code: `code`
            // - Bold: **text** or __text__
            // - Italic: *text* or _text_
            // - Links: [text](url)
            var regex = new Regex(
                @"(?<escape>\\.)|" +
                @"(?<code>`(?<codetext>[^`]+)`)|" +
                @"(?<bold>(\*\*|__)(?<boldtext>.+?)(\*\*|__))|" +
                @"(?<italic>(\*|_)(?<italictext>.+?)(\*|_))|" +
                @"(?<link>\[(?<linktext>[^\]]+)\]\((?<linkurl>[^)]+)\))",
                RegexOptions.Compiled);

            int lastIndex = 0;
            foreach (Match match in regex.Matches(text))
            {
                if (match.Index > lastIndex)
                {
                    string plainText = text.Substring(lastIndex, match.Index - lastIndex);
                    textBlock.Inlines.Add(new Run(plainText));
                }

                if (match.Groups["escape"].Success)
                {
                    // Remove the backslash
                    string escaped = match.Value.Substring(1);
                    textBlock.Inlines.Add(new Run(escaped));
                }
                else if (match.Groups["code"].Success)
                {
                    string code = match.Groups["codetext"].Value;
                    textBlock.Inlines.Add(new Run(code)
                    {
                        FontFamily = new FontFamily("Courier New"),
                        Background = Brushes.LightGray
                    });
                }
                else if (match.Groups["bold"].Success)
                {
                    string boldText = match.Groups["boldtext"].Value;
                    textBlock.Inlines.Add(new Run(boldText)
                    {
                        FontWeight = FontWeight.ExtraBold
                    });
                }
                else if (match.Groups["italic"].Success)
                {
                    string italicText = match.Groups["italictext"].Value;
                    textBlock.Inlines.Add(new Run(italicText)
                    {
                        FontStyle = FontStyle.Italic
                    });
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                textBlock.Inlines.Add(new Run(text.Substring(lastIndex)));
            }
        }
    }
}
