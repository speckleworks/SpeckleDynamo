using Dynamo.Controls;
using Dynamo.Wpf;
using System;

namespace SpeckleDynamo
{
    public class SpeckleDynamoReceiverUI : INodeViewCustomization<SpeckleDynamoReceiverNode>
    {
        public void CustomizeView(SpeckleDynamoReceiverNode model, NodeView nodeView)
        {
            var ui = new SpeckleDynamoUserControl();
            nodeView.inputGrid.Children.Add(ui);
            ui.DataContext = model;
        }

        public void Dispose()
        {
           
        }
    }
}
