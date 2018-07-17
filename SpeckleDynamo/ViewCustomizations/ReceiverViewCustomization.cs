using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Wpf;
using SpeckleDynamo.UserControls;
using System;

namespace SpeckleDynamo.ViewCustomizations
{
  public class ReceiverViewCustomization : INodeViewCustomization<Receiver>
  {
    private Receiver _receiver;

    public void CustomizeView(Receiver model, NodeView nodeView)
    {
      var ui = new ReceiverUi();
      model.DocumentGuid = nodeView.ViewModel.DynamoViewModel.CurrentSpace.Guid.ToString();
      model.DocumentName = nodeView.ViewModel.DynamoViewModel.CurrentSpace.Name;

      if(Version.Parse(nodeView.ViewModel.DynamoViewModel.Version).CompareTo(new Version(2, 0)) < 0)
      {
        model.Error("Dynamo 2.0 or greater is required to run this package");
        return;
      }

      _receiver = model;

      //bindings   
      ui.DataContext = _receiver;
      ui.Loaded += _receiver.AddedToDocument;
      ui.PausePlayButton.Click += _receiver.PausePlayButtonClick;
      ui.StreamChanged += _receiver.StreamChanged;

      nodeView.ViewModel.DynamoViewModel.HomeSpace.RunSettings.PropertyChanged += RunSettings_PropertyChanged; ;

      nodeView.inputGrid.Children.Add(ui);
    }

    private void RunSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      _receiver.RunType = ((RunSettings)sender).RunType;
    }

    public void Dispose()
    {
    }
  }
}
