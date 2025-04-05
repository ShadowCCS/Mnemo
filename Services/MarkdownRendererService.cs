using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia;
using Avalonia.Input;

namespace MnemoProject.Services
{
    public class MarkdownRendererService
    {
        private readonly FontFamily _customFont = (FontFamily)Avalonia.Application.Current.Resources["LexendFont"];
        private static readonly HttpClient _httpClient = new HttpClient();
        private Dictionary<string, string> _footnotes = new Dictionary<string, string>();

        // Define states for our state machine
        private enum ParserState
        {
            Normal,
            Escape,
            InCode,
            InBold,
            InItalic,
            InStrikethrough,
            InLinkText,
            InLinkUrl,
            InFootnote
        }

        public Control RenderMarkdown(string markdown)
        {
            _footnotes.Clear();
            var stackPanel = new StackPanel { Spacing = 0, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };

            string[] lines = markdown.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToArray();

            bool inCodeBlock = false;
            var codeBlockLines = new List<string>();
            string codeBlockLanguage = string.Empty;
            
            // First pass to collect footnotes
            for (int idx = 0; idx < lines.Length; idx++)
            {
                string line = lines[idx];
                if (line.StartsWith("[^") && line.Contains("]:"))
                {
                    var match = Regex.Match(line, @"^\[\^(.*?)\]:\s*(.*)$");
                    if (match.Success)
                    {
                        string id = match.Groups[1].Value;
                        string content = match.Groups[2].Value;
                        _footnotes[id] = content;
                    }
                }
            }

            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i];
                
                // Skip empty lines (but you could count them if you want double-space handling)
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                // Skip footnote definitions (we collected them in the first pass)
                if (line.StartsWith("[^") && line.Contains("]:"))
                {
                    i++;
                    continue;
                }

                // Code Block detection
                if (line.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlockLines.Clear();
                        
                        // Capture language if specified
                        codeBlockLanguage = line.Length > 3 ? line.Substring(3).Trim() : string.Empty;
                        i++;
                        continue;
                    }
                    else
                    {
                        // End of code block
                        inCodeBlock = false;
                        stackPanel.Children.Add(CreateCodeBlock(codeBlockLines, codeBlockLanguage));
                        codeBlockLanguage = string.Empty;
                        i++;
                        continue;
                    }
                }
                
                if (inCodeBlock)
                {
                    codeBlockLines.Add(line);
                    i++;
                    continue;
                }

