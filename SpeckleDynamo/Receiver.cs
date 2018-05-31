using Dynamo.Graph;
using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using SpeckleCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Xml;

namespace SpeckleDynamo
{

  [NodeName("Speckle Receiver")]
  [NodeDescription("Receives data from Speckle.")]
  [NodeCategory("Speckle.IO")]

  //Inputs
  [InPortNames("ID")]
  [InPortTypes("string")]
  [InPortDescriptions("Stream ID")]

  [IsDesignScriptCompatible]
  public class Receiver : NodeModel, INotifyPropertyChanged
  {
    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _streamId;
    private string _oldStreamId;
    private string _message = "Initialising...";
    private bool _paused = false;
    private bool _streamTextBoxEnabled = true;
    private bool _coldStart = false;

    private int elCount = 0;
    private int subsetCount = 0;
    private List<object> subset = new List<object>();

    private bool _registeringPorts = false;

    internal string AuthToken { get => _authToken; set { _authToken = value; NotifyPropertyChanged("AuthToken"); } }
    public string RestApi { get => _restApi; set { _restApi = value; NotifyPropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; NotifyPropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; NotifyPropertyChanged("Server"); } }
    public string StreamId { get => _streamId; set
      { _streamId = value; NotifyPropertyChanged("StreamId");
      } }

    public string Message { get => _message; set { _message = value; NotifyPropertyChanged("Message"); } }
    public bool Paused { get => _paused; set { _paused = value; NotifyPropertyChanged("Paused"); NotifyPropertyChanged("Receiving"); } }
    public bool StreamTextBoxEnabled { get => _streamTextBoxEnabled; set { _streamTextBoxEnabled = value; NotifyPropertyChanged("StreamTextBoxEnabled");  } }
    //could instead use another value converter
    internal bool Receiving { get => !_paused; }
    internal bool Expired = false;

    internal SpeckleApiClient myReceiver;
    List<Layer> Layers;
    List<SpeckleObject> SpeckleObjects;
    List<object> ConvertedObjects;

    private readonly SynchronizationContext _context;
    private bool hasNewData = false;


    private Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();

    public Receiver()
    {
      var hack = new ConverterHack();

      RegisterAllPorts();

      //for handling code execution on main thread, better ideas welcome
      _context = SynchronizationContext.Current;
    }

