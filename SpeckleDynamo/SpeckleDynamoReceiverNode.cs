using Dynamo.Graph.Nodes;


namespace SpeckleDynamo
{
    [NodeName("Data Receiver")]
    [NodeDescription ("Receives data from Speckle.")]
    [NodeCategory("Speckle.IO")]
    //Inputs
    [InPortNames("ID")]
    [InPortDescriptions("The stream's short id.")]
    [InPortTypes("string")]
    public class SpeckleDynamoReceiverNode : NodeModel
    {
        public SpeckleDynamoReceiverNode()
        {
            RegisterAllPorts();
        }


    }
}
