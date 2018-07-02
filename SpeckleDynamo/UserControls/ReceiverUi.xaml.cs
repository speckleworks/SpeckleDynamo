using System;
using System.Windows;
using System.Windows.Controls;


namespace SpeckleDynamo.UserControls
{
  /// <summary>
  /// Interaction logic for ReceiverUi.xaml
  /// </summary>
  public partial class ReceiverUi : UserControl
  {
    public event Action StreamChanged;
    public ReceiverUi()
    {
      InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      Stream.Text = Clipboard.GetText();
      StreamChanged.Invoke();
    }

    private void Stream_LostFocus(object sender, RoutedEventArgs e)
    {
      StreamChanged.Invoke();
    }
  }
}
