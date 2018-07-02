using System.Windows;
using System.Windows.Controls;


namespace SpeckleDynamo.UserControls
{
  /// <summary>
  /// Interaction logic for ReceiverUi.xaml
  /// </summary>
  public partial class SenderUi : UserControl
  {
    public SenderUi()
    {
      InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      Clipboard.SetText(Stream.Text);
    }
  }
}
