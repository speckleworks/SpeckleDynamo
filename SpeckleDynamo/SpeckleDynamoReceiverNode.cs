using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using System.Collections.Generic;

namespace SpeckleDynamo
{
  [NodeName("DataReceiver")]
  [NodeDescription("Receives data from Speckle.")]
  [NodeCategory("Speckle.IO")]

  //Inputs
  [InPortNames("ID")]
  [InPortDescriptions("The stream's short id.")]
  [InPortTypes("string")]

  [IsDesignScriptCompatible]
  public class SpeckleDynamoReceiverNode : NodeModel
  {
    public SpeckleDynamoReceiverNode()
    {
      RegisterAllPorts();
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
    }


  }
}