    private void DataBridgeCallback(object obj)
    {
      try
      {
        //ID disconnected
        if (!HasConnectedInput(0) && !StreamTextBoxEnabled && StreamId!=null)
        {
          StreamTextBoxEnabled = true;
          StreamId = "";
          ChangeStreams(StreamId);
          return;
        }
        //ID connected
        else if (HasConnectedInput(0) && obj!=null)
        {
          StreamTextBoxEnabled = false;
          var newStreamID = (string)obj;
          if (newStreamID != _oldStreamId)
          {
            StreamId = newStreamID;
            ChangeStreams(StreamId);
          }
        }
       
       
      }
      catch (Exception ex)
      {
        throw new WarningException("Inputs are not formatted correctly");
      }
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {

      if (_registeringPorts)
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      //probably means that the stream ID has changed
      else if (!hasNewData)
      {

        return new[] { AstFactory.BuildAssignment(
                          AstFactory.BuildIdentifier(AstIdentifierBase + "_dummy"),
                          VMDataBridge.DataBridge.GenerateBridgeDataAst(GUID.ToString(), inputAstNodes[0])) };
      }
      else 
      {
        Message = "Got data\n@" + DateTime.Now.ToString("HH:mm:ss");
        hasNewData = false;
        if (Layers == null || ConvertedObjects.Count == 0)
          return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };

        var associativeNodes = new List<AssociativeNode>();
        foreach (Layer layer in Layers)
        {

          subset = ConvertedObjects.GetRange((int)layer.StartIndex, (int)layer.ObjectCount);
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

          associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex((int)layer.OrderIndex), functionCall));

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

      NickName = getStream.Result.Resource.Name;
      Layers = getStream.Result.Resource.Layers.ToList();

      // TODO: check statement below with dimitrie
      // we can safely omit the displayValue, since this is rhino!
      Message = "Getting objects";

      var payload = getStream.Result.Resource.Objects.Where(o => !ObjectCache.ContainsKey(o._id)).Select(obj => obj._id).ToArray();

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

        this.Message = "Updating...";


       
        //expire node on main thread
        hasNewData = true;
        _context.Post(ExpireNode, "");

      });
    }



    public virtual void UpdateMeta()
    {
      var result = myReceiver.StreamGetAsync(myReceiver.StreamId, "fields=name,layers").Result;

      NickName = result.Resource.Name;
      Layers = result.Resource.Layers.ToList();
      //run on main thread
      _context.Post(UpdateOutputStructure, "");
    }

    public virtual void UpdateChildren()
    {
      var result = myReceiver.StreamGetAsync(myReceiver.StreamId, "fields=children").Result;
      myReceiver.Stream.Children = result.Resource.Children;
    }

    public void UpdateOutputStructure()
    {
      List<Layer> toRemove, toAdd, toUpdate;
      toRemove = new List<Layer>();
      toAdd = new List<Layer>();
      toUpdate = new List<Layer>();

      Layer.DiffLayerLists(GetLayers(), Layers, ref toRemove, ref toAdd, ref toUpdate);

      foreach (Layer layer in toRemove)
      {
        var myparam = OutPortData.FirstOrDefault(item => { return item.NickName == layer.Name; });

        if (myparam != null)
          OutPortData.Remove(myparam);
      }

      foreach (var layer in toAdd)
      {
        OutPortData.Add(getGhParameter(layer));
      }

      foreach (var layer in toUpdate)
      {
        var myparam = OutPortData.FirstOrDefault(item => { return item.ToolTipString == layer.Guid; });
        myparam.NickName = layer.Name;
      }

      _registeringPorts = true;
      RegisterOutputPorts();
      ValidateConnections();
      _registeringPorts = false;
    }

    public List<Layer> GetLayers()
    {
      List<Layer> layers = new List<Layer>();
      int startIndex = 0;
      int count = 0;
      foreach (var myParam in OutPortData)
      {
        // NOTE: For gh receivers, we store the original guid of the sender component layer inside the parametr name.
        Layer myLayer = new Layer(
            myParam.NickName,
            myParam.ToolTipString,
            "",  //todo: check this
            0, //todo: check this
            startIndex,
            count);

        layers.Add(myLayer);
        // startIndex += myParam.VolatileDataCount;
        count++;
      }
      return layers;
    }

    private PortData getGhParameter(Layer param)
    {
      //guid stored in tooltip!
      PortData newParam = new PortData(param.Name, param.Guid);
      return newParam;
    }

    public void UpdateOutputStructure(object o)
    {
      UpdateOutputStructure();
    }
    private void ExpireNode(object o)
    {
      UpdateOutputStructure();
      ExpireNode();
    }
    public void ExpireNode()
    {
      if (_coldStart)
      {
        var coldStart = new System.Timers.Timer(2000) { AutoReset = false, Enabled = true };
        coldStart.Elapsed += (sender, e) =>
        {
          OnNodeModified(true);
        };
      }
      else
        OnNodeModified(true);
    }

    internal void InitReceiverEventsAndGlobals()
    {
      ObjectCache = new Dictionary<string, SpeckleObject>();

      SpeckleObjects = new List<SpeckleObject>();

      ConvertedObjects = new List<object>();

      myReceiver.OnReady += (sender, e) =>
      {
        //could be paused if saved receiver
        if (!Paused)
        {
          UpdateGlobal();
        }
          
      };

      myReceiver.OnWsMessage += OnWsMessage;

      myReceiver.OnError += (sender, e) =>
      {
        if (e.EventName == "websocket-disconnected")
          return;
        throw new WarningException(e.EventName + ": " + e.EventData);
      };

    }

    internal void Stream_LostFocus(object sender, RoutedEventArgs e)
    {
      ChangeStreams(StreamId);
    }

    private void ChangeStreams(string StreamId)
    {
      this.ClearRuntimeError();

      if (StreamId == _oldStreamId)
        return;
      _oldStreamId = StreamId;

      Console.WriteLine("Changing streams...");

      if (myReceiver != null)
        myReceiver.Dispose(true);

      if(StreamId == "")
      {
        ResetReceiver();
        return;
      }

      myReceiver = new SpeckleApiClient(RestApi, true);

      InitReceiverEventsAndGlobals();

      //TODO: get documentname and guid, not sure how... Maybe with an extension?
      myReceiver.IntializeReceiver(StreamId, "none", "Dynamo", "none", AuthToken);
    }

    private void ResetReceiver()
    {
      Layers = new List<Layer>();
      ObjectCache = new Dictionary<string, SpeckleObject>();
      SpeckleObjects = new List<SpeckleObject>();
      ConvertedObjects = new List<object>();
      _context.Post(UpdateOutputStructure, "");
      Message = "";
      NickName = "Speckle Receiver";
    }

    internal void AddedToDocument(object sender, System.EventArgs e)
    {
      //saved receiver
      if (myReceiver != null)
      {
        Message = "";
        return;
      }

      var myForm = new SpecklePopup.MainWindow();
      myForm.Owner = Application.Current.MainWindow;
      Application.Current.Dispatcher.BeginInvoke((Action)(() =>
      {
        //if default account exists form is closed automatically
        if (myForm.IsActive)
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
          Message = "Account selection failed.";
          return;
        }
      }));
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
      if (Paused)
      {
        Message = "Update available since " + DateTime.Now;
        Expired = true;
        return;
      }

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

        _coldStart = true;
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
                StreamId = myReceiver.StreamId;
                AuthToken = myReceiver.AuthToken;

                InitReceiverEventsAndGlobals();
       

              }
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

    public event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(String info)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(info));
      }
    }

  }
}
