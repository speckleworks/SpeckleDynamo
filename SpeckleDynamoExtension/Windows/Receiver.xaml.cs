using System;
using System.Windows;

namespace SpeckleDynamoExtension
{
  public partial class Receiver : Window
  {
    public event Action StreamChanged;
    public Receiver()
    {
      InitializeComponent();

      this.DragRectangle.MouseDown += (sender, e) =>
      {
        this.DragMove();
      };
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
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