                // Header detection
                if (Regex.IsMatch(line, @"^#{1,6}\s"))
                {
                    int level = line.TakeWhile(c => c == '#').Count();
                    string headerText = line.Substring(level).Trim();
                    stackPanel.Children.Add(CreateHeader(headerText, level));
                    i++;
                }
                // Horizontal rule
                else if (Regex.IsMatch(line, @"^(-{3,}|\*{3,})$"))
                {
                    // Create a styled horizontal rule
                    var hr = new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Color.FromRgb(80, 84, 92)),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        Margin = new Avalonia.Thickness(10, 15, 10, 15)
                    };
                    
                    stackPanel.Children.Add(hr);
                    i++;
                }
                // Blockquote
                else if (line.StartsWith("> "))
                {
                    var (quoteControl, newIndex) = ParseBlockquotes(lines, i);
                    stackPanel.Children.Add(quoteControl);
                    i = newIndex;
                }
                // Image detection
                else if (Regex.IsMatch(line, @"!\[.*?\]\(.*?\)"))
                {
                    var match = Regex.Match(line, @"!\[(.*?)\]\((.*?)\)");
                    if (match.Success)
                    {
                        string altText = match.Groups[1].Value;
                        string imageUrl = match.Groups[2].Value;
                        stackPanel.Children.Add(CreateImageElement(imageUrl, altText));
                    }
                    i++;
                }
                // Table detection
                else if (line.Contains("|") && i < lines.Length - 1 && lines[i + 1].Contains("|") && 
                          Regex.IsMatch(lines[i + 1], @"^\|?\s*[:\-]+\s*(?:\|[:\-]*\s*)*\|?$"))
                {
                    var (tableControl, newIndex) = ParseTable(lines, i);
                    stackPanel.Children.Add(tableControl);
                    i = newIndex;
                }
                // Definition list detection
                else if (i < lines.Length - 1 && lines[i+1].StartsWith(": "))
                {
                    var (definitionList, newIndex) = ParseDefinitionList(lines, i);
                    stackPanel.Children.Add(definitionList);
                    i = newIndex;
                }
                // Lists (ordered, unordered, and nested)
                else if (IsListItem(line))
                {
                    var (listItems, nextIndex) = ParseListItems(lines, i);
                    stackPanel.Children.Add(RenderListItems(listItems));
                    i = nextIndex;
                }
                // Task list items
                else if (IsTaskListItem(line))
                {
                    var (taskItems, nextIndex) = ParseTaskItems(lines, i);
                    stackPanel.Children.Add(RenderTaskItems(taskItems));
                    i = nextIndex;
                }
                else
                {
                    stackPanel.Children.Add(CreateFormattedParagraph(line));
                    i++;
                }
            }

            // Add footnotes section if any exist
            if (_footnotes.Any())
            {
                stackPanel.Children.Add(new Separator { Margin = new Avalonia.Thickness(0, 10, 0, 5) });
                stackPanel.Children.Add(CreateHeader("Footnotes", 2));
                
                foreach (var footnote in _footnotes)
                {
                    var footnoteLine = $"[^{footnote.Key}]: {footnote.Value}";
                    var footnotePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
                    
                    var footnoteBlock = CreateFormattedParagraph(footnoteLine);
                    footnoteBlock.Margin = new Avalonia.Thickness(0, 2, 0, 2);
                    
                    stackPanel.Children.Add(footnoteBlock);
                }
            }

            return new ScrollViewer { Content = stackPanel };
        }

        private (Control, int) ParseTable(string[] lines, int startIndex)
        {
            var tableRows = new List<string[]>();
            int currentIndex = startIndex;
            bool hasHeader = false;
            
            // Process the header row
            string headerRow = lines[currentIndex].Trim();
            var headerCells = ProcessTableRow(headerRow);
            tableRows.Add(headerCells);
            currentIndex++;
            
            // Process the delimiter row (skip it from the data)
            if (currentIndex < lines.Length && Regex.IsMatch(lines[currentIndex], @"^\|?\s*[:\-]+\s*(?:\|[:\-]*\s*)*\|?$"))
            {
                hasHeader = true;
                currentIndex++;
            }
            
            // Process data rows
            while (currentIndex < lines.Length)
            {
                string line = lines[currentIndex].Trim();
                if (!line.Contains("|") || string.IsNullOrWhiteSpace(line))
                    break;
                
                var cells = ProcessTableRow(line);
                tableRows.Add(cells);
                currentIndex++;
            }
            
            // Create modern dark-themed table UI
            var grid = new Grid { 
                Margin = new Avalonia.Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(30, 34, 42))
            };
            
            // Define columns
            int columnCount = tableRows.Count > 0 ? tableRows[0].Length : 0;
            for (int colIdx = 0; colIdx < columnCount; colIdx++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Avalonia.Controls.GridLength(1, Avalonia.Controls.GridUnitType.Star) });
            }
            
            // Add rows
            for (int rowIndex = 0; rowIndex < tableRows.Count; rowIndex++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = Avalonia.Controls.GridLength.Auto });
                var rowData = tableRows[rowIndex];
                
                for (int colIndex = 0; colIndex < rowData.Length && colIndex < columnCount; colIndex++)
                {
                    // Instead of using CreateFormattedParagraph directly, we'll create a new TextBlock
                    // and apply ParseInline to it to handle formatting within cells
                    var cellContent = new TextBlock
                    {
                        FontFamily = _customFont,
                        FontSize = 16,
                        TextWrapping = TextWrapping.Wrap,
                        Padding = new Avalonia.Thickness(8, 6, 8, 6),
                        Margin = new Avalonia.Thickness(0),
                        Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
                    };
                    
                    // Parse the cell content for markdown formatting
                    ParseInline(rowData[colIndex], cellContent);
                    
                    // Create border for cell with modern styling
                    var border = new Border
                    {
                        Child = cellContent,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(60, 64, 72)),
                        BorderThickness = new Avalonia.Thickness(0, 0, 1, 1),
                        Padding = new Avalonia.Thickness(0),
                        Margin = new Avalonia.Thickness(0)
                    };
                    
                    // Add styling for header row
                    if (hasHeader && rowIndex == 0)
                    {
                        // For header cells, we'll apply bold to the whole cell
                        cellContent.FontWeight = FontWeight.Bold;
                        border.Background = new SolidColorBrush(Color.FromRgb(50, 54, 62));
                        // Remove bottom border for header cells
                        border.BorderThickness = new Avalonia.Thickness(0, 0, 1, 0);
                    }
                    else
                    {
                        // Alternate row colors for better readability
                        if (rowIndex % 2 == 0)
                        {
                            border.Background = new SolidColorBrush(Color.FromRgb(37, 41, 49));
                        }
                        else
                        {
                            border.Background = new SolidColorBrush(Color.FromRgb(30, 34, 42));
                        }
                    }
                    
                    Grid.SetRow(border, rowIndex);
                    Grid.SetColumn(border, colIndex);
                    grid.Children.Add(border);
                }
            }
            
            // Wrap the grid in a border for a cleaner look
            var tableBorder = new Border
            {
                Child = grid,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 64, 72)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                ClipToBounds = true,
                Padding = new Avalonia.Thickness(0),
                Margin = new Avalonia.Thickness(0, 10, 0, 10) // Keep vertical spacing between elements
            };
            
            return (tableBorder, currentIndex);
        }

        private string[] ProcessTableRow(string row)
        {
            // Remove optional leading and trailing pipe
            row = row.Trim();
            if (row.StartsWith("|")) row = row.Substring(1);
            if (row.EndsWith("|")) row = row.Substring(0, row.Length - 1);
            
            // Split by pipe but account for escaped pipes
            var cells = new List<string>();
            int startPos = 0;
            bool isEscaped = false;
            
            for (int idx = 0; idx < row.Length; idx++)
            {
                if (row[idx] == '\\' && idx + 1 < row.Length && row[idx + 1] == '|')
                {
                    isEscaped = true;
                    idx++; // Skip the backslash
                    continue;
                }
                
                if (row[idx] == '|' && !isEscaped)
                {
                    cells.Add(row.Substring(startPos, idx - startPos).Trim());
                    startPos = idx + 1;
                }
                
                isEscaped = false;
            }
            
            cells.Add(row.Substring(startPos).Trim());
            return cells.ToArray();
        }

        private (Control, int) ParseDefinitionList(string[] lines, int startIndex)
        {
            var definitionPanel = new StackPanel 
            { 
                Margin = new Avalonia.Thickness(0, 5, 0, 10),
                Spacing = 5
            };
            
            int currentIndex = startIndex;
            
            while (currentIndex < lines.Length)
            {
                // Must have at least a term line and a definition line
                if (currentIndex + 1 >= lines.Length || !lines[currentIndex + 1].StartsWith(": "))
                    break;
                
                string term = lines[currentIndex].Trim();
                
                // Skip empty term
                if (string.IsNullOrWhiteSpace(term))
                {
                    currentIndex++;
                    continue;
                }
                
                var termBlock = CreateFormattedParagraph(term);
                termBlock.FontWeight = FontWeight.Bold;
                definitionPanel.Children.Add(termBlock);
                
                currentIndex++;
                
                // Process all consecutive definition lines for this term
                while (currentIndex < lines.Length && lines[currentIndex].StartsWith(": "))
                {
                    string definition = lines[currentIndex].Substring(2).Trim();
                    var definitionBlock = CreateFormattedParagraph(definition);
                    definitionBlock.Margin = new Avalonia.Thickness(20, 0, 0, 0);
                    definitionPanel.Children.Add(definitionBlock);
                    
                    currentIndex++;
                }
            }
            
            return (definitionPanel, currentIndex);
        }

        private bool IsListItem(string line)
        {
            return Regex.IsMatch(line, @"^\s*(\*|-|\d+\.)\s");
        }

        private bool IsTaskListItem(string line)
        {
            return Regex.IsMatch(line, @"^\s*(\*|-|\d+\.)\s\[([ xX])\]\s");
        }

        private (List<ListItem>, int) ParseListItems(string[] lines, int startIndex)
        {
            var result = new List<ListItem>();
            int currentIndex = startIndex;
            
            while (currentIndex < lines.Length)
            {
                string line = lines[currentIndex];
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    currentIndex++;
                    // An empty line usually terminates a list, unless the next line is also a list item
                    if (currentIndex < lines.Length && !IsListItem(lines[currentIndex]))
                        break;
                    continue;
                }
                
                if (!IsListItem(line))
                    break;
                
                // Calculate indentation level
                int indentation = line.TakeWhile(char.IsWhiteSpace).Count();
                
                // Extract list marker (* or - or number.)
                var match = Regex.Match(line, @"^\s*(\*|-|\d+\.)\s");
                bool isOrdered = match.Value.Contains('.');
                
                // Extract content (remove marker)
                string content = line.Substring(match.Index + match.Length);
                
                var listItem = new ListItem
                {
                    Content = content,
                    Indentation = indentation,
                    IsOrdered = isOrdered,
                    NestedItems = new List<ListItem>()
                };
                
                currentIndex++;
                
                // Check for a paragraph continuation or nested content
                List<string> continuationLines = new List<string>();
                
                // Look ahead for paragraph continuation or nested items
                while (currentIndex < lines.Length)
                {
                    string nextLine = lines[currentIndex];
                    
                    // Empty line terminates the current paragraph unless followed by an indented line
                    if (string.IsNullOrWhiteSpace(nextLine))
                    {
                        currentIndex++;
                        // If the line after empty line is more indented than the list item, 
                        // it's a continuation or a nested non-list content
                        if (currentIndex < lines.Length)
                        {
                            string afterEmptyLine = lines[currentIndex];
                            int afterEmptyIndent = afterEmptyLine.TakeWhile(char.IsWhiteSpace).Count();
                            if (afterEmptyIndent > indentation && !IsListItem(afterEmptyLine))
                            {
                                // This is a continuation paragraph, collect it
                                continuationLines.Add(afterEmptyLine.Trim());
                                currentIndex++;
                                continue;
                            }
                        }
                        break;
                    }
                    
                    int nextIndentation = nextLine.TakeWhile(char.IsWhiteSpace).Count();
                    
                    // If more indented but not a list item, it's a continuation
                    if (nextIndentation > indentation && !IsListItem(nextLine))
                    {
                        continuationLines.Add(nextLine.Trim());
                        currentIndex++;
                        continue;
                    }
                    // If it's a list item with more indentation, it's a nested list
                    else if (IsListItem(nextLine) && nextIndentation > indentation)
                    {
                        var (nestedItems, newIndex) = ParseListItems(lines, currentIndex);
                        listItem.NestedItems.AddRange(nestedItems);
                        currentIndex = newIndex;
                    }
                    else
                    {
                        break; // Same level or less indentation, not part of this item
                    }
                }
                
                // Add continuation paragraphs if any
                if (continuationLines.Count > 0)
                {
                    listItem.ContinuationContent = string.Join("\n", continuationLines);
                }
                
                result.Add(listItem);
            }
            
            return (result, currentIndex);
        }

        private (List<TaskItem>, int) ParseTaskItems(string[] lines, int startIndex)
        {
            var result = new List<TaskItem>();
            int currentIndex = startIndex;
            
            while (currentIndex < lines.Length)
            {
                string line = lines[currentIndex];
                
                if (string.IsNullOrWhiteSpace(line) || !IsTaskListItem(line))
                    break;
                
                // Extract checkbox state
                var match = Regex.Match(line, @"^\s*(\*|-|\d+\.)\s\[([ xX])\]\s");
                bool isChecked = match.Groups[2].Value.ToLower() == "x";
                
                // Extract content
                string content = line.Substring(match.Index + match.Length);
                
                result.Add(new TaskItem { 
                    Content = content, 
                    IsChecked = isChecked 
                });
                
                currentIndex++;
            }
            
            return (result, currentIndex);
        }

        private Control RenderListItems(List<ListItem> items)
        {
            var listPanel = new StackPanel { Margin = new Avalonia.Thickness(5, 0, 0, 5) };
            
            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                string prefix;
                
                if (item.IsOrdered)
                {
                    prefix = $"{idx + 1}. ";
                }
                else // unordered
                {
                    prefix = "• ";
                }
                
                var itemPanel = new StackPanel { Spacing = 4 };
                
                // Main content
                var itemContent = CreateFormattedParagraph(prefix + item.Content);
                itemPanel.Children.Add(itemContent);
                
                // Continuation content (additional paragraphs)
                if (!string.IsNullOrEmpty(item.ContinuationContent))
                {
                    var continuationPanel = new StackPanel { Margin = new Avalonia.Thickness(15, 0, 0, 0) };
                    var paragraphs = item.ContinuationContent.Split('\n');
                    
                    foreach (var paragraph in paragraphs)
                    {
                        continuationPanel.Children.Add(CreateFormattedParagraph(paragraph));
                    }
                    
                    itemPanel.Children.Add(continuationPanel);
                }
                
                // Render nested items if any
                if (item.NestedItems.Any())
                {
                    var nestedListPanel = RenderListItems(item.NestedItems);
                    nestedListPanel.Margin = new Avalonia.Thickness(20, 0, 0, 0);
                    itemPanel.Children.Add(nestedListPanel);
                }
                
                listPanel.Children.Add(itemPanel);
            }
            
            return listPanel;
        }

        private Control RenderTaskItems(List<TaskItem> items)
        {
            var taskPanel = new StackPanel { 
                Margin = new Avalonia.Thickness(5, 0, 0, 5),
                Spacing = 6 // Add spacing between tasks
            };
            
            foreach (var item in items)
            {
                var itemPanel = new StackPanel { 
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 8 // Add spacing between checkbox and text
                };
                
                // Create a checkbox with proper styling
                var checkbox = new CheckBox { 
                    IsChecked = item.IsChecked,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    Margin = new Avalonia.Thickness(0, 2, 0, 0) // Align better with text
                };
                
                // Style the text differently if checked
                var content = new TextBlock
                {
                    FontFamily = _customFont,
                    FontSize = 16,
                    FontWeight = FontWeight.Regular,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                
                // Parse and apply markdown formatting to the task content
                ParseInline(item.Content, content);
                
                // Set up the checkbox state change handler
                checkbox.IsCheckedChanged += (sender, e) =>
                {
                    // Update the visual appearance when checkbox state changes
                    if (checkbox.IsChecked == true)
                    {
                        // Apply strikethrough when checked
                        content.TextDecorations = TextDecorations.Strikethrough;
                        content.Opacity = 0.7; // Dim the text
                    }
                    else
                    {
                        // Remove strikethrough when unchecked
                        content.TextDecorations = null;
                        content.Opacity = 1.0;
                    }
                };
                
                // Set initial styling based on checked state
                if (item.IsChecked)
                {
                    content.TextDecorations = TextDecorations.Strikethrough;
                    content.Opacity = 0.7;
                }
                
                // Add the checkbox and content to the item panel
                itemPanel.Children.Add(checkbox);
                itemPanel.Children.Add(content);
                
                // Add the item panel to the task panel
                taskPanel.Children.Add(itemPanel);
            }
            
            return taskPanel;
        }

        private Control CreateImageElement(string imageUrl, string altText)
        {
            try
            {
                var image = new Image { 
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.DownOnly,
                    MaxWidth = 600
                };
                
                // For local files
                if (File.Exists(imageUrl))
                {
                    image.Source = new Bitmap(imageUrl);
                    
                    return new Panel
                    {
                        Children = { image },
                        Margin = new Avalonia.Thickness(0, 5, 0, 5)
                    };
                }
                // For remote URLs
                else if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri uri))
                {
                    // Create a placeholder panel that we'll return immediately
                    var panel = new Panel
                    {
                        Margin = new Avalonia.Thickness(0, 5, 0, 5)
                    };
                    
                    // Add a loading text that will be replaced once the image loads
                    var loadingText = new TextBlock
                    {
                        Text = $"Loading image: {altText}...",
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyle.Italic
                    };
                    panel.Children.Add(loadingText);
                    
                    // Start loading the image asynchronously
                    LoadImageAsync(uri, image, panel, loadingText, altText);
                    
                    return panel;
                }
                else
                {
                    return new TextBlock
                    {
                        Text = $"[Image: {altText}] (Invalid URL: {imageUrl})",
                        Foreground = Brushes.Red,
                        FontStyle = FontStyle.Italic
                    };
                }
            }
            catch (Exception ex)
            {
                return new TextBlock
                {
                    Text = $"[Image: {altText}] (Error: {ex.Message})",
                    Foreground = Brushes.Red,
                    FontStyle = FontStyle.Italic
                };
            }
        }
        
        private async void LoadImageAsync(Uri uri, Image image, Panel panel, TextBlock loadingText, string altText)
        {
            try
            {
                // Load the image asynchronously
                using (var httpClientHandler = new HttpClientHandler())
                {
                    // Handle SSL certificate issues
                    httpClientHandler.ServerCertificateCustomValidationCallback = 
                        (message, cert, chain, errors) => true; // Accept all certificates
                        
                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        // Set timeout to prevent hanging
                        httpClient.Timeout = TimeSpan.FromSeconds(10);
                        
                        using (var response = await httpClient.GetAsync(uri))
                        {
                            response.EnsureSuccessStatusCode();
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                // Create a memory stream to keep the data around after the response is disposed
                                var memoryStream = new MemoryStream();
                                await stream.CopyToAsync(memoryStream);
                                memoryStream.Position = 0;
                                
                                // Set image source
                                image.Source = new Bitmap(memoryStream);
                                
                                // Replace loading text with the actual image
                                panel.Children.Remove(loadingText);
                                panel.Children.Add(image);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Replace loading text with error message
                panel.Children.Remove(loadingText);
                panel.Children.Add(new TextBlock
                {
                    Text = $"[Image: {altText}] (Failed to load: {ex.Message})",
                    Foreground = Brushes.Red,
                    FontStyle = FontStyle.Italic
                });
            }
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

        private Control CreateCodeBlock(List<string> codeLines, string language)
        {
            // Create a modern dark-themed code block container
            var codePanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 34, 42)),
                Margin = new Avalonia.Thickness(0), // Remove extra margin
                Spacing = 0 // Remove spacing between elements
            };

            // Add language header with modern styling if specified
            if (!string.IsNullOrEmpty(language))
            {
                var headerPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Background = new SolidColorBrush(Color.FromRgb(40, 44, 52))
                };
                
                var languageHeader = new TextBlock
                {
                    Text = language,
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 220, 254)), // Light blue for language name
                    Padding = new Avalonia.Thickness(15, 8, 15, 8),
                };
                
                headerPanel.Children.Add(languageHeader);
                codePanel.Children.Add(headerPanel);
            }

            // Create a container for line numbers and code text
            var codeContainer = new Grid
            {
                Margin = new Avalonia.Thickness(0)
            };
            
            // Add two columns - one for line numbers, one for code
            codeContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            codeContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            // Create line numbers panel if there's more than one line
            if (codeLines.Count > 1)
            {
                var lineNumbersPanel = new StackPanel
                {
                    Background = new SolidColorBrush(Color.FromRgb(40, 44, 52)),
                    Margin = new Avalonia.Thickness(0),
                    MinWidth = 30,
                    Spacing = 0 // Remove spacing between line numbers
                };

                // Generate line numbers
                for (int i = 0; i < codeLines.Count; i++)
                {
                    lineNumbersPanel.Children.Add(new TextBlock
                    {
                        Text = (i + 1).ToString(),
                        FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)), // Gray for line numbers
                        TextAlignment = TextAlignment.Right,
                        Padding = new Avalonia.Thickness(5, 2, 10, 2)
                    });
                }

                Grid.SetColumn(lineNumbersPanel, 0);
                codeContainer.Children.Add(lineNumbersPanel);
            }

            // Create the main code content
            var codeBlock = new TextBlock
            {
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 14,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Avalonia.Thickness(15, 10, 15, 10),
                Foreground = Brushes.White,
                LineHeight = 20
            };

            // Join the code lines preserving line breaks
            codeBlock.Text = string.Join(Environment.NewLine, codeLines);
            
            // Set the code block in the second column
            Grid.SetColumn(codeBlock, 1);
            codeContainer.Children.Add(codeBlock);

            // Create a scrollable container for the code with exact sizing
            var scrollContainer = new ScrollViewer
            {
                Content = codeContainer,
                MaxHeight = 400,
                Padding = new Avalonia.Thickness(0),
                Margin = new Avalonia.Thickness(0)
            };

            codePanel.Children.Add(scrollContainer);

            // Wrap everything in a nice border that fits tightly
            return new Border
            {
                Child = codePanel,
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 54, 62)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                ClipToBounds = true,
                Padding = new Avalonia.Thickness(0),
                Margin = new Avalonia.Thickness(0, 10, 0, 10) // Keep vertical margins for spacing between blocks
            };
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
        /// Parses inline markdown for escapes, inline code, bold, italic, links, strikethrough, and footnotes.
        /// </summary>
        private void ParseInline(string text, TextBlock textBlock)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var state = ParserState.Normal;
            var currentBuffer = new System.Text.StringBuilder();
            string linkText = string.Empty;
            bool usedAsteriskForBold = false;
            bool usedAsteriskForItalic = false;
            
            // Track combined formatting
            bool isCurrentlyBold = false;
            bool isCurrentlyItalic = false;
            
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                char? nextChar = (i + 1 < text.Length) ? text[i + 1] : (char?)null;
                char? nextNextChar = (i + 2 < text.Length) ? text[i + 2] : (char?)null;
                
                switch (state)
                {
                    case ParserState.Normal:
                        // Check for the ***text*** pattern (3 asterisks for combined bold+italic)
                        if (c == '*' && nextChar.HasValue && nextChar.Value == '*' && 
                            nextNextChar.HasValue && nextNextChar.Value == '*')
                        {
                            // ***text*** -> bold and italic with asterisks
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            isCurrentlyBold = isCurrentlyItalic = true;
                            i += 2; // Skip to after the third asterisk
                        }
                        // Combined bold and italic - handle as a special case
                        else if (c == '*' && nextChar.HasValue && nextChar.Value == '*' && 
                            i + 2 < text.Length && text[i + 2] == '*' && i + 3 < text.Length && text[i + 3] == '*')
                        {
                            // ****text**** -> bold and italic with **
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            isCurrentlyBold = isCurrentlyItalic = true;
                            i += 3; // Skip all four asterisks
                        }
                        else if (c == '_' && nextChar.HasValue && nextChar.Value == '_' && 
                                 i + 2 < text.Length && text[i + 2] == '_' && i + 3 < text.Length && text[i + 3] == '_')
                        {
                            // ____text____ -> bold and italic with __
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            isCurrentlyBold = isCurrentlyItalic = true;
                            i += 3; // Skip all four underscores
                        }
                        else if (c == '*' && nextChar.HasValue && nextChar.Value == '*' && 
                                 i + 2 < text.Length && text[i + 2] == '_')
                        {
                            // **_text_** -> bold with ** and italic with _
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            isCurrentlyBold = isCurrentlyItalic = true;
                            i += 2; // Skip ** and _
                        }
                        else if (c == '_' && nextChar.HasValue && nextChar.Value == '*' && 
                                 i + 2 < text.Length && text[i + 2] == '*')
                        {
                            // _**text**_ -> bold with ** and italic with _
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            isCurrentlyBold = isCurrentlyItalic = true;
                            i += 2; // Skip _ and **
                        }
                        else if (c == '*' && nextChar.HasValue && nextChar.Value == '_' && 
                                 i + 2 < text.Length && text[i + 2] == '_')
                        {
                            // *__text__* -> bold with __ and italic with *
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            isCurrentlyBold = isCurrentlyItalic = true;
                            i += 2; // Skip * and __
                        }
                        else if (c == '_' && nextChar.HasValue && nextChar.Value == '_' && 
                                 i + 2 < text.Length && text[i + 2] == '*')
                        {
                            // __*text*__ -> bold with __ and italic with *
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            isCurrentlyBold = isCurrentlyItalic = true;
                            i += 2; // Skip __ and *
                        }
                        // Check for end of combined formatting
                        else if (isCurrentlyBold && isCurrentlyItalic)
                        {
                            // Check for ***text*** ending
                            if (c == '*' && nextChar.HasValue && nextChar.Value == '*' && 
                                nextNextChar.HasValue && nextNextChar.Value == '*')
                            {
                                var run = new Run(currentBuffer.ToString())
                                {
                                    FontWeight = FontWeight.ExtraBold,
                                    FontStyle = FontStyle.Italic
                                };
                                textBlock.Inlines.Add(run);
                                currentBuffer.Clear();
                                isCurrentlyBold = isCurrentlyItalic = false;
                                i += 2; // Skip the next two *
                            }
                            // Check for other combined format endings
                            else if ((c == '*' && nextChar.HasValue && nextChar.Value == '*') ||
                                    (c == '_' && nextChar.HasValue && nextChar.Value == '_'))
                            {
                                var run = new Run(currentBuffer.ToString())
                                {
                                    FontWeight = FontWeight.ExtraBold,
                                    FontStyle = FontStyle.Italic
                                };
                                textBlock.Inlines.Add(run);
                                currentBuffer.Clear();
                                isCurrentlyBold = isCurrentlyItalic = false;
                                i++; // Skip the next character which is part of the closing formatting
                            }
                            else
                            {
                                currentBuffer.Append(c);
                            }
                        }
                        // Regular formatting checks...
                        else if (c == '\\' && nextChar.HasValue && IsSpecialChar(nextChar.Value))
                        {
                            // Escaped character
                            state = ParserState.Escape;
                            i++;
                        }
                        else if (c == '`')
                        {
                            // Start code
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.InCode;
                        }
                        else if (c == '*' && nextChar.HasValue && nextChar.Value == '*')
                        {
                            // Start bold with **
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.InBold;
                            usedAsteriskForBold = true;
                            i++;
                        }
                        else if (c == '_' && nextChar.HasValue && nextChar.Value == '_')
                        {
                            // Start bold with __
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.InBold;
                            usedAsteriskForBold = false;
                            i++;
                        }
                        else if (c == '*' && (i == 0 || !IsAlphanumeric(text[i - 1])))
                        {
                            // Start italic with *
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.InItalic;
                            usedAsteriskForItalic = true;
                        }
                        else if (c == '_' && (i == 0 || !IsAlphanumeric(text[i - 1])))
                        {
                            // Start italic with _
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.InItalic;
                            usedAsteriskForItalic = false;
                        }
                        else if (c == '~' && nextChar.HasValue && nextChar.Value == '~')
                        {
                            // Start strikethrough
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.InStrikethrough;
                            i++;
                        }
                        else if (c == '[' && nextChar.HasValue && nextChar.Value != '^')
                        {
                            // Start link text
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.InLinkText;
                        }
                        else if (c == '[' && nextChar.HasValue && nextChar.Value == '^')
                        {
                            // Start footnote
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.InFootnote;
                            i++; // Skip the ^
                        }
                        else
                        {
                            currentBuffer.Append(c);
                        }
                        break;
                        
                    case ParserState.Escape:
                        // Just add the escaped character
                        currentBuffer.Append(c);
                        state = ParserState.Normal;
                        break;
                        
                    case ParserState.InCode:
                        if (c == '`')
                        {
                            // End code
                            textBlock.Inlines.Add(new Run(currentBuffer.ToString())
                            {
                                FontFamily = new FontFamily("Courier New"),
                                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                                Foreground = Brushes.White
                            });
                            currentBuffer.Clear();
                            state = ParserState.Normal;
                        }
                        else
                        {
                            currentBuffer.Append(c);
                        }
                        break;
                        
                    case ParserState.InBold:
                        if ((usedAsteriskForBold && c == '*' && nextChar.HasValue && nextChar.Value == '*') ||
                            (!usedAsteriskForBold && c == '_' && nextChar.HasValue && nextChar.Value == '_'))
                        {
                            // End bold
                            textBlock.Inlines.Add(new Run(currentBuffer.ToString())
                            {
                                FontWeight = FontWeight.ExtraBold
                            });
                            currentBuffer.Clear();
                            state = ParserState.Normal;
                            i++;
                        }
                        else
                        {
                            currentBuffer.Append(c);
                        }
                        break;
                        
                    case ParserState.InItalic:
                        if ((usedAsteriskForItalic && c == '*' && (i == text.Length - 1 || (nextChar.HasValue && !IsAlphanumeric(nextChar.Value)))) ||
                            (!usedAsteriskForItalic && c == '_' && (i == text.Length - 1 || (nextChar.HasValue && !IsAlphanumeric(nextChar.Value)))))
                        {
                            // End italic
                            textBlock.Inlines.Add(new Run(currentBuffer.ToString())
                            {
                                FontStyle = FontStyle.Italic
                            });
                            currentBuffer.Clear();
                            state = ParserState.Normal;
                        }
                        else
                        {
                            currentBuffer.Append(c);
                        }
                        break;
                        
                    case ParserState.InStrikethrough:
                        if (c == '~' && nextChar.HasValue && nextChar.Value == '~')
                        {
                            // End strikethrough
                            textBlock.Inlines.Add(new Run(currentBuffer.ToString())
                            {
                                TextDecorations = TextDecorations.Strikethrough
                            });
                            currentBuffer.Clear();
                            state = ParserState.Normal;
                            i++;
                        }
                        else
                        {
                            currentBuffer.Append(c);
                        }
                        break;
                        
                    case ParserState.InLinkText:
                        if (c == ']' && nextChar.HasValue && nextChar.Value == '(')
                        {
                            // End link text, start URL
                            linkText = currentBuffer.ToString();
                            currentBuffer.Clear();
                            state = ParserState.InLinkUrl;
                            i++;
                        }
                        else if (c == ']')
                        {
                            // Malformed link - no URL part
                            currentBuffer.Insert(0, '[');
                            currentBuffer.Append(']');
                            AddTextToInlines(currentBuffer.ToString(), textBlock);
                            currentBuffer.Clear();
                            state = ParserState.Normal;
                        }
                        else
                        {
                            currentBuffer.Append(c);
                        }
                        break;
                        
                    case ParserState.InLinkUrl:
                        if (c == '"' && !currentBuffer.ToString().Contains('"'))
                        {
                            // This might be the start of a title
                            currentBuffer.Append(c);
                        }
                        else if (c == ')' && (!currentBuffer.ToString().Contains('"') || currentBuffer.ToString().LastIndexOf('"') < currentBuffer.Length - 1))
                        {
                            // End link URL
                            string url = currentBuffer.ToString();
                            string title = null;
                            
                            // Extract title if present
                            int titleStartIndex = url.IndexOf(" \"");
                            if (titleStartIndex >= 0 && url.EndsWith("\""))
                            {
                                title = url.Substring(titleStartIndex + 2, url.Length - titleStartIndex - 3);
                                url = url.Substring(0, titleStartIndex);
                            }
                            
                            // Use a TextBlock directly instead of a Button for better inline alignment
                            var linkTextBlock = new TextBlock
                            {
                                Text = linkText, // Use the linkText captured earlier
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = textBlock.FontSize,
                                FontFamily = textBlock.FontFamily,
                                Foreground = new SolidColorBrush(Color.Parse("#2686C2")),
                                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                                TextDecorations = null // No underline
                            };
                            
                            // Store the URL to use when clicked
                            string finalUrl = url;
                            
                            // Handle pointer pressed event for the TextBlock
                            linkTextBlock.PointerPressed += (sender, e) =>
                            {
                                try
                                {
                                    // Try to open the URL in the default browser
                                    var processInfo = new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = finalUrl,
                                        UseShellExecute = true
                                    };
                                    System.Diagnostics.Process.Start(processInfo);
                                }
                                catch (Exception ex)
                                {
                                    // Handle any errors
                                    Console.WriteLine($"Error opening URL: {ex.Message}");
                                }
                            };
                            
                            // Add tooltip if title is provided
                            if (!string.IsNullOrEmpty(title))
                            {
                                ToolTip.SetTip(linkTextBlock, title);
                            }
                            
                            // Add the clickable link to the textblock using InlineUIContainer
                            var container = new InlineUIContainer(linkTextBlock);
                            textBlock.Inlines.Add(container);
                            
                            currentBuffer.Clear();
                            state = ParserState.Normal;
                        }
                        else
                        {
                            currentBuffer.Append(c);
                        }
                        break;
                        
                    case ParserState.InFootnote:
                        if (c == ']')
                        {
                            // End footnote
                            string footnoteId = currentBuffer.ToString();
                            var footnoteRef = new Run($"[{footnoteId}]")
                            {
                                FontSize = textBlock.FontSize * 0.8,
                                Foreground = Brushes.Blue
                            };
                            
                            textBlock.Inlines.Add(footnoteRef);
                            currentBuffer.Clear();
                            state = ParserState.Normal;
                        }
                        else
                        {
                            currentBuffer.Append(c);
                        }
                        break;
                }
                
                i++;
            }
            
            // Handle unclosed formatting elements at end of text
            if (state != ParserState.Normal)
            {
                switch (state)
                {
                    case ParserState.InCode:
                        AddTextToInlines("`" + currentBuffer.ToString(), textBlock);
                        break;
                    case ParserState.InBold:
                        AddTextToInlines((usedAsteriskForBold ? "**" : "__") + currentBuffer.ToString(), textBlock);
                        break;
                    case ParserState.InItalic:
                        AddTextToInlines((usedAsteriskForItalic ? "*" : "_") + currentBuffer.ToString(), textBlock);
                        break;
                    case ParserState.InStrikethrough:
                        AddTextToInlines("~~" + currentBuffer.ToString(), textBlock);
                        break;
                    case ParserState.InLinkText:
                        AddTextToInlines("[" + currentBuffer.ToString(), textBlock);
                        break;
                    case ParserState.InLinkUrl:
                        AddTextToInlines("[" + linkText + "](" + currentBuffer.ToString(), textBlock);
                        break;
                    case ParserState.InFootnote:
                        AddTextToInlines("[^" + currentBuffer.ToString(), textBlock);
                        break;
                    default:
                        AddTextToInlines(currentBuffer.ToString(), textBlock);
                        break;
                }
            }
            else if (currentBuffer.Length > 0)
            {
                // Add any remaining text in normal state
                AddTextToInlines(currentBuffer.ToString(), textBlock);
            }
        }
        
        private void AddTextToInlines(string text, TextBlock textBlock)
        {
            if (!string.IsNullOrEmpty(text))
            {
                textBlock.Inlines.Add(new Run(text));
            }
        }
        
        private bool IsSpecialChar(char c)
        {
            // Characters that have special meaning in markdown
            return "*_`[]()~\\#".Contains(c);
        }
        
        private bool IsAlphanumeric(char c)
        {
            return char.IsLetterOrDigit(c) || char.IsPunctuation(c);
        }

        private (Control, int) ParseBlockquotes(string[] lines, int startIndex)
        {
            int currentIndex = startIndex;
            int quoteLevel = 0;
            var contentLines = new List<string>();
            var nestedQuotes = new List<(int Level, string Content)>();
            
            // Process all blockquote lines
            while (currentIndex < lines.Length && lines[currentIndex].StartsWith(">"))
            {
                string line = lines[currentIndex];
                
                // Count how many > characters to determine nesting level
                int level = 0;
                int i = 0;
                while (i < line.Length && line[i] == '>')
                {
                    level++;
                    i++;
                    // Skip a space after each > if present
                    if (i < line.Length && line[i] == ' ')
                        i++;
                }
                
                // Extract content without the quote markers
                string content = line.Substring(i);
                
                // If we changed level, store the previous content and start new
                if (level != quoteLevel && contentLines.Count > 0)
                {
                    nestedQuotes.Add((quoteLevel, string.Join("\n", contentLines)));
                    contentLines.Clear();
                }
                
                quoteLevel = level;
                contentLines.Add(content);
                currentIndex++;
            }
            
            // Add the final content
            if (contentLines.Count > 0)
            {
                nestedQuotes.Add((quoteLevel, string.Join("\n", contentLines)));
            }
            
            // Render the blockquotes from the innermost outward
            Control currentQuote = null;
            int maxLevel = nestedQuotes.Count > 0 ? nestedQuotes.Max(q => q.Level) : 0;
            
            for (int level = maxLevel; level > 0; level--)
            {
                var quotesAtLevel = nestedQuotes.Where(q => q.Level == level).ToList();
                if (quotesAtLevel.Count == 0) continue;
                
                var contentAtLevel = string.Join("\n", quotesAtLevel.Select(q => q.Content));
                
                // Create the blockquote for this level
                var quoteBlock = CreateFormattedParagraph(contentAtLevel);
                if (currentQuote != null)
                {
                    // If we have inner quotes, add them to this level
                    var panel = new StackPanel();
                    panel.Children.Add(quoteBlock);
                    panel.Children.Add(currentQuote);
                    
                    quoteBlock = new TextBlock
                    {
                        FontFamily = _customFont,
                        FontSize = 18,
                        FontWeight = FontWeight.Regular,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 34,
                        Margin = new Avalonia.Thickness(0, 0, 0, 1)
                    };
                    
                    // Instead of copying the formatted paragraph, just add the panel
                    var inlines = new List<Inline>();
                    inlines.Add(new InlineUIContainer(panel));
                    quoteBlock.Inlines.AddRange(inlines);
                }
                
                // Style the blockquote
                quoteBlock.Background = new SolidColorBrush(Color.FromRgb(40, 44, 52));
                quoteBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                quoteBlock.Padding = new Avalonia.Thickness(15);
                quoteBlock.Margin = new Avalonia.Thickness(10, 5, 10, 5);
                
                // Create new container for this level
                var borderPanel = new StackPanel();
                borderPanel.Children.Add(quoteBlock);
                
                // Set different colors for different nesting levels
                var borderColor = level switch
                {
                    1 => new SolidColorBrush(Color.FromRgb(86, 156, 214)), // Blue for level 1
                    2 => new SolidColorBrush(Color.FromRgb(156, 220, 254)), // Light blue for level 2
                    3 => new SolidColorBrush(Color.FromRgb(212, 212, 212)), // Light gray for level 3
                    _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))  // Gray for deeper levels
                };
                
                var quoteBorder = new Border
                {
                    Child = borderPanel,
                    BorderThickness = new Avalonia.Thickness(4, 0, 0, 0),
                    BorderBrush = borderColor,
                    CornerRadius = new Avalonia.CornerRadius(3),
                    Margin = new Avalonia.Thickness(0, 5, 0, 5),
                    Background = new SolidColorBrush(Color.FromRgb(40, 44, 52)),
                    Padding = new Avalonia.Thickness(0)
                };
                
                currentQuote = quoteBorder;
            }
            
            // If no quotes were processed, return an empty panel
            if (currentQuote == null)
            {
                currentQuote = new StackPanel();
            }
            
            return (currentQuote, currentIndex);
        }
    }

    public class ListItem
    {
        public string Content { get; set; }
        public int Indentation { get; set; }
        public bool IsOrdered { get; set; }
        public List<ListItem> NestedItems { get; set; }
        public string ContinuationContent { get; set; }
    }

    public class TaskItem
    {
        public string Content { get; set; }
        public bool IsChecked { get; set; }
    }
}
