extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using System.Collections.Generic;
using Dynamo.Utilities;
using System;
using System.Linq;
using System.Globalization;
using ProtoCore.SyntaxAnalysis.Imperative;
using ProtoCore.AST;
using System.Xml;
using Dynamo.Configuration;
using Dynamo.Engine;
using Dynamo.Engine.CodeGeneration;

using ProtoCore.SyntaxAnalysis;

using ArrayNode = ProtoCore.AST.AssociativeAST.ArrayNode;
using Dynamo.Wpf;
using Dynamo.Controls;
using Dynamo.Nodes;
using System.Windows.Controls;
using ProtoCore.Utils;

namespace Test
{
  [NodeName("Test")]
  [NodeDescription("Tetst.")]
  [NodeCategory("Test")]
  [NodeSearchTags("Test")]
  //[IsDesignScriptCompatible]
  public class Test : VariableInputNode
  {
    public LibraryServices libraryServices;
    private List<string> portnames = new List<string>();
    private List<StringNode> outnodes = new List<StringNode>();

    [DNJ.JsonConstructor]
    private Test(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      ArgumentLacing = LacingStrategy.Disabled;
    }

    public Test()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("In", "")));
      //this.libraryServices = libraryServices;
      //OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + (1), "")));
      //OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + (2), "")));
      //OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out" + (3), "")));
      RegisterAllPorts();
    }

    protected override void AddInput()
    {
      var ports = OutPorts.Count;
      portnames = GetDefinedVariableNames();

      OutPorts.RemoveAll((p) => { return true; });

     

      foreach (var p in portnames)
        OutPorts.Add(new PortModel(PortType.Output, this, new PortData(p, "")));
       RegisterAllPorts();
    }

    protected override void RemoveInput()
    {
        OnNodeModified(true);
    }
    internal List<string> GetDefinedVariableNames()
    {
      var names = new List<string>();
      var ports = OutPorts.Count;
      for (var i = 0; i < ports + 1; i++)
      {
          names.Add("var_"+Guid.NewGuid().ToString().Substring(0, 4)+"_"+i);
      }
      return names;
    }

    private void GenerateOutNodes()
    {
      outnodes.Clear();
      for (var i = 0; i < OutPorts.Count + 1; i++)
      {
        outnodes.Add(AstFactory.BuildStringNode((DateTime.Now.ToLongTimeString())));
      }
    }

    private bool ShouldBeRenamed(string ident)
    {
      return !ident.Equals(AstIdentifierForPreview.Value) && GetDefinedVariableNames().Contains(ident);
    }

    private string LocalizeIdentifier(string identifierName)
    {
      return string.Format("{0}_{1}", identifierName, AstIdentifierGuid);
    }

    public override IdentifierNode GetAstIdentifierForOutputIndex(int outputIndex)
    {
      return GetAstIdentifierForOutputIndexInternal(outputIndex, false);
    }

    private IdentifierNode GetAstIdentifierForOutputIndexInternal(int portIndex, bool forRawName)
    {
      

      var ass = outnodes[portIndex] as AssociativeNode;
      var identNode = ass as IdentifierNode;
      if (identNode == null)
        return null;

      var mappedIdent = NodeUtils.Clone(identNode);

      if (!forRawName)
      {
        var identMapper = new IdentifierInPlaceMapper(libraryServices.LibraryManagementCore, ShouldBeRenamed, LocalizeIdentifier);
        mappedIdent.Accept(identMapper);
      }

      return mappedIdent as IdentifierNode;
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      GenerateOutNodes();
      var associativeNodes = new List<AssociativeNode>();
      for (var p =0; p< portnames.Count; p++)
      {
        associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(p), outnodes[p]));
      }
      return associativeNodes;
      
    }


    public override string AstIdentifierBase
    {
      get
      {
        return "aa";
      }
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
  public class IdentifierInPlaceMapper : AstReplacer
  {
    private ProtoCore.Core core;
    private Func<string, string> mapper;
    private Func<string, bool> cond;

    public IdentifierInPlaceMapper(ProtoCore.Core core, Func<string, bool> cond, Func<string, string> mapper)
    {
      this.core = core;
      this.cond = cond;
      this.mapper = mapper;
    }

    public override AssociativeNode VisitIdentifierNode(IdentifierNode node)
    {
      var variable = node.Value;
      if (cond(variable))
        node.Value = node.Name = mapper(variable);

      return base.VisitIdentifierNode(node);
    }

    public override AssociativeNode VisitIdentifierListNode(IdentifierListNode node)
    {
      node.LeftNode = node.LeftNode.Accept(this);

      var rightNode = node.RightNode;
      while (rightNode != null)
      {
        if (rightNode is FunctionCallNode)
        {
          var funcCall = rightNode as FunctionCallNode;
          funcCall.FormalArguments = VisitNodeList(funcCall.FormalArguments);
          if (funcCall.ArrayDimensions != null)
          {
            funcCall.ArrayDimensions = funcCall.ArrayDimensions.Accept(this) as ArrayNode;
          }
          break;
        }
        else if (rightNode is IdentifierListNode)
        {
          rightNode = (rightNode as IdentifierListNode).RightNode;
        }
        else
        {
          break;
        }
      }

      return node;
    }


  }

  public class TestViewCustomization : INodeViewCustomization<Test>
  {
    private Test _receiver;

    public void CustomizeView(Test model, NodeView nodeView)
    {
      StackPanel s = new StackPanel();
      s.Orientation = Orientation.Horizontal;

      //add remove input buttons
      var addButton = new DynamoNodeButton(nodeView.ViewModel.NodeModel, "AddInPort") { Content = "+", Width = 20 };
      var subButton = new DynamoNodeButton(nodeView.ViewModel.NodeModel, "RemoveInPort") { Content = "-", Width = 20 };
      s.Children.Add(addButton);
      s.Children.Add(subButton);
      nodeView.inputGrid.Children.Add(s);

      _receiver = model;

      //bindings

      _receiver.libraryServices = nodeView.ViewModel.DynamoViewModel.EngineController.LibraryServices;
    }

    public void Dispose()
    {
    }

    //private void ExpireNode(object sender, System.EventArgs e)
    //{
    //  _receiver.ExpireNode();
    //}



  }
}
