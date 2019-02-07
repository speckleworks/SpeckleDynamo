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
using Dynamo.Graph;
using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using SpeckleCore;
using SpeckleDynamo.Serialization;

namespace SpeckleDynamo
{
  [NodeName("Speckle Sender")]
  [NodeDescription("Sends data to Speckle.")]
  [NodeCategory("Speckle.I/O")]
  [NodeSearchTags("SpeckleSender")]
  [IsDesignScriptCompatible]
  public class Sender : VariableInputNode
  {
    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _streamId;
    private bool _transmitting = false;
    private string _message = "Initialising...";
    private Timer MetadataSender, DataSender;
    private ArrayList DataBridgeData = new ArrayList();
    private string BucketName;
    private List<Layer> BucketLayers = new List<Layer>();
    private List<object> BucketObjects = new List<object>();
    private bool _registeringPorts = false;
    private List<int> branchIndexes = new List<int>();
    private Dictionary<string, int> branches = new Dictionary<string, int>();
    internal Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();
    public string DocumentName = "none";
    public string DocumentGuid = "none";
    internal string Log { get; set; } = "";
    internal string AuthToken { get => _authToken; set { _authToken = value; RaisePropertyChanged("AuthToken"); } }

    #region public properties
    public string RestApi { get => _restApi; set { _restApi = value; RaisePropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; RaisePropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; RaisePropertyChanged("Server"); } }
    public string StreamId { get => _streamId; set { _streamId = value; RaisePropertyChanged("StreamId"); } }
    [JsonIgnore]
    public bool Transmitting { get => _transmitting; set { _transmitting = value; RaisePropertyChanged("Transmitting"); } }
    [JsonIgnore]
    public string Message { get => _message; set { _message = value; RaisePropertyChanged("Message"); } }

    [JsonConverter(typeof(SpeckleClientConverter))]
    public SpeckleApiClient mySender;
    #endregion


