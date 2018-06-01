using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SpeckleDynamo
{
  [NodeName("Test")]
  [NodeDescription("Tetst.")]
  [NodeCategory("Test")]
  [NodeSearchTags("Test")]
  [IsDesignScriptCompatible]
  public class Test : NodeModel
  {

    [JsonConstructor]
    private Test(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      ArgumentLacing = LacingStrategy.Disabled;
    }

    public Test()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("In", "")));
      RegisterAllPorts();

      ArgumentLacing = LacingStrategy.Disabled;
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + (OutPorts.Count + 1), "")));
      RegisterAllPorts();

      var associativeNodes = new List<AssociativeNode>();
      for (var i = 0; i< OutPorts.Count; i++)
      {
        var functionCall = AstFactory.BuildFunctionCall(
          new Func<string, string>(Functions.Functions.Test),
          new List<AssociativeNode>
          {
                AstFactory.BuildStringNode(i.ToString())
          });

        associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(i), functionCall));
      }
      return associativeNodes;
    }
 

    public override bool IsConvertible
    {
      get { return true; }
    }

    protected override void OnConnectorAdded(ConnectorModel obj)
    {
      base.OnConnectorAdded(obj);
    }
  }
}
