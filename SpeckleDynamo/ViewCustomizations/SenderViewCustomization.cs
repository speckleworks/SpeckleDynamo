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

      //wModel.InPorts.

    }



    public void Dispose()
    {
    }

    //public static T GetChildOfType<T>(this DependencyObject depObj)
    //where T : DependencyObject
    //{
    //  if (depObj == null) return null;

    //  for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
    //  {
    //    var child = VisualTreeHelper.GetChild(depObj, i);

    //    var result = (child as T) ?? GetChildOfType<T>(child);
    //    if (result != null) return result;
    //  }
    //  return null;
    //}
  }

}