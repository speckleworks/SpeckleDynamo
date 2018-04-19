using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;
using ProtoCore.AST.AssociativeAST;
using System.Collections;
using System.Collections.Generic;
using SpeckleCore;
using System.Linq;
using System;
using System.Windows;
using System.ComponentModel;
using System.Timers;
using System.Text;

namespace SpeckleDynamo
{
  [NodeName("Speckle Sender")]
  [NodeDescription("Sends data to Speckle.")]
  [NodeCategory("Speckle.IO")]

  //Outputs
  [OutPortNames("Log", "ID")]
  [OutPortDescriptions("Log Data", "Stream ID")]
  [OutPortTypes("string", "string")]

  [IsDesignScriptCompatible]
  public class Sender : VariableInputNode, INotifyPropertyChanged
  {


    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _stream;
    private string _message = "Initialising...";

    public string AuthToken { get => _authToken; set { _authToken = value; NotifyPropertyChanged("AuthToken"); } }
    public string RestApi { get => _restApi; set { _restApi = value; NotifyPropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; NotifyPropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; NotifyPropertyChanged("Server"); } }
    public string Stream { get => _stream; set { _stream = value; NotifyPropertyChanged("Stream"); } }
    public string Message { get => _message; set { _message = value; NotifyPropertyChanged("Message"); } }


    public SpeckleApiClient mySender;
    public string Log { get; set; }
    System.Timers.Timer MetadataSender, DataSender;
    private string BucketName;
    private List<Layer> BucketLayers = new List<Layer>();
    private List<object> BucketObjects = new List<object>();


    public Sender()
    {
      //needs to be done here otherwise outports are wiped out upon adding /removing input ports, not sure why
      OutPortData.Add(new PortData("Log", "Log Data"));
      OutPortData.Add(new PortData("ID", "Stream ID"));
      RegisterAllPorts();
      //PropertyChanged += SendData_PropertyChanged;
      ArgumentLacing = LacingStrategy.Disabled;
    }

    protected override void OnBuilt()
    {
      base.OnBuilt();
      VMDataBridge.DataBridge.Instance.RegisterCallback(GUID.ToString(), DataBridgeCallback);
    }

    /// <summary>
    /// Unregister the data bridge callback.
    /// </summary>
    public override void Dispose()
    {
      base.Dispose();
      VMDataBridge.DataBridge.Instance.UnregisterCallback(GUID.ToString());
    }

    /// <summary>
    /// Callback method for DataBridge mechanism.
    /// This callback only gets called once after the BuildOutputAst Function is executed 
    /// This callback casts the response data object.
    /// </summary>
    /// <param name="data">The data passed through the data bridge.</param>
    private void DataBridgeCallback(object obj)
    {
      ArrayList inputs = obj as ArrayList;
      try
      {
        //TODO: handle data conversion
        var data = new List<object>();
        foreach (var i in inputs)
        {
          data.Add(inputs[0]);
        }

        UpdateData(data);

      }
      catch (Exception ex)
      {
        throw new WarningException("Inputs are not formatted correctly");
      }

    }


    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {

      if (!HasConnectedInput(0))
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };


