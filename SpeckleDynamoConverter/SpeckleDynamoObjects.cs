extern alias SpeckleNewtonsoft;
using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using SpeckleCore;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Dynamo.Graph.Workspaces;
using SpeckleNewtonsoft.Newtonsoft.Json;

namespace SpeckleDynamoConverter
{
  public enum SpeckleDynamoEventType
  {
    Added,
    Removed
  }

  public class SpeckleNodeEvent
  {
    // public SpeckleDynamoEventType NodeEventType { get; set; }
    // public string Name { get; set; }
    public string Json { get; set; }
    // public string Id { get; set; }
    //public double X { get; set; }
    // public double Y { get; set; }
  }
  public class SpeckleDynamoWorkspace
  {

    public List<NodeModel> Nodes { get; set; }
    public List<ConnectorModel> Connectors { get; set; }
    public ExtraWorkspaceViewInfo View { get; set; }


  }

  public class NodeView
  {
    public bool ShowGeometry { get; set; }
    public string Name { get; set; }
    [JsonConverter(typeof(IdToGuidConverter))]
    public string Id { get; set; }
    public bool IsSetAsInput { get; set; }
    public bool IsSetAsOutput { get; set; }
    public bool Excluded { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
  }

  [Serializable]
  public class SpeckleConnectorEvent
  {
    public SpeckleDynamoEventType ConnectorEventType { get; set; }
    public string StartId { get; set; }
    public int StartPortIndex { get; set; }
    public string EndId { get; set; }
    public int EndPortIndex { get; set; }
  }
}
