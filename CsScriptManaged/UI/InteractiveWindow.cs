﻿using CsScriptManaged.UI.CodeWindow;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CsScriptManaged.UI
{
    internal class InteractiveWindow : Window
    {
        private const string DefaultStatusText = "Type 'help' to get started :)";
        private const string ExecutingStatusText = "Executing...";
        private const string InitializingStatusText = "Initializing...";
        private const string ExecutingPrompt = "...> ";
        private static readonly Brush ExecutingBackground = Brushes.LightGray;
        private static readonly Brush NormalBackground = Brushes.White;
        private InteractiveCodeEditor textEditor;
        private StackPanel resultsPanel;
        private TextBlock promptBlock;
        private StatusBarItem statusBarStatusText;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveWindow"/> class.
        /// </summary>
        public InteractiveWindow()
        {
            // Set window look
            Background = ExecutingBackground;
            ShowInTaskbar = false;
            Title = "C# Interactive Window";

            // Add dock panel and status bar
            DockPanel dockPanel = new DockPanel();
            StatusBar statusBar = new StatusBar();
            statusBarStatusText = new StatusBarItem();
            statusBarStatusText.Content = InitializingStatusText;
            statusBar.Items.Add(statusBarStatusText);
            DockPanel.SetDock(statusBar, Dock.Bottom);
            dockPanel.Children.Add(statusBar);
            Content = dockPanel;

            // Add results panel
            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            dockPanel.Children.Add(scrollViewer);
            resultsPanel = new StackPanel();
            resultsPanel.Orientation = Orientation.Vertical;
            resultsPanel.CanVerticallyScroll = true;
            resultsPanel.CanHorizontallyScroll = true;
            scrollViewer.Content = resultsPanel;

            // Add prompt for text editor
            var panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            resultsPanel.Children.Add(panel);

            promptBlock = new TextBlock();
            promptBlock.FontFamily = new FontFamily("Consolas");
            promptBlock.FontSize = 14;
            promptBlock.Text = ExecutingPrompt;
            panel.Children.Add(promptBlock);

            // Add text editor
            textEditor = new InteractiveCodeEditor();
            textEditor.Background = Brushes.Transparent;
            textEditor.CommandExecuted += TextEditor_CommandExecuted;
            textEditor.CommandFailed += TextEditor_CommandFailed;
            textEditor.Executing += TextEditor_Executing;
            textEditor.CloseRequested += TextEditor_CloseRequested;
            textEditor.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            textEditor.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            panel.Children.Add(textEditor);
        }

        private UIElement CreateTextOutput(string textOutput)
        {
            textOutput = textOutput.Replace("\r\n", "\n");
            if (textOutput.EndsWith("\n"))
                textOutput = textOutput.Substring(0, textOutput.Length - 1);

            var textBlock = new TextBlock();
            textBlock.FontFamily = new FontFamily("Consolas");
            textBlock.FontSize = 14;
            textBlock.Text = textOutput;
            return textBlock;
        }

        private UIElement CreateCSharpCode(string code)
        {
            var panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;

            var textBlock = new TextBlock();
            textBlock.FontFamily = new FontFamily("Consolas");
            textBlock.FontSize = 14;
            textBlock.Text = InteractiveExecution.DefaultPrompt;
            panel.Children.Add(textBlock);

            var codeControl = new CsTextEditor();
            codeControl.IsEnabled = false;
            codeControl.Text = code;
            codeControl.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            codeControl.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            panel.Children.Add(codeControl);

            return panel;
        }

        private void TextEditor_CommandExecuted(string textOutput, object objectOutput)
        {
            int textEditorIndex = resultsPanel.Children.Count - 1;

            if (objectOutput != null)
            {
                UIElement elementOutput = objectOutput as UIElement;

                if (elementOutput != null)
                    resultsPanel.Children.Insert(textEditorIndex, elementOutput);
                else
                    resultsPanel.Children.Insert(textEditorIndex, CreateTextOutput(objectOutput.ToString()));
            }
            if (!string.IsNullOrEmpty(textOutput))
                resultsPanel.Children.Insert(textEditorIndex, CreateTextOutput(textOutput));
            resultsPanel.Children.Insert(textEditorIndex, CreateCSharpCode(textEditor.Text));
        }

        private void TextEditor_CommandFailed(string textOutput, string errorOutput)
        {
            // TODO:
            MessageBox.Show(textOutput + errorOutput);
        }

        private void TextEditor_Executing(bool started)
        {
            if (!started)
            {
                textEditor.TextArea.Focus();
                statusBarStatusText.Content = DefaultStatusText;
                Background = NormalBackground;
                promptBlock.Text = InteractiveExecution.DefaultPrompt;
            }
            else
            {
                statusBarStatusText.Content = ExecutingStatusText;
                Background = ExecutingBackground;
                promptBlock.Text = ExecutingPrompt;
            }
        }

        private void TextEditor_CloseRequested()
        {
            Close();
        }

        /// <summary>
        /// Shows the window as modal dialog.
        /// </summary>
        public static void ShowModalWindow()
        {
            ExecuteInSTA(() =>
            {
                Window window = null;

                try
                {
                    window = new InteractiveWindow();
                    window.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                window.Close();
            });
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        public static void ShowWindow()
        {
            ExecuteInSTA(() =>
            {
                Window window = null;

                try
                {
                    window = new InteractiveWindow();
                    window.Show();

                    var _dispatcherFrame = new System.Windows.Threading.DispatcherFrame();
                    window.Closed += (obj, e) => { _dispatcherFrame.Continue = false; };
                    System.Windows.Threading.Dispatcher.PushFrame(_dispatcherFrame);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                window.Close();
            }, waitForExecution: false);
        }

        private static void ExecuteInSTA(Action action, bool waitForExecution = true)
        {
            Thread thread = new Thread(() => { action(); });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (waitForExecution)
            {
                thread.Join();
            }
        }
    }
}
