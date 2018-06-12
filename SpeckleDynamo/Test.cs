using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using System.Collections.Generic;
using Dynamo.Utilities;
using System;
using System.Linq;


namespace Test
{
  [NodeName("Test")]
  [NodeDescription("Tetst.")]
  [NodeCategory("Test")]
  [NodeSearchTags("Test")]
  [IsDesignScriptCompatible]


  public class Test : VariableInputNode
  {

    [JsonConstructor]
    private Test(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      ArgumentLacing = LacingStrategy.Disabled;
    }

    public Test()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("In", "")));
      //OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + (1), "")));
      //OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + (2), "")));
      //OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + (3), "")));
      RegisterAllPorts();
    }

    protected override void AddInput()
    {
      var ports = OutPorts.Count;
      OutPorts.RemoveAll((p) => { return true; });

      for (var i = 0; i < ports + 1; i++)
        OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + i, "")));
       RegisterAllPorts();
    }

    protected override void RemoveInput()
    {
      //var t = Task.Run(() => OnNodeModified(true));
      //this.BuildOutputAst(new List<AssociativeNode>() {AstFactory.BuildNullNode() });
      
        OnNodeModified(true);
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {

      var associativeNodes = new List<AssociativeNode>();
      for (var i = 0; i < OutPorts.Count; i++)
      {
        associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(i), 
          AstFactory.BuildStringNode((DateTime.Now.ToLongTimeString()))));
      }
      return associativeNodes;
    }


    protected override string GetInputName(int index)
    {
      return "item" + index;
    }

    protected override string GetInputTooltip(int index)
    {
      return "item" + index;
    }
  }
}
