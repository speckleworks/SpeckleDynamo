using Dynamo.Controls;
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
      ui.Stream.LostFocus += _receiver.Stream_LostFocus;
      nodeView.inputGrid.Children.Add(ui);
    }

    public void Dispose()
    {
    }
  }
}
