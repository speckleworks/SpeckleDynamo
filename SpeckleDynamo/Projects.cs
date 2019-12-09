
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
using System.Linq;

namespace SpeckleDynamo
{
  //based on Matteo Cominetti's Streams node

  //Node properties
  [NodeName("Speckle Projects")]
  [NodeDescription("Lists projects owned or accessed by an account.")]
  [NodeCategory("Speckle.I/O")]

  //Node outputs
  [OutPortNames("ID")]
  [OutPortTypes("string")]
  [OutPortDescriptions("Project ID")]

  [IsDesignScriptCompatible]

  public class Projects : NodeModel, INotifyPropertyChanged
  {
    private string authToken;
    private string restApi;
    private string email;
    private string server;
    private string projectId;
    private bool transmitting = true;
    private ObservableCollection<Project> userProjects = new ObservableCollection<Project>();

    internal string AuthToken { get => authToken; set { authToken = value; NotifyPropertyChanged("AuthToken"); } }

    public string RestApi { get => restApi; set { restApi = value; NotifyPropertyChanged("RestApi"); } }
    public string Email { get => email; set { email = value; NotifyPropertyChanged("Email"); } }
    public string Server { get => server; set { server = value; NotifyPropertyChanged("Server"); } }

    public string ProjectId
    {
      get => projectId;
      set
      {
        projectId = value;
        NotifyPropertyChanged("ProjectId");
        ExpireNode();
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

    [DNJ.JsonIgnore]
    public ObservableCollection<Project> UserProjects
    {
      get => userProjects;
      set
      {
        userProjects = value;
        NotifyPropertyChanged("UserProjects");
      }
    }

    [DNJ.JsonIgnore]
    [DNJ.JsonConverter(typeof(SpeckleClientConverter))]
    SpeckleApiClient Client;

    [DNJ.JsonConstructor]
    private Projects(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
    }

    public Projects()
    {
      RegisterAllPorts();
    }


    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      this.ClearErrorsAndWarnings();
      if (string.IsNullOrEmpty(ProjectId))
        return Enumerable.Empty<AssociativeNode>();

      return new AssociativeNode[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(ProjectId)) };
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
        GetProjects();
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

        GetProjects();
      }
      else
      {
        Error("Account selection failed.");
        Transmitting = false;
      }


    }

    private void GetProjects()
    {
      //caching projects
      if (DateTime.Now.Subtract(Globals.LastCheckedProjects).Seconds < 10)
      {
        UserProjects.Clear();
        if (Globals.UserProjects.Count > 0)
        {
          UserProjects.AddRange(Globals.UserProjects);
        }
        Transmitting = false;
        return;
      }

      Client.BaseUrl = RestApi;
      Client.AuthToken = AuthToken;
      Client.ProjectGetAllAsync().ContinueWith(tsk =>
      {
        DispatchOnUIThread(() =>
        {
          UserProjects.Clear();
          UserProjects.AddRange(tsk.Result.Resources.ToList());
          Transmitting = false;
          Globals.LastCheckedProjects = DateTime.Now;
          Globals.UserProjects = UserProjects;
        });
      });
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
