using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using Dynamo.Nodes;
using System;
using System.Collections.Generic;
using System.Windows;
using SpeckleDynamo.UserControls;
using SpecklePopup;

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
      ui.Loaded += _sender.PromptAccountSelection;
      ui.ForceSend.Click += _sender.ForceSendClick;

      //add remove input buttons
      var addButton = new DynamoNodeButton(nodeView.ViewModel.NodeModel, "AddInPort") { Content = "+", Width = 20 };
      var subButton = new DynamoNodeButton(nodeView.ViewModel.NodeModel, "RemoveInPort") { Content = "-", Width = 20 };
      ui.DynamoButtons.Children.Add(addButton);
      ui.DynamoButtons.Children.Add(subButton);

      nodeView.inputGrid.Children.Add(ui);

    }



    public void Dispose()
    {
    }
  }

}