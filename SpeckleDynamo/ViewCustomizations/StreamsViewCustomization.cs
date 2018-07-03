using Dynamo.Controls;
using Dynamo.Wpf;
using SpeckleDynamo.UserControls;
using System;

namespace SpeckleDynamo.ViewCustomizations
{
  public class StreamsViewCustomization : INodeViewCustomization<Streams>
  {
    private Streams _streams;

    public void CustomizeView(Streams model, NodeView nodeView)
    {
      var ui = new StreamsUi();
      if(Version.Parse(nodeView.ViewModel.DynamoViewModel.Version).CompareTo(new Version(2, 0)) < 0)
      {
        model.Error("Dynamo 2.0 or greater is required to run this package");
        return;
      }

      _streams = model;

      //bindings   
      ui.DataContext = _streams;
      ui.Loaded += _streams.AddedToDocument;
;
      nodeView.inputGrid.Children.Add(ui);
    }

    public void Dispose()
    {
    }
  }
}
