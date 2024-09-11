using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private Stack<FlowDocument> undoStack = new Stack<FlowDocument>();
        private bool isFirstLetterInLine = true;
        private const string PlaceholderText = "请输入注释...";

        public MainWindow()
        {
            InitializeComponent();
            RemarkTextBox.GotFocus += RemarkTextBox_GotFocus;
            RemarkTextBox.LostFocus += RemarkTextBox_LostFocus;
            SetRemarkTextBoxPlaceholder();
        }

        // Button_Click

        private void LetterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            string outputText = button.Tag?.ToString() ?? string.Empty;

            if (!isFirstLetterInLine)
            {
                LogAppendText("  " + outputText);
            }
            else
            {
                LogAppendText(outputText);
                isFirstLetterInLine = false;
            }

            if (outputText.EndsWith("*"))
            {
                isFirstLetterInLine = true;
            }
        }

        private void NewlineButton_Click(object sender, RoutedEventArgs e)
        {
            LogAppendText(Environment.NewLine);
            isFirstLetterInLine = true;
        }

        private void RemarkButton_Click(object sender, RoutedEventArgs e)
        {
            string remarkText = GetRemarkTextBoxText();
            if (!string.IsNullOrEmpty(remarkText) && remarkText != PlaceholderText)
            {
                LogAppendText("(" + remarkText + ")", Brushes.Gray);
                RemarkTextBox.Document.Blocks.Clear();
                SetRemarkTextBoxPlaceholder();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (LogTextBox.Document.Blocks.Count > 0)
            {
                undoStack.Push(CloneFlowDocument(LogTextBox.Document));
                LogTextBox.Document.Blocks.Clear();
            }
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                LogTextBox.Document = undoStack.Pop();
            }
        }

        // Lib Functions

        private void LogAppendText(string text, Brush? color = null)
        {
            undoStack.Push(CloneFlowDocument(LogTextBox.Document));

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

        private void SetRemarkTextBoxPlaceholder()
        {
            Paragraph paragraph = new Paragraph(new Run(PlaceholderText) { Foreground = Brushes.Gray });
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
