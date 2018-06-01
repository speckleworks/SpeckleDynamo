using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using System.Collections.Generic;
using SpeckleCore;
using System.Threading;
using System.Linq;
using System;
using System.Windows;
using System.ComponentModel;

namespace SpeckleDynamo
{

  [NodeName("Speckle Receiver")]
  [NodeDescription("Receives data from Speckle.")]
  [NodeCategory("Speckle.IO")]

  //Inputs
  [InPortNames("ID")]
  [InPortTypes("string")]
  [InPortDescriptions("Stream ID")]


  //TODO: dynamically generate out ports
  [OutPortNames("A")]
  [OutPortTypes("string")]
  [OutPortDescriptions("No data received yet")]

  [IsDesignScriptCompatible]
  public class Receiver : NodeModel, INotifyPropertyChanged
  {
    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _stream;
    private string _oldStream;
    private string _message = "Initialising...";
    private bool _paused = false;

    private bool _registeringPorts = false;

    public string AuthToken { get => _authToken; set { _authToken = value; NotifyPropertyChanged("AuthToken"); } }
    public string RestApi { get => _restApi; set { _restApi = value; NotifyPropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; NotifyPropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; NotifyPropertyChanged("Server"); } }
    public string Stream { get => _stream; set { _stream = value; NotifyPropertyChanged("Stream"); } }

    public string Message { get => _message; set { _message = value; NotifyPropertyChanged("Message"); } }
    public bool Paused { get => _paused; set { _paused = value; NotifyPropertyChanged("Paused"); NotifyPropertyChanged("Receiving"); } }
    //could instead use another value converter
    public bool Receiving { get => !_paused; }
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

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!hasNewData || _registeringPorts)
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      else
      {
        Message = "Data received\n@" + DateTime.Now.ToString("HH:mm:ss");
        if (Layers == null || ConvertedObjects.Count == 0)
          return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };

        var associativeNodes = new List<AssociativeNode>();
        foreach (Layer layer in Layers)
        {

          var subset = ConvertedObjects.GetRange((int)layer.StartIndex, (int)layer.ObjectCount);
          //if (layer.Topology == "")
          //{
            Functions.SpeckleTempData.AddLayerObjects(Stream+layer.Guid, subset);

            var functionCall = AstFactory.BuildFunctionCall(
             new Func<string, object>(Functions.Functions.SpeckleOutput),
             new List<AssociativeNode>
             {
                AstFactory.BuildStringNode(Stream+layer.Guid)
             });

            associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex((int)layer.OrderIndex), functionCall));
          //}
         
          //else
          //{
          //  //HIC SVNT DRACONES
          //  var tree = new DataTree<object>();
          //  var treeTopo = layer.Topology.Split(' ');
          //  int subsetCount = 0;
          //  foreach (var branch in treeTopo)
          //  {
          //    if (branch != "")
          //    {
          //      var branchTopo = branch.Split('-')[0].Split(';');
          //      var branchIndexes = new List<int>();
          //      foreach (var t in branchTopo) branchIndexes.Add(Convert.ToInt32(t));

          //      var elCount = Convert.ToInt32(branch.Split('-')[1]);
          //      GH_Path myPath = new GH_Path(branchIndexes.ToArray());

          //      for (int i = 0; i < elCount; i++)
          //        tree.EnsurePath(myPath).Add(new Grasshopper.Kernel.Types.GH_ObjectWrapper(subset[subsetCount + i]));
          //      subsetCount += elCount;
          //    }
          //  }

          //}

        }

        return associativeNodes;
      }
    }

    public virtual void UpdateGlobal()
    {

      var getStream = myReceiver.StreamGetAsync(myReceiver.StreamId, null);
      getStream.Wait();

      NickName = getStream.Result.Resource.Name;
      Layers = getStream.Result.Resource.Layers.ToList();

      // TODO: Implement cache
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

        _context.Post(UpdateOutputStructure, "");
        Message = "Got data\n@" + DateTime.Now.ToString("hh:mm:ss");
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
      ExpireNode();
    }
    public void ExpireNode()
    {
      OnNodeModified(true);
    }

    internal void InitReceiverEventsAndGlobals()
    {
      ObjectCache = new Dictionary<string, SpeckleObject>();

      SpeckleObjects = new List<SpeckleObject>();

      ConvertedObjects = new List<object>();

      myReceiver.OnReady += (sender, e) =>
      {
        UpdateGlobal();
      };

      myReceiver.OnWsMessage += OnWsMessage;

      myReceiver.OnError += (sender, e) =>
      {
        throw new WarningException(e.EventName + ": " + e.EventData);
      };

    }

    internal void Stream_LostFocus(object sender, RoutedEventArgs e)
    {
      if (Stream == _oldStream)
        return;
      _oldStream = Stream;
      //TODO: check integrity of stream id? Maybe length comparison?
      Console.WriteLine("Changing streams...");

      if (myReceiver != null)
        myReceiver.Dispose(true);

      myReceiver = new SpeckleApiClient(RestApi, true);

      InitReceiverEventsAndGlobals();

      //TODO: get documentname and guid, not sure how... Maybe with an extension?
      myReceiver.IntializeReceiver(Stream, "none", "Dynamo", "none", AuthToken);


    }

    internal void PromptAccountSelection(object sender, System.EventArgs e)
    {
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
