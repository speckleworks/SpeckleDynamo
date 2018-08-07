﻿using Dynamo.Graph;
using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using SpeckleCore;
using SpeckleDynamo.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Xml;
using Dynamo.Models;

namespace SpeckleDynamo
{

  [NodeName("Speckle Receiver")]
  [NodeDescription("Receives data from Speckle.")]
  [NodeCategory("Speckle.I/O")]

  //Inputs
  [InPortNames("ID")]
  [InPortTypes("string")]
  [InPortDescriptions("Stream ID")]

  [IsDesignScriptCompatible]
  public class Receiver : NodeModel
  {
    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _streamId;
    private bool _transmitting;
    private bool _registeringPorts = false;
    private string _message = "Initialising...";
    private bool _paused = false;
    private bool _streamTextBoxEnabled = true;
    private int elCount = 0;
    private int subsetCount = 0;
    private List<object> subset = new List<object>();
    private List<Layer> Layers = new List<Layer>();
    private List<Layer> OldLayers = new List<Layer>();
    private List<SpeckleObject> SpeckleObjects;
    private List<object> ConvertedObjects;
    private bool hasNewData = false;
    private Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();
    internal string AuthToken { get => _authToken; set { _authToken = value; RaisePropertyChanged("AuthToken"); } }
    internal bool Expired = false;
    #region public properties
    public string RestApi { get => _restApi; set { _restApi = value; RaisePropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; RaisePropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; RaisePropertyChanged("Server"); } }
    public string StreamId { get => _streamId; set { _streamId = value; RaisePropertyChanged("StreamId"); } }
    public bool Transmitting { get => _transmitting; set { _transmitting = value; RaisePropertyChanged("Transmitting"); } }
    public string DocumentName = "none";
    public string DocumentGuid = "none";
    internal RunType RunType;

    public string OldStreamId;
    [JsonIgnore]
    public string Message { get => _message; set { _message = value; RaisePropertyChanged("Message"); } }
    public bool Paused { get => _paused; set { _paused = value; RaisePropertyChanged("Paused"); RaisePropertyChanged("Receiving"); } }
    public bool StreamTextBoxEnabled { get => _streamTextBoxEnabled; set { _streamTextBoxEnabled = value; RaisePropertyChanged("StreamTextBoxEnabled"); } }
    [JsonConverter(typeof(SpeckleClientConverter))]
    public SpeckleApiClient myReceiver;
    #endregion


    [JsonConstructor]
    private Receiver(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      var hack = new ConverterHack();
    }

    public Receiver()
    {
      var hack = new ConverterHack();
      Transmitting = false;
      RegisterAllPorts();
    }

