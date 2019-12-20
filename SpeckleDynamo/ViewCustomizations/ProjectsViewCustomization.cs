using Dynamo.Controls;
using Dynamo.Wpf;
using SpeckleDynamo.UserControls;
using System;

namespace SpeckleDynamo.ViewCustomizations
{
  public class ProjectsViewCustomization : INodeViewCustomization<Projects>
  {
    private Projects projects;

    public void CustomizeView(Projects model, NodeView nodeView)
    {
      var ui = new ProjectsUi();
      if(Version.Parse(nodeView.ViewModel.DynamoViewModel.Version).CompareTo(new Version(2, 0)) < 0)
      {
        model.Error("Dynamo 2.0 or greater is required to run this package");
        return;
      }

      projects = model;

      //bindings   
      ui.DataContext = projects;
      ui.Loaded += projects.AddedToDocument;

      nodeView.inputGrid.Children.Add(ui);
    }

    public void Dispose()
    {
    }
  }
}
