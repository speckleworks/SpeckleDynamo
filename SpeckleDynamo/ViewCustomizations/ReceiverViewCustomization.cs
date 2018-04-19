using Dynamo.Controls;
using Dynamo.Wpf;
using SpeckleDynamo.UserControls;


namespace SpeckleDynamo.ViewCustomizations
{
  public class ReceiverViewCustomization : INodeViewCustomization<Receiver>
  {
    private Receiver _receiver;

    public void CustomizeView(Receiver model, NodeView nodeView)
    {
      var ui = new ReceiverUi();
      _receiver = model;

      //bindings
      ui.DataContext = _receiver;
      ui.Loaded += _receiver.PromptAccountSelection;
      ui.PausePlayButton.Click += _receiver.PausePlayButtonClick;
      ui.Stream.LostFocus += _receiver.Stream_LostFocus;
      nodeView.inputGrid.Children.Add(ui);
    }

    public void Dispose()
    {
    }

    private void ExpireNode(object sender, System.EventArgs e)
    {
      _receiver.ExpireNode();
    }

   

  }
}
