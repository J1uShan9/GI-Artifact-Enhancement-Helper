using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private readonly Stack<FlowDocument> logTextStack = new Stack<FlowDocument>();
        private readonly Stack<bool> isFirstLetterInLineStack = new Stack<bool>();
        private bool isFirstLetterInLine = true;

        private const string placeholderText = "请输入注释...";

        public MainWindow()
        {
            InitializeComponent();
            SetRemarkTextBoxPlaceholder();

            RemarkTextBox.GotFocus += RemarkTextBox_GotFocus;
            RemarkTextBox.LostFocus += RemarkTextBox_LostFocus;
            LogTextBox.KeyDown += LogTextBox_KeyDown;

            this.Closing += MainWindow_Closing;
        }

        // Button_Click

        private void LetterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            string outputText = button.Tag?.ToString() ?? string.Empty;

            // Multiple n* Check
            if (outputText.EndsWith("*") &&
                LogTextBox.Document.Blocks.LastBlock is Paragraph lastParagraph &&
                lastParagraph.Inlines.LastInline is Run lastRun &&
                lastRun.Text.EndsWith("*") )
            {
                e.Handled = true;
                return;
            }

            // First Letter Interval Check
            if (!isFirstLetterInLine)
            {
                AppendRichText("  " + outputText);
            }
            else
            {
                AppendRichText(outputText);
                isFirstLetterInLine = false;
            }

            // n* Connectivity Check
            if (outputText.EndsWith("*"))
            {
                isFirstLetterInLine = true;
            }
            else
            {
                isFirstLetterInLine = false;
            }
        }

        private void NewlineButton_Click(object sender, RoutedEventArgs e)
        {
            AppendRichText("  // ");
            AppendRichText(Environment.NewLine);

            isFirstLetterInLine = true;
        }

        private void RemarkButton_Click(object sender, RoutedEventArgs e)
        {
            string remarkText = GetRemarkTextBoxText();
            if (!string.IsNullOrEmpty(remarkText) && remarkText != placeholderText)
            {
                AppendRichText("(", Brushes.Black);
                AppendRichText(remarkText, Brushes.Gray);
                AppendRichText(")", Brushes.Black);
                RemarkTextBox.Document.Blocks.Clear();
                SetRemarkTextBoxPlaceholder();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (LogTextBox.Document.Blocks.Count > 0)
            {
                logTextStack.Push(CloneFlowDocument(LogTextBox.Document));
                LogTextBox.Document.Blocks.Clear();
            }

            isFirstLetterInLine = true;
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (logTextStack.Count > 0)
            {
                LogTextBox.Document = logTextStack.Pop();
                isFirstLetterInLine = isFirstLetterInLineStack.Pop();
            }
        }

        // Hook Method

        private void RemarkTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RemarkTextBox.Document.Blocks.Clear();
        }

        private void RemarkTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(GetRemarkTextBoxText()))
            {
                SetRemarkTextBoxPlaceholder();
            }
        }

        private void LogTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                TextPointer caretPosition = LogTextBox.CaretPosition;
                LogTextBox.CaretPosition.InsertLineBreak();
                LogTextBox.CaretPosition = caretPosition.GetNextInsertionPosition(LogicalDirection.Forward);

                isFirstLetterInLine = true;
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            SaveLogToFile();
        }

        // Lib Functions

        private void AppendRichText(string text, Brush? color = null)
        {
            logTextStack.Push(CloneFlowDocument(LogTextBox.Document));
            isFirstLetterInLineStack.Push(isFirstLetterInLine);

            if (LogTextBox.Document.Blocks.LastBlock is Paragraph lastParagraph)
            {
                if (color != null)
                {
                    lastParagraph.Inlines.Add(new Run(text) { Foreground = color });
                }
                else
                {
                    lastParagraph.Inlines.Add(new Run(text));
                }
            }
            else
            {
                Paragraph paragraph = new Paragraph();
                if (color != null)
                {
                    paragraph.Inlines.Add(new Run(text) { Foreground = color });
                }
                else
                {
                    paragraph.Inlines.Add(new Run(text));
                }
                LogTextBox.Document.Blocks.Add(paragraph);
            }
        }

        private void SaveLogToFile()
        {
            string dirPath = "./Logs";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            if (LogTextBox == null || LogTextBox.Document == null) return;

            string fileName = $"log-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
            string filePath = Path.Combine(dirPath, fileName);

            TextRange textRange = new TextRange(LogTextBox.Document.ContentStart, LogTextBox.Document.ContentEnd);
            string logContent = textRange.Text;

            File.WriteAllText(filePath, logContent);
        }


        private FlowDocument CloneFlowDocument(FlowDocument original)
        {
            FlowDocument clone = new FlowDocument();
            foreach (var block in original.Blocks)
            {
                var clonedBlock = CloneBlock(block);
                if (clonedBlock != null)
                {
                    clone.Blocks.Add(clonedBlock);
                }
            }
            return clone;
        }

        private Block CloneBlock(Block block)
        {
            if (block is Paragraph paragraph)
            {
                Paragraph newParagraph = new Paragraph();
                foreach (var inline in paragraph.Inlines)
                {
                    var clonedInline = CloneInline(inline);
                    if (clonedInline != null)
                    {
                        newParagraph.Inlines.Add(clonedInline);
                    }
                }
                return newParagraph;
            }
            return new Paragraph();
        }

        private Inline CloneInline(Inline inline)
        {
            if (inline is Run run)
            {
                return new Run(run.Text) { Foreground = run.Foreground };
            }
            return new Run();
        }
        private void SetRemarkTextBoxPlaceholder()
        {
            Paragraph paragraph = new Paragraph(new Run(placeholderText) { Foreground = Brushes.Gray });
            RemarkTextBox.Document.Blocks.Clear();
            RemarkTextBox.Document.Blocks.Add(paragraph);
        }

        private string GetRemarkTextBoxText()
        {
            TextRange textRange = new TextRange(RemarkTextBox.Document.ContentStart, RemarkTextBox.Document.ContentEnd);
            return textRange.Text.Trim();
        }
    }
}
