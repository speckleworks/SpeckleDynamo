using Dynamo.Controls;
using Dynamo.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleDynamo
{
    public class SpeckleDynamoSenderUI : INodeViewCustomization<SpeckleDynamoSenderNode>
    {
        public void CustomizeView(SpeckleDynamoSenderNode model, NodeView nodeView)
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