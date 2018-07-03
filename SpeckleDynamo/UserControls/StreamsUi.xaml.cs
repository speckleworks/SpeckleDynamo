using System;
using System.Windows;
using System.Windows.Controls;


namespace SpeckleDynamo.UserControls
{
  /// <summary>
  /// Interaction logic for ReceiverUi.xaml
  /// </summary>
  public partial class StreamsUi : UserControl
  {
    public event Action StreamChanged;
    public StreamsUi()
    {
      InitializeComponent();
    }

    private void StreamsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //StreamChanged.Invoke()
    }
  }
}
