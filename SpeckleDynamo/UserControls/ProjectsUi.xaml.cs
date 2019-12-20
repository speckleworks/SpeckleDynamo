using System;
using System.Windows;
using System.Windows.Controls;


namespace SpeckleDynamo.UserControls
{
  /// <summary>
  /// Interaction logic for ReceiverUi.xaml
  /// </summary>
  public partial class ProjectsUi : UserControl
  {
    public event Action ProjectChanged;
    public ProjectsUi()
    {
      InitializeComponent();
    }

    private void ProjectsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //StreamChanged.Invoke()
    }
  }
}