      //using BridgeData to get value of input from within the node itself
      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(Log)),
                     AstFactory.BuildAssignment(
                        AstFactory.BuildIdentifier(AstIdentifierBase + "_dummy"),
                        VMDataBridge.DataBridge.GenerateBridgeDataAst(GUID.ToString(), AstFactory.BuildExprList(inputAstNodes))
                    )};
    }

    public void UpdateData(List<object> data)
    {
      BucketName = this.NickName;
      //TODO: handle layers
      // BucketLayers = this.GetLayers();
      BucketObjects = data;
      //BucketObjects = this.GetData();

      DataSender.Start();
    }

    internal void ForceSendClick(object sender, RoutedEventArgs e)
    {
      ExpireNode();
    }

    internal void PromptAccountSelection(object sender, System.EventArgs e)
    {
      var myForm = new SpecklePopup.MainWindow();
      myForm.Owner = Application.Current.MainWindow;
      Application.Current.Dispatcher.BeginInvoke((Action)(() =>
      {
        myForm.ShowDialog();
        if (myForm.restApi != null && myForm.apitoken != null)
        {
          mySender = new SpeckleApiClient(myForm.restApi);

          Email = myForm.selectedEmail;
          Server = myForm.selectedServer;

          RestApi = myForm.restApi;
          AuthToken = myForm.apitoken;

          Message = "";

          InitializeSender();
        }
        else
        {
          Message = "Account selection failed.";
          return;
        }
      }));
    }

    private void InitializeSender()
    {
      mySender.IntializeSender(AuthToken, "none", "Dynamo", "none").ContinueWith(task =>
      {
        ExpireNode();
      });


      mySender.OnReady += (sender, e) =>
        {
          Stream = mySender.StreamId;
        //this.Locked = false;
        NickName = "Anonymous Stream";
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
        throw new WarningException(e.EventName + ": " + e.EventData);
      };
      //TODO:
      //ExpireComponentAction = () => ExpireSolution(true);

      //ObjectChanged += (sender, e) => UpdateMetadata();

      //foreach (var param in Params.Input)
      //  param.ObjectChanged += (sender, e) => UpdateMetadata();

      MetadataSender = new System.Timers.Timer(1000) { AutoReset = false, Enabled = false };
      //MetadataSender.Elapsed += MetadataSender_Elapsed;

      DataSender = new System.Timers.Timer(2000) { AutoReset = false, Enabled = false };
      DataSender.Elapsed += DataSender_Elapsed;

      //ObjectCache = new Dictionary<string, SpeckleObject>();
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

      var convertedObjects = Converter.Serialise(BucketObjects).ToList();

      //TODO: cache
      //var convertedObjects = Converter.Serialise(BucketObjects).Select(obj =>
      //{
      //  if (ObjectCache.ContainsKey(obj.Hash))
      //    return new SpecklePlaceholder() { Hash = obj.Hash, _id = ObjectCache[obj.Hash]._id };
      //  return obj;
      //}).ToList();

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
        throw new WarningException("This is a humongous update, in the range of ~50mb. For now, create more streams instead of just one massive one! Updates will be faster and snappier, and you can combine them back together at the other end easier.");
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

      var response = mySender.StreamUpdateAsync(mySender.StreamId, updateStream);

      mySender.BroadcastMessage(new { eventType = "update-global" });

      //TODO: add cache
      // put the objects in the cache 
      //int l = 0;
      //foreach (var obj in placeholders)
      //{
      //  ObjectCache[convertedObjects[l].Hash] = placeholders[l];
      //  l++;
      //}

      Log += response.Result.Message;
      //AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Data sent at " + DateTime.Now);
      Message = "Data sent\n@" + DateTime.Now.ToString("HH:mm:ss");
    }

    public virtual void OnWsMessage(object source, SpeckleEventArgs e)
    {
      Console.WriteLine("[Gh Sender] Got a volatile message. Extend this class and implement custom protocols at ease.");
    }

    public void ExpireNode()
    {
      OnNodeModified(true);
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
      InPortData.Last().NickName = string.Join("", GetSequence().ElementAt(InPorts.Count));
    }

    protected override void RemoveInput()
    {
      if (InPorts.Count > 1)
        base.RemoveInput();
    }

    public override bool IsConvertible
    {
      get { return true; }
    }

    protected override void OnConnectorAdded(ConnectorModel obj)
    {
      //TODO: find a better strategy to rename in ports, maybe with a right click? Or with a popup?
      //InPortData[obj.End.Index].NickName = obj.Start.Owner.NickName;
      //InPortData[obj.End.Index].ToolTipString = obj.Start.Owner.NickName;
      //RegisterAllPorts();
      base.OnConnectorAdded(obj);
    }

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
}
