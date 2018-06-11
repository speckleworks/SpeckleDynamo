using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using SpeckleCore;
using System;
using System.Collections.Generic;

namespace SpeckleDynamo
{
  [NodeName("Test")]
  [NodeDescription("Tetst.")]
  [NodeCategory("Test")]
  [NodeSearchTags("Test")]
  [IsDesignScriptCompatible]


  public class Test : VariableInputNode
  {
    public SpeckleApiClient myReceiver;
    private bool _registeringPorts = false;

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

      myReceiver = new SpeckleApiClient("https://hestia.speckle.works/api/v1", true);

      InitReceiverEventsAndGlobals();

      //TODO: get documentname and guid, not sure how... Maybe with an extension?
      myReceiver.IntializeReceiver("H1oKSCveQ", "none", "Dynamo", "none", "JWT eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJfaWQiOiI1YWQ0YWUxYzIyN2ZlNTExMGQ2Njc0ZWMiLCJpYXQiOjE1MjM4ODc2NDQsImV4cCI6MTU4NzAwMjg0NH0.Kjw-8p2meT2zkCV5ctkGMpqL4VZ6mK_DXLO4XWyMj7w");
    }

    internal void InitReceiverEventsAndGlobals()
    {
     


      myReceiver.OnWsMessage += OnWsMessage;

     

    }

    public virtual void OnWsMessage(object source, SpeckleEventArgs e)
    {
      Console.WriteLine(e);
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {

      if(_registeringPorts)
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };

      var associativeNodes = new List<AssociativeNode>();
      for (var i = 0; i < OutPorts.Count; i++)
      {
        associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(i), inputAstNodes[0]));
      }
      return associativeNodes;
    }

    protected override void AddInput()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + (OutPorts.Count + 1), "")));
      RegisterAllPorts();
    }

    protected override string GetInputName(int index)
    {
      return "item" + index;
    }

    protected override string GetInputTooltip(int index)
    {
      return "Layer " + InPorts[index].Name;
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
