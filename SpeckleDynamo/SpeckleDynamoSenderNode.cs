using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using System;
using System.Collections.Generic;

namespace SpeckleDynamo
{
    [NodeName("Data Sender")]
    [NodeDescription("Sends data tp Speckle.")]
    [NodeCategory("Speckle.IO")]
    //Outputs
    [OutPortNames("Log","ID")]
    [OutPortDescriptions("Log Data","Stream ID")]
    [OutPortTypes("string", "string")]

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

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
        }

    }
}
