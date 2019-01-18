using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Wpf;
using SpeckleDynamo.UserControls;
using System;
using System.Windows.Controls;

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
      ui.ForceDownloadButton.Click += _receiver.ForceDownloadButtonClick;
      ui.PauseToggle.Checked += _receiver.PauseToggleChecked;
      ui.StreamChanged += _receiver.StreamChanged;

      nodeView.ViewModel.DynamoViewModel.HomeSpace.RunSettings.PropertyChanged += RunSettings_PropertyChanged; ;

      nodeView.inputGrid.Children.Add(ui);


      nodeView.grid.ContextMenu.Items.Add(new Separator());
      //stream view
      var viewStream = new MenuItem { Header = "View stream online" };
      viewStream.Click += (s, e) =>
      {
        if (_receiver.StreamId == null) return;
        System.Diagnostics.Process.Start(_receiver.RestApi.Replace("api/v1", "view") + @"/?streams=" + _receiver.StreamId);
      };
      var viewStreamData = new MenuItem { Header = "(API) View stream data" };
      viewStreamData.Click += (s, e) =>
      {
        if (_receiver.StreamId == null) return;
        System.Diagnostics.Process.Start(_receiver.RestApi + @"/streams/" + _receiver.StreamId);
      };
      var viewObjectsData = new MenuItem { Header = "(API) View objects data" };
      viewObjectsData.Click += (s, e) =>
      {
        if (_receiver.StreamId == null) return;
        System.Diagnostics.Process.Start(_receiver.RestApi + @"/streams/" + _receiver.StreamId + @"/objects?omit=displayValue,base64");
      };

      nodeView.grid.ContextMenu.Items.Add(viewStream);
      nodeView.grid.ContextMenu.Items.Add(viewStreamData);
      nodeView.grid.ContextMenu.Items.Add(viewObjectsData);
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
