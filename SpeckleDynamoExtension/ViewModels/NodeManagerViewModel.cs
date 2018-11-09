using Dynamo.Core;
using Dynamo.Graph.Nodes;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SpeckleDynamoExtension.ViewModels
{
  public class NodeManagerViewModel : NotificationObject, IDisposable
  {
    private ViewLoadedParams viewLoadedParams;

    private ObservableCollection<NodeModel> _speckleNodes = new ObservableCollection<NodeModel>();
    public ObservableCollection<NodeModel> SpeckleNodes { get { return _speckleNodes; } set { _speckleNodes = value; RaisePropertyChanged("SpeckleNodes"); } }

    public NodeManagerViewModel(ViewLoadedParams p)
    {
      viewLoadedParams = p;

      //subscribing to dynamo events
      viewLoadedParams.CurrentWorkspaceModel.NodeAdded += CurrentWorkspaceModel_NodeAdded;
      viewLoadedParams.CurrentWorkspaceModel.NodeRemoved += CurrentWorkspaceModel_NodeRemoved;
    }

    private void CurrentWorkspaceModel_NodeRemoved(NodeModel obj)
    {
      var type = obj.GetType().ToString();
      if (type == "SpeckleDynamo.Sender" || type == "SpeckleDynamo.Receiver")
      {
        SpeckleNodes.Remove(obj);
      }
    }

    private void CurrentWorkspaceModel_NodeAdded(NodeModel obj)
    {
      var type = obj.GetType().ToString();
      if (type == "SpeckleDynamo.Sender" || type == "SpeckleDynamo.Receiver")
      {
        SpeckleNodes.Add(obj);
      }
    }

    public void ZoomToFitNodes()
    {
      //IsSelected on the NodeModel is not enough, need to call AddToSelectionCommand
      var dynViewModel = viewLoadedParams.DynamoWindow.DataContext as DynamoViewModel;
      var selectedNodes = SpeckleNodes.Where(x => x.IsSelected).ToList();
      Utilities.ClearSelection();
      foreach (var node in selectedNodes)
        dynViewModel.AddToSelectionCommand.Execute(node);
      dynViewModel.FitViewCommand.Execute(null);
    }

    public void DeleteNodes()
    {
      //IsSelected on the NodeModel is not enough, need to call AddToSelectionCommand
      var dynViewModel = viewLoadedParams.DynamoWindow.DataContext as DynamoViewModel;
      var selectedNodes = SpeckleNodes.Where(x => x.IsSelected).ToList();
      Utilities.ClearSelection();
      foreach(var node in selectedNodes)
        dynViewModel.AddToSelectionCommand.Execute(node);
      dynViewModel.DeleteCommand.Execute(null);
    }

    public void Dispose()
    {
      //unsubscribing from events
      viewLoadedParams.CurrentWorkspaceModel.NodeAdded -= CurrentWorkspaceModel_NodeAdded;
      viewLoadedParams.CurrentWorkspaceModel.NodeRemoved -= CurrentWorkspaceModel_NodeRemoved;
    }


  }
}
