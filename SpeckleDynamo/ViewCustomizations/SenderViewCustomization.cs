using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using Dynamo.Nodes;
using System;
using System.Linq;
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
      //rename layers
      var mi = new MenuItem { Header = "Rename Layers (Inputs)..." };
      mi.Click += (s, e) =>
       {
         var rl = new RenameLayers(_sender.InPorts.Select(x => x.Name));
         rl.Owner = Window.GetWindow(nodeView);
         rl.WindowStartupLocation = WindowStartupLocation.CenterOwner;
         rl.Title = _sender.Name + " | Rename Layers";
         var result = rl.ShowDialog();
         if(result.HasValue && result.Value)
         {
           _sender.RenameLayers(rl.Layers.Select(x=>x.Name).ToList());
         }
       };

      //stream view
      var viewStream = new MenuItem { Header = "View stream online" };
      viewStream.Click += (s, e) =>
      {
        if (_sender.StreamId == null) return;
        System.Diagnostics.Process.Start(_sender.RestApi.Replace("api/v1", "view") + @"/?streams=" + _sender.StreamId);
      };
      var viewStreamData = new MenuItem { Header = "(API) View stream data" };
      viewStreamData.Click += (s, e) =>
      {
        if (_sender.StreamId == null) return;
        System.Diagnostics.Process.Start(_sender.RestApi + @"/streams/" + _sender.StreamId);
      };
      var viewObjectsData = new MenuItem { Header = "(API) View objects data" };
      viewObjectsData.Click += (s, e) =>
      {
        if (_sender.StreamId == null) return;
        System.Diagnostics.Process.Start(_sender.RestApi + @"/streams/" + _sender.StreamId + @"/objects?omit=displayValue,base64");
      };

      var addToHistory = new MenuItem { Header = "Save current stream as a version" };
      addToHistory.Click += (s, e) =>
      {
        if (_sender.StreamId == null) return;
        var cloneResult = _sender.mySender.StreamCloneAsync(_sender.StreamId).Result;
        _sender.mySender.Stream.Children.Add(cloneResult.Clone.StreamId);

        _sender.mySender.BroadcastMessage(new { eventType = "update-children" });

        System.Windows.MessageBox.Show("Stream version saved. CloneId: " + cloneResult.Clone.StreamId);
      };




      nodeView.grid.ContextMenu.Items.Add(mi);
      nodeView.grid.ContextMenu.Items.Add(viewStream);
      nodeView.grid.ContextMenu.Items.Add(viewStreamData);
      nodeView.grid.ContextMenu.Items.Add(viewObjectsData);
      nodeView.grid.ContextMenu.Items.Add(addToHistory);



    }
    public void Dispose()
    {
    }
  }
}
