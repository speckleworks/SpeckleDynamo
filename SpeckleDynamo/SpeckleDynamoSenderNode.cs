using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using System;
using System.Collections.Generic;

namespace SpeckleDynamo
{
    [NodeName("DataSender")]
    [NodeDescription("Sends data to Speckle.")]
    [NodeCategory("Speckle.IO")]

    //Inputs
    [InPortNames("A")]
    [InPortDescriptions("Things to be sent around.")]
    [InPortTypes("object")]

    //Outputs
    [OutPortNames("Log", "ID")]
    [OutPortDescriptions("Log Data", "Stream ID")]
    [OutPortTypes("string", "string")]

    [IsDesignScriptCompatible]
    public class SpeckleDynamoSenderNode : VariableInputNode
    {
        public SpeckleDynamoSenderNode()
        {
            RegisterAllPorts();
        }

        protected override string GetInputName(int index)
        {
            return index.ToString();
        }

        protected override string GetInputTooltip(int index)
        {
            return "Layer " + index.ToString();
        }

        protected override void AddInput()
        {
            base.AddInput();
        }

        protected override void RemoveInput()
        {
            base.RemoveInput();
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
        }

    }
}
