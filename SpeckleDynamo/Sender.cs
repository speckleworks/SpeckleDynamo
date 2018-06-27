using Dynamo.Graph;
using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using SpeckleCore;
using SpeckleDynamo.Serialization;
using SpeckleDynamo.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Timers;
using System.Windows;
using System.Xml;

namespace SpeckleDynamo
{
  [NodeName("Speckle Sender")]
  [NodeDescription("Sends data to Speckle.")]
  [NodeCategory("Speckle")]
  [NodeSearchTags("SpeckleSender")]
  [IsDesignScriptCompatible]
  public class Sender : VariableInputNode, INotifyPropertyChanged
  {
    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _streamId;
    private string _message = "Initialising...";
    private Timer MetadataSender, DataSender;
    private ArrayList DataBridgeData = new ArrayList();
    private string BucketName;
    private List<Layer> BucketLayers = new List<Layer>();
    private List<object> BucketObjects = new List<object>();
    private bool _registeringPorts = false;
    private List<int> branchIndexes = new List<int>();
    private Dictionary<string, int> branches = new Dictionary<string, int>();
    private ObservableCollectionEx<InputName> _inputs = new ObservableCollectionEx<InputName> { new InputName("A"), new InputName("B"), new InputName("C") };
    internal Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();
    public string DocumentName = "none";
    public string DocumentGuid = "none";
    internal string Log { get; set; }
    internal string AuthToken { get => _authToken; set { _authToken = value; NotifyPropertyChanged("AuthToken"); } }

    #region public properties
    public string RestApi { get => _restApi; set { _restApi = value; NotifyPropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; NotifyPropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; NotifyPropertyChanged("Server"); } }
    public string StreamId { get => _streamId; set { _streamId = value; NotifyPropertyChanged("StreamId"); } }
    [JsonIgnore]
    public string Message { get => _message; set { _message = value; NotifyPropertyChanged("Message"); } }
    [JsonIgnore]
    public ObservableCollectionEx<InputName> Inputs { get => _inputs; set { _inputs = value; NotifyPropertyChanged("Inputs"); } }
    [JsonConverter(typeof(SpeckleClientConverter))]
    public SpeckleApiClient mySender;
    #endregion
   

    [JsonConstructor]
    private Sender(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      var hack = new ConverterHack();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    public Sender()
    {
      var hack = new ConverterHack();
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("Log", "Log Data")));
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ID", "Stream ID")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("A", "")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("B", "")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("C", "")));

      RegisterAllPorts();

      Inputs.CollectionChanged += Inputs_CollectionChanged;

      ArgumentLacing = LacingStrategy.Disabled;
    }

    /// <summary>
    /// Callback method for DataBridge mechanism.
    /// This callback only gets called once after the BuildOutputAst Function is executed 
    /// This callback casts the response data object.
    /// </summary>
    /// <param name="data">The data passed through the data bridge.</param>
    private void DataBridgeCallback(object obj)
    {
      try
      {
        DataBridgeData = obj as ArrayList;
        UpdateData();

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

      if (mySender == null || Log == null)
        return Enumerable.Empty<AssociativeNode>();

      var associativeNodes = new List<AssociativeNode> {
                     AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(Log)),
                     AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(1), AstFactory.BuildStringNode(mySender.StreamId)) };

      //if node expired just because a port was added/removed don't bother updating global
      //if (_updatingPorts > 0)
      //{
      //  _updatingPorts--;
      //  return associativeNodes;
      //}

      if (InPorts.Count == 0)
        return associativeNodes;

      //using BridgeData to get value of input from within the node itself
      associativeNodes.Add(AstFactory.BuildAssignment(
                        AstFactory.BuildIdentifier(AstIdentifierBase + "_dummy"),
                        VMDataBridge.DataBridge.GenerateBridgeDataAst(GUID.ToString(), AstFactory.BuildExprList(inputAstNodes))));