    [JsonConstructor]
    private Sender(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      var hack = new ConverterHack();
      LocalContext.Init();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    public Sender()
    {
      Transmitting = true;
      var hack = new ConverterHack();
      LocalContext.Init();

      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("Log", "Log Data")));
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("ID", "Stream ID")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("A", "")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("B", "")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("C", "")));

      RegisterAllPorts();
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
      ClearErrorsAndWarnings();

      if (_registeringPorts)
      {
        return Enumerable.Empty<AssociativeNode>();
      }

      if (mySender == null || mySender.StreamId == null || DataSender ==null)
      {
        return Enumerable.Empty<AssociativeNode>();
      }

      var associativeNodes = new List<AssociativeNode> {
                     AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(Log)),
                     AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(1), AstFactory.BuildStringNode(mySender.StreamId)) };

      if (InPorts.Count == 0)
      {
        return associativeNodes;
      }

      //using BridgeData to get value of input from within the node itself
      Transmitting = true;
      Message = "Sending...";
      associativeNodes.Add(AstFactory.BuildAssignment(
                        AstFactory.BuildIdentifier(AstIdentifierBase + "_dummy"),
                        VMDataBridge.DataBridge.GenerateBridgeDataAst(GUID.ToString(), AstFactory.BuildExprList(inputAstNodes))));

      return associativeNodes;
    }

    public void UpdateData()
    {
      BucketName = Name;
      BucketLayers = GetLayers();

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
      {
        RecursivelyCreateTopology(data as ArrayList);
      }
      else
      {
        RecursivelyCreateTopology(new ArrayList { data });
      }
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
            {
              branchIndexes.RemoveAt(branchIndexes.Count - 1);
            }
          }
          else
          {
            if (!branchIndexes.Any())
            {
              branchIndexes.Add(0);
            }

            var thisBranchTopo = string.Join(";", branchIndexes.Select(x => x.ToString()));
            if (branches.ContainsKey(thisBranchTopo))
            {
              branches[thisBranchTopo] = branches[thisBranchTopo] + 1;
            }
            else
            {
              branches[thisBranchTopo] = 1;
            }
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
        {
          RecursivelyFlattenData(list[i] as ArrayList, flattenedList);
        }
        else
        {
          flattenedList.Add(list[i]);
        }

      }
    }

    internal void ForceSendClick(object sender, RoutedEventArgs e)
    {
      Transmitting = true;
      Message = "Sending...";
      ExpireNode();

    }

    internal void AddedToDocument(object sender, System.EventArgs e)
    {
      //saved sender
      if (mySender != null)
      {
        AuthToken = mySender.AuthToken;
        if (string.IsNullOrEmpty(AuthToken))
        {
          Warning(@"This sender was created under another account ¯\_(⊙︿⊙)_/¯. Either add it to your Speckle Accounts or create a new sender.");
          Message = "Account error";
          return;
        }
        
        InitializeSender(false);
        Message = "";
        return;
      }

      Message = "Initialising...";
      var myForm = new SpecklePopup.MainWindow(true,true);
      // myForm.Owner = Application.Current.MainWindow;
      DispatchOnUIThread(() =>
      {
        //if default account exists form is closed automatically
        if (!myForm.HasDefaultAccount)
        {
          myForm.ShowDialog();
        }

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
          Transmitting = false;
        }
      });
    }

    private void InitializeSender(bool init)
    {
      if (init)
      {
        mySender.IntializeSender(AuthToken, DocumentName, "Dynamo", DocumentGuid).ContinueWith(task =>
      {
        // ExpireNode();
      });
      }

      mySender.OnReady += (sender, e) =>
        {
          StreamId = mySender.StreamId;
          //this.Locked = false;
          Name = "Anonymous Stream";
          ExpireNode();

        };

      mySender.OnWsMessage += OnWsMessage;

      mySender.OnLogData += (sender, e) =>
      {
        Log += DateTime.Now.ToString("dd:HH:mm:ss ") + e.EventData + "\n";
      };

      mySender.OnError += (sender, e) =>
      {
        Log += DateTime.Now.ToString("dd:HH:mm:ss ") + e.EventData + "\n";
        if (e.EventName == "websocket-disconnected")
        {
          return;
        }

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
      try
      {
        if (MetadataSender.Enabled)
        {
          //  start the timer again, as we need to make sure we're updating
          DataSender.Start();
          return;
        }



        Message = String.Format("Converting {0} \n objects", BucketObjects.Count);

        var convertedObjects = Converter.Serialise(BucketObjects).ToList();

        Message = String.Format("Creating payloads");

        LocalContext.PruneExistingObjects(convertedObjects, mySender.BaseUrl);

        List<SpeckleObject> persistedObjects = new List<SpeckleObject>();

        if (convertedObjects.Count(obj => obj.Type == SpeckleObjectType.Placeholder) != convertedObjects.Count)
        {
          // create the update payloads
          int count = 0;
          var objectUpdatePayloads = new List<List<SpeckleObject>>();
          long totalBucketSize = 0;
          long currentBucketSize = 0;
          var currentBucketObjects = new List<SpeckleObject>();
          var allObjects = new List<SpeckleObject>();
          foreach (SpeckleObject convertedObject in convertedObjects)
          {

            if (count++ % 100 == 0)
            {
              Message = "Converted " + count + " objects out of " + convertedObjects.Count() + ".";
            }

            // size checking & bulk object creation payloads creation
            long size = Converter.getBytes(convertedObject).Length;
            currentBucketSize += size;
            totalBucketSize += size;
            currentBucketObjects.Add(convertedObject);

            // Object is too big?
            if (size > 2e6)
            {

              Warning("This stream contains a super big object. These will fail. Sorry for the bad error message - we're working on improving this.");

              currentBucketObjects.Remove(convertedObject);
            }

            if (currentBucketSize > 5e5) // restrict max to ~500kb; should it be user config? anyway these functions should go into core. at one point. 
            {
              Console.WriteLine("Reached payload limit. Making a new one, current  #: " + objectUpdatePayloads.Count);
              objectUpdatePayloads.Add(currentBucketObjects);
              currentBucketObjects = new List<SpeckleObject>();
              currentBucketSize = 0;
            }
          }

          // add in the last bucket
          if (currentBucketObjects.Count > 0)
          {
            objectUpdatePayloads.Add(currentBucketObjects);
          }

          Console.WriteLine("Finished, payload object update count is: " + objectUpdatePayloads.Count + " total bucket size is (kb) " + totalBucketSize / 1000);

          // create bulk object creation tasks
          int k = 0;
          List<ResponseObject> responses = new List<ResponseObject>();
          foreach (var payload in objectUpdatePayloads)
          {
            Message = String.Format("Sending payload {0} out of {1}", k++, objectUpdatePayloads.Count);

            try
            {
              var objResponse = mySender.ObjectCreateAsync(payload).Result;
              responses.Add(objResponse);
              persistedObjects.AddRange(objResponse.Resources);

              // push sent objects in the cache
              int m = 0;
              foreach (var oL in payload)
              {
                oL._id = objResponse.Resources[m++]._id;

                if (oL.Type != SpeckleObjectType.Placeholder)
                {
                  LocalContext.AddSentObject(oL, mySender.BaseUrl);
                }
              }
            }
            catch (Exception err)
            {
              Error(err.Message);
              return;
            }
          }
        }
        else
        {
          persistedObjects = convertedObjects;
        }

        Message = "Updating stream...";

        // create placeholders for stream update payload
        List<SpeckleObject> placeholders = new List<SpeckleObject>();

        //foreach ( var myResponse in responses )
        foreach (var obj in persistedObjects)
          placeholders.Add(new SpecklePlaceholder() { _id = obj._id });

        SpeckleStream updateStream = new SpeckleStream()
        {
          Layers = BucketLayers,
          Name = BucketName,
          Objects = placeholders
        };

        //// set some base properties (will be overwritten)
        var baseProps = new Dictionary<string, object>();
        baseProps["units"] = "unit";
        baseProps["tolerance"] = "0.001";
        baseProps["angleTolerance"] = "0.001";
        updateStream.BaseProperties = baseProps;

        var response = mySender.StreamUpdateAsync(mySender.StreamId, updateStream).Result;

        mySender.BroadcastMessage(new { eventType = "update-global" });

        Log += response.Message;
        Message = "Data sent\n@" + DateTime.Now.ToString("HH:mm:ss");
        Transmitting = false;
      }
      catch (Exception ex)
      {
        Error(ex.Message);
        Transmitting = false;
      }
    }

    public void UpdateMetadata()
    {
      BucketName = Name;
      BucketLayers = GetLayers();
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
      Transmitting = false;
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


    public void RenameLayers(List<string> layers)
    {

      _registeringPorts = true;
      //collect connections
      List<List<PortModel>> startPorts = new List<List<PortModel>>();
      foreach (var i in InPorts)
      {
        startPorts.Add(new List<PortModel>());
        foreach (var c in i.Connectors)
        {
          startPorts.Last().Add(c.Start);
        }
      }
      InPorts.RemoveAll((p) => { return true; });

      //add new ports and old connections
      for (var i = 0; i < layers.Count; i++)
      {
        InPorts.Add(new PortModel(PortType.Input, this, new PortData(layers[i], "")));
        foreach (var s in startPorts[i])
        {
          InPorts.Last().Connectors.Add(new ConnectorModel(s, InPorts.Last(), Guid.NewGuid()));
        }
      }
      RegisterAllPorts();
      _registeringPorts = false;
      Transmitting = true;
      UpdateMetadata();


    }

    protected override void AddInput()
    {
      var name = GetSequence().ElementAt(InPorts.Count);
      InPorts.Add(new PortModel(PortType.Input, this, new PortData(name, "Layer " + name)));

      if (MetadataSender != null)
      {
        UpdateMetadata();
      }
    }

    protected override void RemoveInput()
    {
      if (InPorts.Count > 1)
      {
        base.RemoveInput();
      }

      if (MetadataSender != null)
      {
        UpdateMetadata();
      }
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
      {
        mySender.Dispose();
      }

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
      return "Layer " + InPorts[index].Name;
    }

    #endregion

    private static IEnumerable<string> GetSequence(string start = "")
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
        {
          chars.Insert(0, 'A');
        }
        else
        {
          chars[i]++;
        }

        yield return chars.ToString();
      }
    }
  }

  public class InputName : INotifyPropertyChanged
  {
    private string _name { get; set; }
    public string Name { get => _name; set { _name = value; RaisePropertyChanged("Name"); } }

    public InputName(string name)
    {
      Name = name;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void RaisePropertyChanged(String info)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(info));
      }
    }
  }
}
