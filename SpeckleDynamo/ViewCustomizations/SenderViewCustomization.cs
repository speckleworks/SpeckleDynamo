using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using Dynamo.Nodes;
using System;
using System.Collections.Generic;
using System.Windows;
using SpeckleDynamo.UserControls;
using SpecklePopup;
using System.Windows.Controls;

namespace SpeckleDynamo
{
  public class SenderViewCustomization : INodeViewCustomization<Sender>
  {
    private Sender _sender;

    public void CustomizeView(Sender model, NodeView nodeView)
    {
      var ui = new SenderUi();
      model.DocumentGuid = nodeView.ViewModel.DynamoViewModel.CurrentSpace.Guid.ToString();
      model.DocumentName = nodeView.ViewModel.DynamoViewModel.CurrentSpace.Name;
      if (Version.Parse(nodeView.ViewModel.DynamoViewModel.Version).CompareTo(new Version(2, 0)) < 0)
      {
        model.Error("Dynamo 2.0 or greater is required to run this package");
        return;
      }
      _sender = model;

      //bindings
      ui.DataContext = _sender;
      ui.Loaded += _sender.AddedToDocument;
      ui.ForceSend.Click += _sender.ForceSendClick;

      //add remove input buttons
      var addButton = new DynamoNodeButton(nodeView.ViewModel.NodeModel, "AddInPort") { Content = "+", Width = 20 };
      var subButton = new DynamoNodeButton(nodeView.ViewModel.NodeModel, "RemoveInPort") { Content = "-", Width = 20 };
      ui.DynamoButtons.Children.Add(addButton);
      ui.DynamoButtons.Children.Add(subButton);

      nodeView.inputGrid.Children.Add(ui);
      nodeView.grid.ContextMenu.Items.Add(new Separator());
      var mi = new MenuItem { Header = "Rename Inputs..." };
      var inputs = new ContextMenuItem();
      inputs.DataContext = _sender;
      mi.Items.Add(inputs);
      nodeView.grid.ContextMenu.Items.Add(mi);
    }
    public void Dispose()
    {
    }
  }
}