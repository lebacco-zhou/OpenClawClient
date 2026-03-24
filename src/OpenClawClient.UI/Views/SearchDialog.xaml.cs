using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OpenClawClient.Core.Models;
using OpenClawClient.Core.Services;

namespace OpenClawClient.UI.Views;

/// <summary>
/// 消息搜索对话框
/// </summary>
public partial class SearchDialog : Window
{
    private readonly ObservableCollection<ChatMessage> _allMessages;
    private readonly ObservableCollection<ChatMessage> _searchResults = new();

    public SearchDialog(ObservableCollection<ChatMessage> allMessages)
    {
        InitializeComponent();
        _allMessages = allMessages;
        ResultsListBox.ItemsSource = _searchResults;
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        PerformSearch();
    }

    private void PerformSearch()
    {
        var query = SearchTextBox.Text.Trim().ToLower();
        
        if (string.IsNullOrEmpty(query))
        {
            _searchResults.Clear();
            return;
        }

        var results = _allMessages
            .Where(m => m.Content.ToLower().Contains(query) || 
                       (!string.IsNullOrEmpty(m.FileName) && m.FileName.ToLower().Contains(query)))
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        _searchResults.Clear();
        foreach (var result in results)
        {
            _searchResults.Add(result);
        }

        ResultCountText.Text = $"找到 {results.Count} 条消息";
    }

    private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            PerformSearch();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ResultsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ResultsListBox.SelectedItem is ChatMessage message)
        {
            DialogResult = true;
            // 返回选中的消息，主窗口可以滚动到该消息
        }
    }
}
