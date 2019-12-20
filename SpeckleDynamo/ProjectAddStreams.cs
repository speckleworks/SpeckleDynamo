
extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using SpeckleCore;
using SpeckleDynamo.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Dynamo.ViewModels;
using System.Linq;
using DesignScript.Builtin;
using Dynamo.Graph.Connectors;
using System.Threading.Tasks;

namespace SpeckleDynamo
{
  //based on Matteo Cominetti's Streams node

  //Node properties
  [NodeName("Add Streams to Project")]
  [NodeDescription("Add a list of streams to a Speckle project.")]
  [NodeCategory("Speckle.Projects")]

  //Node inputs
  [InPortNames("ProjectID", "StreamIDs")]
  [InPortTypes("string", "List<string>")]
  [InPortDescriptions("The project to add streams to", "The streams to add")]

  //Node outputs
  [OutPortNames("Response")]
  [OutPortTypes("string")]
  [OutPortDescriptions("Server Response")]

  [IsDesignScriptCompatible]



  public class ProjectAddStream : NodeModel, INotifyPropertyChanged
  {

    private string authToken;
    private string restApi;
    private string email;
    private string server;
    private bool transmitting = true;

    [DNJ.JsonIgnore]
    [DNJ.JsonConverter(typeof(SpeckleClientConverter))]
    SpeckleApiClient Client;

    public static Dynamo.Controls.NodeView viewModel;

    internal string AuthToken { get => authToken; set { authToken = value; NotifyPropertyChanged("AuthToken"); } }

    public string RestApi { get => restApi; set { restApi = value; NotifyPropertyChanged("RestApi"); } }
    public string Email { get => email; set { email = value; NotifyPropertyChanged("Email"); } }
    public string Server { get => server; set { server = value; NotifyPropertyChanged("Server"); } }

    private string response = "";

    public string Response
    {
      get { return response; }
      set {
        response = value;
        NotifyPropertyChanged("Response");
      }
    }

    public bool Transmitting
    {
      get => transmitting;
      set
      {
        transmitting = value;
        NotifyPropertyChanged("Transmitting");
      }
    }

    [DNJ.JsonConstructor]
    private ProjectAddStream(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
    }

    public ProjectAddStream()
    {
      RegisterAllPorts();
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      ClearErrorsAndWarnings();
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      }
      try
      {
        var projectIds = GetInputValues(0);
        var streamIds = GetInputValues(1);
        if (projectIds != null && streamIds != null)
        {
          //this blocks things, but I couldn't find a better way to update the output after making the query...
          Response = Task.Run(async () => await AddStreamsAsync(projectIds[0].ToString(), streamIds.Select(streamId => streamId.ToString()).ToList())).Result;
        }
      }
      catch (Exception)
      {
        Warning("Please connect a valid ProjectId and StreamId", true);
      }
      return new AssociativeNode[] {AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(Response)) };
    }

    private List<object> GetInputValues(int inputNum)
    {
      var inputNamenode = InPorts[inputNum].Connectors[0].Start.Owner;
      var inputNameIndex = InPorts[inputNum].Connectors[0].Start.Index;
      var inputNameId = inputNamenode.GetAstIdentifierForOutputIndex(inputNameIndex).Name;
      var inputNameMirror = viewModel.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(inputNameId);

      var input = new List<object>();
      if (inputNameMirror == null || inputNameMirror.GetData() == null) return input;
      var data = inputNameMirror.GetData();
      if (data != null)
      {
        if (data.IsCollection)
        {
          input.AddRange(data.GetElements().Select(e => e.Data).OfType<object>());
        }
        else
        {
          var inData = data.Data as Object;
          if (inData != null)
            input.Add(inData);
        }
      }
      return input;
    }

    public void ExpireNode()
    {
      OnNodeModified(forceExecute: true);
    }

    internal void AddedToDocument(object sender, System.EventArgs e)
    {
      //saved receiver
      if (Client != null)
      {
        AuthToken = Client.AuthToken;
        return;
      }

      Transmitting = true;

      //account/form flow
      Account account = null;
      LocalContext.Init();
      try
      {
        //try getting default account, exception is thrownif none is set
        account = LocalContext.GetDefaultAccount();
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(ex.ToString());
      }

      //show account selection window
      if (account == null)
      {
        DispatchOnUIThread(() =>
        {
          //open window with isPopUp=true
          var signInWindow = new SpecklePopup.SignInWindow(true);
          signInWindow.ShowDialog();

          if (signInWindow.AccountListBox.SelectedIndex != -1)
          {
            account = signInWindow.accounts[signInWindow.AccountListBox.SelectedIndex];
          }
        });
      }

      if (account != null)
      {
        Email = account.Email;
        Server = account.ServerName;

        RestApi = account.RestApi;
        AuthToken = account.Token;

        Client = new SpeckleApiClient();
        Transmitting = false;

      }
      else
      {
        Error("Account selection failed.");
        Transmitting = false;
      }
    }

    public async Task<string> AddStreamsAsync(string projectId, List<string> streamIds)
    {
      Client.BaseUrl = RestApi;
      Client.AuthToken = AuthToken;
      Transmitting = true;

      try
      {
        var responseProject = await Client.ProjectGetAsync(projectId);
        responseProject.Resource.Streams.AddRange(streamIds);

        var project = await Client.ProjectUpdateAsync(projectId, responseProject.Resource);

        Transmitting = false;
        return project.Message.ToString();
      }
      catch (Exception e)
      {
        Warning("There was an error adding the streams to the project", true);
        Transmitting = false;
        return e.ToString();
      }
    }

    public override void Dispose()
    {
      if (Client != null)
        Client.Dispose();
      base.Dispose();
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
