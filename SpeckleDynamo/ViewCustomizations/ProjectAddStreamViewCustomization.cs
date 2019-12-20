using Dynamo.Controls;
using Dynamo.Wpf;
using SpeckleDynamo.UserControls;
using System;

namespace SpeckleDynamo.ViewCustomizations
{
  public class ProjectAddStreamCustomization : INodeViewCustomization<ProjectAddStream>
  {
    private ProjectAddStream addStream;

    public void CustomizeView(ProjectAddStream model, NodeView nodeView)
    {
      var ui = new ProjectAddStreamUi();
      if(Version.Parse(nodeView.ViewModel.DynamoViewModel.Version).CompareTo(new Version(2, 0)) < 0)
      {
        model.Error("Dynamo 2.0 or greater is required to run this package");
        return;
      }

      addStream = model;

      //bindings   
      ui.DataContext = addStream;
      ui.Loaded += addStream.AddedToDocument;

      nodeView.inputGrid.Children.Add(ui);
      SpeckleDynamo.ProjectAddStream.viewModel = nodeView;
    }

    public void Dispose()
    {
    }
  }
}
