using SpeckleDynamoExtension.ViewModels;
using System.Windows;


namespace SpeckleDynamoExtension.Windows
{
  /// <summary>
  /// Interaction logic for NodeManager.xaml
  /// </summary>
  public partial class NodeManager : Window
  {
    public NodeManager()
    {
      InitializeComponent();
      this.Closing += NodeManager_Closing;
    }

    private void NodeManager_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      //hide window instead of closing it, so we keep tracking in the background
      e.Cancel = true;
      this.Hide();
    }
    private void Zoom_Click(object sender, RoutedEventArgs e)
    {
      //could deal with this here, but sending to the view model
      var nm = DataContext as NodeManagerViewModel;
      nm.ZoomToFitNodes();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
      //could deal with this here, but sending to the view model
      var nm = DataContext as NodeManagerViewModel;
      nm.DeleteNodes();
    }
  }
}
