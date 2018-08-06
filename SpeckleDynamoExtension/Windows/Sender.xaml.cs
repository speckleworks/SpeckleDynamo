using System.Windows;

namespace SpeckleDynamoExtension
{
  public partial class Sender : Window
  {
    public Sender()
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
      Clipboard.SetText(Stream.Text);
    }
  }
}