    private void DataBridgeCallback(object obj)
    {
      try
      {
        //ID disconnected
        if (!InPorts[0].Connectors.Any() && !StreamTextBoxEnabled && StreamId != null)
        {
          StreamTextBoxEnabled = true;
          StreamId = "";
          ChangeStreams(StreamId);
          return;
        }
        //ID connected
        else if (InPorts[0].Connectors.Any() && obj != null)
        {
          StreamTextBoxEnabled = false;
          var newStreamID = (string)obj;
          if (newStreamID != OldStreamId)
          {
            StreamId = newStreamID;
            ChangeStreams(StreamId);
          }
        }
      }
      catch (Exception ex)
      {
        Warning("Inputs are not formatted correctly");
      }
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      this.ClearErrorsAndWarnings();

      if (_registeringPorts)
        return Enumerable.Empty<AssociativeNode>();
      //probably means that the stream ID has changed
      if (!hasNewData)
      {

        return new[] { AstFactory.BuildAssignment(
                          AstFactory.BuildIdentifier(AstIdentifierBase + "_dummy"),
                          VMDataBridge.DataBridge.GenerateBridgeDataAst(GUID.ToString(), inputAstNodes[0])) };
      }
      else
      {

          Transmitting = false;
          Message = "Got data\n@" + DateTime.Now.ToString("HH:mm:ss");

       
        
        hasNewData = false;
        if (Layers == null || ConvertedObjects.Count == 0)
          return Enumerable.Empty<AssociativeNode>();

        var associativeNodes = new List<AssociativeNode>();
        foreach (Layer layer in Layers)
        {
          try
          {
            subset = ConvertedObjects.GetRange((int)layer.StartIndex, (int)layer.ObjectCount);
          }
          catch (Exception e)
          {
            Console.WriteLine(e);
          }
          if (layer.Topology == "")
          {
            Functions.SpeckleTempData.AddLayerObjects(StreamId + layer.Guid, subset);
          }

          else
          {
            //HIC SVNT DRACONES
            var tree = new List<object>();
            var treeTopo = layer.Topology.Split(' ').Where(x => x != "");
            subsetCount = 0;
            foreach (var branch in treeTopo)
            {

              var branchTopo = branch.Split('-')[0].Split(';');
              var branchIndexes = new List<int>();
              foreach (var t in branchTopo)
                branchIndexes.Add(Convert.ToInt32(t));

              elCount = Convert.ToInt32(branch.Split('-')[1]);

              RecursivelyCreateNestedLists(tree, branchIndexes, 0);

            }

            object output;

            //if only one branch simplify output structure
            if (treeTopo.Count() == 1)
            {
              //if only one item simplify output structure
              if (tree[0] is List<object> && (tree[0] as List<object>).Count == 1)
                output = (tree[0] as List<object>)[0];
              else
                output = tree[0];
            }

            else
              output = tree;

            Functions.SpeckleTempData.AddLayerObjects(StreamId + layer.Guid, output);

          }



          var functionCall = AstFactory.BuildFunctionCall(
           new Func<string, object>(Functions.Functions.SpeckleOutput),
           new List<AssociativeNode>
           {
                AstFactory.BuildStringNode(StreamId+layer.Guid)
           });
          try
          {
            associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex((int)layer.OrderIndex), functionCall));
          }
          catch { }

        }
        return associativeNodes;
      }

    }

    private void RecursivelyCreateNestedLists(List<object> tree, List<int> branchIndexes, int currentIndexPosition)
    {
      try
      {
        var index = branchIndexes[currentIndexPosition];
        //add missing branches
        while (tree.Count <= index || !(tree.ElementAt(index) is List<object>))
        {
          if (tree.Count > index)
            tree.Insert(index, new List<object>());
          else
            tree.Add(new List<object>());
        }
        //when reaching the end of the path just add the elements
        if (branchIndexes.Count == currentIndexPosition + 1)
        {
          for (int i = 0; i < elCount; i++)
            (tree[index] as List<object>).Add(subset[subsetCount + i]);
          subsetCount += elCount;


          return;
        }
        RecursivelyCreateNestedLists((tree[index] as List<object>), branchIndexes, currentIndexPosition + 1);

      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

    }

    public virtual void UpdateGlobal()
    {
      var getStream = myReceiver.StreamGetAsync(myReceiver.StreamId, null);
      getStream.Wait();

      this.Name = getStream.Result.Resource.Name;
      Layers = getStream.Result.Resource.Layers.ToList();

      // TODO: check statement below with dimitrie
      // we can safely omit the displayValue, since this is rhino!
      Message = "Getting objects";

      var payload = getStream.Result.Resource.Objects.Select(obj => obj._id).ToArray(); //.Where(o => !ObjectCache.ContainsKey(o._id)).

      myReceiver.ObjectGetBulkAsync(payload, "omit=displayValue").ContinueWith(tres =>
      {
        //add to cache
        foreach (var x in tres.Result.Resources)
          ObjectCache[x._id] = x;

        // populate real objects
        SpeckleObjects.Clear();
        foreach (var obj in getStream.Result.Resource.Objects)
          SpeckleObjects.Add(ObjectCache[obj._id]);

        this.Message = "Converting objects";
        ConvertedObjects = SpeckleCore.Converter.Deserialise(SpeckleObjects);

        if (ConvertedObjects.Count != SpeckleObjects.Count)
        {
          Console.WriteLine("Some objects failed to convert.");
        }

        if(RunType == RunType.Manual)
          this.Message = "Update available since " + DateTime.Now;
        else
          this.Message = "Updating...";

        //expire node on main thread
        hasNewData = true;
        UpdateOutputStructure();
        ExpireNode();
      });
    }
   


    public virtual void UpdateMeta()
    {
      var result = myReceiver.StreamGetAsync(myReceiver.StreamId, "fields=name,layers").Result;

      this.Name = result.Resource.Name;
      Layers = result.Resource.Layers.ToList();

      UpdateOutputStructure();
      Transmitting = false;
    }

    public virtual void UpdateChildren()
    {
      var result = myReceiver.StreamGetAsync(myReceiver.StreamId, "fields=children").Result;
      myReceiver.Stream.Children = result.Resource.Children;
      Transmitting = false;
    }

    private void UpdateOutputStructure()
    {
        List<Layer> toRemove, toAdd, toUpdate;
        toRemove = new List<Layer>();
        toAdd = new List<Layer>();
        toUpdate = new List<Layer>();

        Layer.DiffLayerLists(OldLayers, Layers, ref toRemove, ref toAdd, ref toUpdate);
        OldLayers = Layers;

        if (toRemove.Count == 0 && toAdd.Count == 0)
          return;

      this.DispatchOnUIThread(() =>
      {

        _registeringPorts = true;
       
        //port was renamed, collect connectins and then try restore them
        if(toRemove.Count == toAdd.Count && toRemove.Count == OutPorts.Count)
        {
          //collect connections
          List<List<PortModel>> endPorts = new List<List<PortModel>>();
          foreach (var i in OutPorts)
          {
            endPorts.Add(new List<PortModel>());
            foreach (var c in i.Connectors)
              endPorts.Last().Add(c.End);
          }
          OutPorts.RemoveAll((p) => { return true; });
          for (var i = 0; i < toAdd.Count; i++)
          {
            var layer = toAdd[i];
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData(layer.Name, layer.Guid)));
            foreach (var e in endPorts[i])
              OutPorts.Last().Connectors.Add(new ConnectorModel(OutPorts.Last(),e, Guid.NewGuid()));
          }
        }
        else
        {
          foreach (Layer layer in toRemove)
          {
            var port = OutPorts.FirstOrDefault(item => { return item.Name == layer.Name; });
            if (port != null)
              OutPorts.Remove(port);
          }
          foreach (var layer in toAdd)
          {
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData(layer.Name, layer.Guid)));
          }
        }
       
        //can't rename ports, so no need for this
        //foreach (var layer in toUpdate)
        //{
         //...
        //}

        RegisterAllPorts();
        _registeringPorts =false;
      });
    }

    public void ExpireNode()
    {
        OnNodeModified(forceExecute: true);
    }

    internal void InitReceiverEventsAndGlobals()
    {
      ObjectCache = new Dictionary<string, SpeckleObject>();

      SpeckleObjects = new List<SpeckleObject>();

      ConvertedObjects = new List<object>();

      if(myReceiver.IsConnected && !Paused)
        UpdateGlobal();
      else
        myReceiver.OnReady += (sender, e) =>
        {
          if (!Paused)
            UpdateGlobal();
        };

      myReceiver.OnWsMessage += OnWsMessage;

      myReceiver.OnError += (sender, e) =>
      {
        if (e.EventName == "websocket-disconnected")
          return;
        Warning(e.EventName + ": " + e.EventData);
      };

    }

    internal void StreamChanged()
    {
      ChangeStreams(StreamId);
    }

    internal void ChangeStreams(string StreamId)
    {
      if (StreamId == OldStreamId)
        return;
      Transmitting = true;
      OldStreamId = StreamId;

      Console.WriteLine("Changing streams...");

      if (myReceiver != null)
        myReceiver.Dispose(true);

      if (StreamId == "")
      {
        ResetReceiver();
        return;
      }

      myReceiver = new SpeckleApiClient(RestApi, true);

      InitReceiverEventsAndGlobals();
      myReceiver.IntializeReceiver(StreamId, DocumentName, "Dynamo", DocumentGuid, AuthToken);
    }

    private void ResetReceiver()
    {
      Layers = new List<Layer>();
      ObjectCache = new Dictionary<string, SpeckleObject>();
      SpeckleObjects = new List<SpeckleObject>();
      ConvertedObjects = new List<object>();
      this.DispatchOnUIThread(() => OutPorts.RemoveAll((p) => { return true; }));
      Message = "";
      Transmitting = false;
      Name = "Speckle Receiver";
    }

    internal void AddedToDocument(object sender, System.EventArgs e)
    {
      //saved receiver
      if (myReceiver != null)
      {
       // this.DispatchOnUIThread(() => OutPorts.RemoveAll((p) => { return true; }));
        Message = "";
        Transmitting = false;
        AuthToken = Utils.Accounts.GetAuthToken(Email, RestApi);
        OldLayers = OutPorts.Select(x => new Layer(x.Name, x.ToolTip, "", 0, 0, 0)).ToList();
        InitReceiverEventsAndGlobals();
        return;
      }
      Transmitting = true;
      var myForm = new SpecklePopup.MainWindow();
      //TODO: fix this it's crashing revit
      //myForm.Owner = Application.Current.MainWindow;
      this.DispatchOnUIThread(() =>
      {
        //if default account exists form is closed automatically
        if (!myForm.HasDefaultAccount)
          myForm.ShowDialog();
        if (myForm.restApi != null && myForm.apitoken != null)
        {
          Email = myForm.selectedEmail;
          Server = myForm.selectedServer;

          RestApi = myForm.restApi;
          AuthToken = myForm.apitoken;

          Message = "";
        }
        else
        {
          Message = "";
          Error("Account selection failed.");
        }
        Transmitting = false;
      });
    }

    internal void PausePlayButtonClick(object sender, RoutedEventArgs e)
    {
      
      Paused = !Paused;
      //if there's new data, get it on resume
      if (Expired && !Paused)
      {
        Expired = false;
        //TODO: instead, we could store it in a local cache and release it
        UpdateGlobal();
      }
    }

    public virtual void OnWsMessage(object source, SpeckleEventArgs e)
    {
      //node disconnected before event was received
      if (string.IsNullOrEmpty(StreamId))
        return;
      if (Paused)
      {
        Message = "Update available since " + DateTime.Now;
        Expired = true;
        return;
      }
      Transmitting = true;
      switch ((string)e.EventObject.args.eventType)
      {
        case "update-global":
          UpdateGlobal();
          break;
        case "update-meta":
          UpdateMeta();
          break;
        case "update-name":
          UpdateMeta();
          break;
        case "update-children":
          UpdateChildren();
          break;
        default:
          //CustomMessageHandler((string)e.EventObject.args.eventType, e);
          break;
      }
    }

    protected override void OnBuilt()
    {
      base.OnBuilt();
      VMDataBridge.DataBridge.Instance.RegisterCallback(GUID.ToString(), DataBridgeCallback);
    }

    public override void Dispose()
    {
      if (myReceiver != null)
        myReceiver.Dispose();
      base.Dispose();

      VMDataBridge.DataBridge.Instance.UnregisterCallback(GUID.ToString());
    }



    #region Serialization/Deserialization Methods

    protected override void SerializeCore(XmlElement element, SaveContext context)
    {
      base.SerializeCore(element, context); // Base implementation must be called.
      if (myReceiver == null)
        return;

      //https://stackoverflow.com/questions/13674395/no-map-for-object-error-when-deserializing-object
      using (var input = new MemoryStream())
      {
        var formatter = new BinaryFormatter();
        formatter.Serialize(input, myReceiver);
        input.Seek(0, SeekOrigin.Begin);

        using (MemoryStream output = new MemoryStream())
        using (DeflateStream deflateStream = new DeflateStream(output, CompressionMode.Compress))
        {
          input.CopyTo(deflateStream);
          deflateStream.Close();

          var client = Convert.ToBase64String(output.ToArray());

          var xmlDocument = element.OwnerDocument;
          var subNode = xmlDocument.CreateElement("Speckle");
          subNode.SetAttribute("speckleclient", client);
          //could be part of the sender
          subNode.SetAttribute("stream", StreamId);
          subNode.SetAttribute("email", Email);
          subNode.SetAttribute("server", Server);
          subNode.SetAttribute("paused", Paused.ToString());
          subNode.SetAttribute("streamTextBoxEnabled", StreamTextBoxEnabled.ToString());
          element.AppendChild(subNode);
        }
      }
    }

    protected override void DeserializeCore(XmlElement element, SaveContext context)
    {
      base.DeserializeCore(element, context); //Base implementation must be called.

      foreach (XmlNode subNode in element.ChildNodes)
      {
        if (!subNode.Name.Equals("Speckle"))
          continue;
        if (subNode.Attributes == null || (subNode.Attributes.Count <= 0))
          continue;

       // _coldStart = true;
        foreach (XmlAttribute attr in subNode.Attributes)
        {
          switch (attr.Name)
          {
            case "speckleclient":
              using (MemoryStream input = new MemoryStream(Convert.FromBase64String(attr.Value)))
              using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
              using (MemoryStream output = new MemoryStream())
              {
                deflateStream.CopyTo(output);
                deflateStream.Close();
                output.Seek(0, SeekOrigin.Begin);

                BinaryFormatter bformatter = new BinaryFormatter();
                myReceiver = (SpeckleApiClient)bformatter.Deserialize(output);
                RestApi = myReceiver.BaseUrl;
              }
              break;
            case "stream":
              StreamId = attr.Value;
              break;
            case "email":
              Email = attr.Value;
              break;
            case "server":
              Server = attr.Value;
              break;
            case "paused":
              Paused = bool.Parse(attr.Value);
              break;
            case "streamTextBoxEnabled":
              StreamTextBoxEnabled = bool.Parse(attr.Value);
              break;
            default:
              Log(string.Format("{0} attribute could not be deserialized for {1}", attr.Name, GetType()));
              break;
          }
        }

        break;
      }
    }

    #endregion
  }
}
