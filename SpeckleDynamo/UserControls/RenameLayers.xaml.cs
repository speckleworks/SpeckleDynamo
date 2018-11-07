
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

using System.Windows;


namespace SpeckleDynamo.UserControls
{
  /// <summary>
  /// Interaction logic for RenameLayers.xaml
  /// </summary>
  public partial class RenameLayers : Window
  {

    public ObservableCollection<InputName> Layers = new ObservableCollection<InputName>();

    public RenameLayers(IEnumerable<string> layernames)
    {
      InitializeComponent();
      foreach (var l in layernames)
        Layers.Add(new InputName(l));
      this.DataContext = Layers;

    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }
  }

  public class InputName : INotifyPropertyChanged
  {
    private string _name { get; set; }
    public string Name { get => _name; set { _name = value; RaisePropertyChanged("Name"); } }

    public InputName(string name)
    {
      Name = name;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void RaisePropertyChanged(String info)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(info));
      }
    }
  }
}