      return associativeNodes;
    }

    public void UpdateData()
    {
      BucketName = this.Name;
      BucketLayers = this.GetLayers();

      BucketObjects = new List<object>();
      RecursivelyFlattenData(DataBridgeData, BucketObjects);

      DataSender.Start();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Layer> GetLayers()
    {
      List<Layer> layers = new List<Layer>();
      int startIndex = 0;
      int orderIndex = 0;
      foreach (var myParam in InPorts)
      {
        var data = DataBridgeData.Count - 1 >= orderIndex ? DataBridgeData[orderIndex] : null;
        GetTopology(data);

        var topo = string.Join(" ", branches.Select(x => x.Key + "-" + x.Value));
        var count = branches.Sum(x => x.Value);


        Layer myLayer = new Layer(
            myParam.Name,
            myParam.GUID.ToString(),
            topo,
            count,
            startIndex,
            orderIndex);

        layers.Add(myLayer);
        startIndex += count;
        orderIndex++;
      }
      return layers;
    }

    private void GetTopology(object data)
    {
      branches = new Dictionary<string, int>();
      branchIndexes = new List<int>();

      //recursion takes an ArrayList
      if (data is ArrayList)
        RecursivelyCreateTopology(data as ArrayList);
      else
        RecursivelyCreateTopology(new ArrayList { data });
    }

    private void RecursivelyCreateTopology(ArrayList list)
    {
      try
      {
        for (int i = 0; i < list.Count; i++)
        {
          var item = list[i];
          if (item is ArrayList)
          {
            branchIndexes.Add(i);
            RecursivelyCreateTopology(item as ArrayList);

            //after adding all items of the branch go back one level
            if (branchIndexes.Any())
              branchIndexes.RemoveAt(branchIndexes.Count - 1);
          }
          else
          {
            if (!branchIndexes.Any())
              branchIndexes.Add(0);
            var thisBranchTopo = string.Join(";", branchIndexes.Select(x => x.ToString()));
            if (branches.ContainsKey(thisBranchTopo))
              branches[thisBranchTopo] = branches[thisBranchTopo] + 1;
            else
              branches[thisBranchTopo] = 1;

          }
        }
      }
      catch (Exception e)
      {
        Warning(e.Message);
      }
    }

    private void RecursivelyFlattenData(ArrayList list, List<object> flattenedList)
    {
      for (int i = 0; i < list.Count; i++)
      {
        if (list[i] is ArrayList)
          RecursivelyFlattenData(list[i] as ArrayList, flattenedList);
        else
        {
          flattenedList.Add(list[i]);
        }

      }
    }

    internal void ForceSendClick(object sender, RoutedEventArgs e)
    {
      ExpireNode();
    }

    internal void AddedToDocument(object sender, System.EventArgs e)
    {
      //saved sender
      if (mySender != null)
      {
        AuthToken = Utils.Accounts.GetAuthToken(Email, RestApi);
        InitializeSender(false);
        Message = "";
        return;
      }

      Message = "Initialising...";
      var myForm = new SpecklePopup.MainWindow();
     // myForm.Owner = Application.Current.MainWindow;
      this.DispatchOnUIThread(() =>
      {
        //if default account exists form is closed automatically
        if (!myForm.HasDefaultAccount)
          myForm.ShowDialog();
        if (myForm.restApi != null && myForm.apitoken != null)
        {
          mySender = new SpeckleApiClient(myForm.restApi);

          Email = myForm.selectedEmail;
          Server = myForm.selectedServer;

          RestApi = myForm.restApi;
          AuthToken = myForm.apitoken;

          InitializeSender(true);
        }
        else
        {
          Message = "";
          Error("Account selection failed.");
        }
      });
    }

    private void InitializeSender(bool init)
    {
      if (init)
        mySender.IntializeSender(AuthToken, DocumentName, "Dynamo", DocumentGuid).ContinueWith(task =>
      {
        // ExpireNode();
      });


      mySender.OnReady += (sender, e) =>
        {
          StreamId = mySender.StreamId;
          //this.Locked = false;
          Name = "Anonymous Stream";
          ExpireNode();
          //Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
        };

      mySender.OnWsMessage += OnWsMessage;

      mySender.OnLogData += (sender, e) =>
      {
        this.Log += DateTime.Now.ToString("dd:HH:mm:ss ") + e.EventData + "\n";
      };

      mySender.OnError += (sender, e) =>
      {
        this.Log += DateTime.Now.ToString("dd:HH:mm:ss ") + e.EventData + "\n";
        if (e.EventName == "websocket-disconnected")
          return;
        Warning(e.EventName + ": " + e.EventData);
      };
      //TODO: check this
      //ExpireComponentAction = () => ExpireSolution(true);
      //this.Modified += (sender) => UpdateMetadata();

      //foreach (var param in Params.Input)
      //  param.ObjectChanged += (sender, e) => UpdateMetadata();

      MetadataSender = new System.Timers.Timer(1000) { AutoReset = false, Enabled = false };
      MetadataSender.Elapsed += MetadataSender_Elapsed;

      DataSender = new System.Timers.Timer(2000) { AutoReset = false, Enabled = false };
      DataSender.Elapsed += DataSender_Elapsed;

      ObjectCache = new Dictionary<string, SpeckleObject>();
    }

    private void DataSender_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (MetadataSender.Enabled)
      {
        //  start the timer again, as we need to make sure we're updating
        DataSender.Start();
        return;
      }

      this.Message = String.Format("Converting {0} \n objects", BucketObjects.Count);

      var convertedObjects = Converter.Serialise(BucketObjects).Select(obj =>
      {
        if (ObjectCache.ContainsKey(obj.Hash))
          return new SpecklePlaceholder() { Hash = obj.Hash, _id = ObjectCache[obj.Hash]._id };
        return obj;
      }).ToList();

      this.Message = String.Format("Creating payloads");

      long totalBucketSize = 0;
      long currentBucketSize = 0;
      List<List<SpeckleObject>> objectUpdatePayloads = new List<List<SpeckleObject>>();
      List<SpeckleObject> currentBucketObjects = new List<SpeckleObject>();
      List<SpeckleObject> allObjects = new List<SpeckleObject>();

      foreach (SpeckleObject convertedObject in convertedObjects)
      {
        long size = Converter.getBytes(convertedObject).Length;
        currentBucketSize += size;
        totalBucketSize += size;
        currentBucketObjects.Add(convertedObject);

        if (currentBucketSize > 5e5) // restrict max to ~500kb; should it be user config? anyway these functions should go into core. at one point. 
        {
          Console.WriteLine("Reached payload limit. Making a new one, current  #: " + objectUpdatePayloads.Count);
          objectUpdatePayloads.Add(currentBucketObjects);
          currentBucketObjects = new List<SpeckleObject>();
          currentBucketSize = 0;
        }
      }

      // add  the last bucket 
      if (currentBucketObjects.Count > 0)
        objectUpdatePayloads.Add(currentBucketObjects);

      Console.WriteLine("Finished, payload object update count is: " + objectUpdatePayloads.Count + " total bucket size is (kb) " + totalBucketSize / 1000);

      if (objectUpdatePayloads.Count > 100)
      {
        Warning("This is a humongous update, in the range of ~50mb. For now, create more streams instead of just one massive one! Updates will be faster and snappier, and you can combine them back together at the other end easier.");
      }

      int k = 0;
      List<ResponseObject> responses = new List<ResponseObject>();
      foreach (var payload in objectUpdatePayloads)
      {
        this.Message = String.Format("Sending payload\n{0} / {1}", k++, objectUpdatePayloads.Count);

        responses.Add(mySender.ObjectCreateAsync(payload).GetAwaiter().GetResult());
      }

      this.Message = "Updating stream...";

      // create placeholders for stream update payload
      List<SpeckleObject> placeholders = new List<SpeckleObject>();
      foreach (var myResponse in responses)
        foreach (var obj in myResponse.Resources) placeholders.Add(new SpecklePlaceholder() { _id = obj._id });

      SpeckleStream updateStream = new SpeckleStream()
      {
        Layers = BucketLayers,
        Name = BucketName,
        Objects = placeholders
      };

      //// set some base properties (will be overwritten)
      //var baseProps = new Dictionary<string, object>();
      //baseProps["units"] = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
      //baseProps["tolerance"] = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      //baseProps["angleTolerance"] = Rhino.RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
      //updateStream.BaseProperties = baseProps;

      var response = mySender.StreamUpdateAsync(mySender.StreamId, updateStream).Result;

      mySender.BroadcastMessage(new { eventType = "update-global" });

      //put the objects in the cache
      int l = 0;
      foreach (var obj in placeholders)
      {
        ObjectCache[convertedObjects[l].Hash] = placeholders[l];
        l++;
      }

      Log += response.Message;
      Message = "Data sent\n@" + DateTime.Now.ToString("HH:mm:ss");

    }

    public void UpdateMetadata()
    {
      BucketName = this.Name;
      BucketLayers = this.GetLayers();
      MetadataSender.Start();
    }

    private void MetadataSender_Elapsed(object sender, ElapsedEventArgs e)
    {
      // we do not need to enque another metadata sending event as the data update superseeds the metadata one.
      if (DataSender.Enabled) { return; };
      SpeckleStream updateStream = new SpeckleStream()
      {
        Name = BucketName,
        Layers = BucketLayers
      };

      var updateResult = mySender.StreamUpdateAsync(mySender.StreamId, updateStream).GetAwaiter().GetResult();

      Log += updateResult.Message;
      mySender.BroadcastMessage(new { eventType = "update-meta" });
    }

    public virtual void OnWsMessage(object source, SpeckleEventArgs e)
    {
      Console.WriteLine("[Gh Sender] Got a volatile message. Extend this class and implement custom protocols at ease.");
    }

    public void ExpireNode()
    {
      OnNodeModified(true);
    }

    #region overrides


    private void Inputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      if (Inputs.Count == InPorts.Count && string.Join(".", Inputs.Select(x => x.Name)) != string.Join(".", InPorts.Select(x => x.Name)))
      {
        _registeringPorts = true;
        //collect connections
        List<List<PortModel>> startPorts = new List<List<PortModel>>();
        foreach(var i in InPorts)
        {
          startPorts.Add(new List<PortModel>());
          foreach(var c in i.Connectors)
            startPorts.Last().Add(c.Start);
        }
        InPorts.RemoveAll((p) => { return true; });

        //add new ports and old connections
        for (var i = 0; i < Inputs.Count; i++)
        {
          InPorts.Add(new PortModel(PortType.Input, this, new PortData(Inputs[i].Name, "")));
          foreach (var s in startPorts[i])
            InPorts.Last().Connectors.Add(new ConnectorModel(s,InPorts.Last(),Guid.NewGuid()));
        }       
        RegisterAllPorts();
        _registeringPorts = false;;
        UpdateMetadata();
      }

    }

    protected override void AddInput()
    {
      var name = GetSequence().ElementAt(InPorts.Count);
      InPorts.Add(new PortModel(PortType.Input, this, new PortData(name, "Layer "+ name)));
      Inputs.Add(new InputName(InPorts.Last().Name));

      if (MetadataSender != null)
        UpdateMetadata();
    }

    protected override void RemoveInput()
    {
      if (InPorts.Count > 1)
      {
        base.RemoveInput();
        Inputs.RemoveAt(Inputs.Count - 1);
      }

      if (MetadataSender != null)
        UpdateMetadata();
    }

    public override bool IsConvertible
    {
      get { return true; }
    }

    protected override void OnConnectorAdded(ConnectorModel obj)
    {
      base.OnConnectorAdded(obj);
    }

    protected override void OnBuilt()
    {
      base.OnBuilt();
      VMDataBridge.DataBridge.Instance.RegisterCallback(GUID.ToString(), DataBridgeCallback);
    }

    public override void Dispose()
    {
      if (mySender != null)
        mySender.Dispose();
      base.Dispose();
      /// Unregister the data bridge callback.
      VMDataBridge.DataBridge.Instance.UnregisterCallback(GUID.ToString());
    }

    protected override string GetInputName(int index)
    {
      return "item" + index;
    }


    protected override string GetInputTooltip(int index)
    {
      return "Layer "+ InPorts[index].Name;
    }

    #endregion

    #region Serialization/Deserialization Methods

    protected override void SerializeCore(XmlElement element, SaveContext context)
    {
      base.SerializeCore(element, context); // Base implementation must be called.
      if (mySender == null)
        return;

      //https://stackoverflow.com/questions/13674395/no-map-for-object-error-when-deserializing-object
      using (var input = new MemoryStream())
      {
        var formatter = new BinaryFormatter();
        formatter.Serialize(input, mySender);
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
                mySender = (SpeckleApiClient)bformatter.Deserialize(output);
                RestApi = mySender.BaseUrl;
                StreamId = mySender.StreamId;
                //_updatingPorts = 0;
              }
              break;
            case "email":
              Email = attr.Value;
              break;
            case "server":
              Server = attr.Value;
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


    static IEnumerable<string> GetSequence(string start = "")
    {
      StringBuilder chars = start == null ? new StringBuilder() : new StringBuilder(start);

      while (true)
      {
        int i = chars.Length - 1;
        while (i >= 0 && chars[i] == 'Z')
        {
          chars[i] = 'A';
          i--;
        }
        if (i == -1)
          chars.Insert(0, 'A');
        else
          chars[i]++;
        yield return chars.ToString();
      }
    }


    public event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(String info)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(info));
      }
    }

  }

  public class InputName : INotifyPropertyChanged
  {
    private string _name { get; set; }
    public string Name { get => _name; set { _name = value; NotifyPropertyChanged("Name"); } }

    public InputName(string name)
    {
      Name = name;
    }

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
