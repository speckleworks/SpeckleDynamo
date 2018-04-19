using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace SpecklePopup
{
  public partial class MainWindow : Window
  {

    List<string> existingServers = new List<string>();
    List<string> existingServers_fullDetails = new List<string>();
    List<SpeckleAccount> accounts = new List<SpeckleAccount>();

    bool validationCheckPass = false;

    Uri server;
    string email;
    string password;

    string serverName;
    public string restApi;
    public string apitoken;
    public string selectedEmail;
    public string selectedServer;



    public MainWindow( )
    {
      InitializeComponent();

      string strPath = System.Environment.GetFolderPath( System.Environment.SpecialFolder.LocalApplicationData );
      strPath = strPath + @"\SpeckleSettings";

      if ( Directory.Exists( strPath ) && Directory.EnumerateFiles( strPath, "*.txt" ).Count() > 0 )
        foreach ( string file in Directory.EnumerateFiles( strPath, "*.txt" ) )
        {
          string content = File.ReadAllText( file );
          string[ ] pieces = content.TrimEnd( '\r', '\n' ).Split( ',' );

          accounts.Add( new SpeckleAccount() { email = pieces[ 0 ], apiToken = pieces[ 1 ], serverName = pieces[ 2 ], restApi = pieces[ 3 ], rootUrl = pieces[ 4 ] } );
        }

      var gridView = new GridView();
      this.existingAccounts.View = gridView;
      gridView.Columns.Add( new GridViewColumn
      {
        Header = "email",
        DisplayMemberBinding = new Binding( "email" )
      } );
      gridView.Columns.Add( new GridViewColumn
      {
        Header = "server",
        DisplayMemberBinding = new Binding( "serverName" )
      } );

      //gridView.Columns.Add(new GridViewColumn
      //{
      //    Header = "API",
      //    DisplayMemberBinding = new Binding("restApi")
      //});

      if ( accounts.Count == 0 )
      {
        existingAccounts.Items.Add( new SpeckleAccount() { email = "No existing accounts found." } );
        Dispatcher.BeginInvoke( ( Action ) ( ( ) => tabControl.SelectedIndex = 1 ) );
      }
      else
      {
        Dispatcher.BeginInvoke( ( Action ) ( ( ) => tabControl.SelectedIndex = 0 ) );
      }

      foreach ( var account in accounts )
      {
        existingAccounts.Items.Add( account );
      }

    }

    private void Rectangle_MouseDown( object sender, MouseButtonEventArgs e )
    {
      this.DragMove();
    }

    private void closeButton_Click( object sender, RoutedEventArgs e )
    {
      //DialogResult = false;
      this.Close();
    }

    private void validateInputs( object sender, RoutedEventArgs e )
    {
      Debug.WriteLine( "validating..." );
      string validationErrors = "";

      Uri uriResult;
      bool urlok = Uri.TryCreate( serverUrlBox.Text, UriKind.Absolute, out uriResult ) &&
          ( uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps );

      if ( !urlok )
        validationErrors += "Invalid server url. ";

      MailAddress addr = null;

      try
      {
        addr = new System.Net.Mail.MailAddress( this.emailBox.Text );
      }
      catch
      {
        validationErrors += "Invalid email. ";
      }

      string password = this.passwordBox.Password;

      if ( this.passwordBox.Password.Length <= 8 )
      {
        validationErrors += "Password too short (<8). ";
      }

      if ( this.passwordBox.Password != this.passwordBox_confirm.Password )
      {
        validationErrors += "Passwords do not match. ";
      }

      if ( validationErrors != "" )
      {
        this.errors.Foreground = new SolidColorBrush( Colors.Red );
        this.errors.Text = validationErrors;
        validationCheckPass = false;
      }
      else
      {
        this.errors.Foreground = new SolidColorBrush( Colors.Green );
        this.errors.Text = "Details seem ok. Go ahead!";
        validationCheckPass = true;
      }

      if ( uriResult != null )
      {
        this.server = uriResult;
      }


      if ( addr != null )
        this.email = addr.ToString();

      if ( passwordBox.Password != null )
        this.password = passwordBox.Password;
    }

    private void registerBtn_Click( object sender, RoutedEventArgs e )
    {
      this.validateInputs( this, null );
      if ( !validationCheckPass ) return;

      bool userCreated = false;

      using ( var client = new WebClient() )
      {
        try
        {
          string rawPingReply = client.DownloadString( server.ToString() );
          dynamic pingReply = JsonConvert.DeserializeObject( rawPingReply );

          this.serverName = pingReply.serverName;
          this.restApi = server.ToString();

          Dictionary<string, string> newUser = new Dictionary<string, string>();
          newUser.Add( "email", email );
          newUser.Add( "password", password );
          newUser.Add( "surname", this.SurnameBox.Text );
          newUser.Add( "name", this.NameBox.Text );

          client.Headers[ HttpRequestHeader.ContentType ] = "application/json";

          string rawUserReply = "";

          try
          {
            rawUserReply = client.UploadString( this.restApi + "/accounts/register", "POST", JsonConvert.SerializeObject( newUser, Formatting.None ) );
          }
          catch ( WebException err_user )
          {
            // failed to create a new user. for some reason or the other.
            MessageBox.Show( "Failed to register user. Have you registered before?" );
            userCreated = false;
            return;
          }

          dynamic userReply = JsonConvert.DeserializeObject( rawUserReply ); //jss.Deserialize<Dictionary<string, string>>(rawUserReply);

          if ( userReply.success == "True" )
          {

            this.apitoken = userReply.apitoken;
            userCreated = true;
            saveAccountToDisk( this.email, this.apitoken, this.serverName, this.restApi, this.server.ToString() );

            MessageBox.Show( "Congrats! You've made an account with " + this.serverName + ". " + "From now you will have access to this account from the existing servers tab." );

            this.Close();
          }
          else
          {
            MessageBox.Show( userReply.message );
            userCreated = false;
            return;
          }

        }
        catch ( WebException err_ping )
        {
          // failed to ping server.
          if ( err_ping.Response != null )
            MessageBox.Show( "Failed to contact server. Did you provide in the correct url? " + err_ping.Response.ToString() );
          else
            MessageBox.Show( "Failed to contact server. Did you provide in the correct url? " + err_ping.ToString() );
          userCreated = false;
          return;
        }
      }
    }

    private void saveAccountToDisk( string _email, string _apitoken, string _serverName, string _restApi, string _rootUrl )
    {

      string strPath = System.Environment.GetFolderPath( System.Environment.SpecialFolder.LocalApplicationData );

      System.IO.Directory.CreateDirectory( strPath + @"\SpeckleSettings" );

      strPath = strPath + @"\SpeckleSettings\";

      string fileName = _email + "." + _apitoken.Substring( 0, 4 ) + ".txt";

      string content = _email + "," + _apitoken + "," + _serverName + "," + _restApi + "," + _rootUrl;

      Debug.WriteLine( content );

      System.IO.StreamWriter file = new System.IO.StreamWriter( strPath + fileName );
      file.WriteLine( content );
      file.Close();
    }

    private void ListView_MouseDoubleClick( object sender, MouseButtonEventArgs e )
    {
      DependencyObject obj = ( DependencyObject ) e.OriginalSource;

      while ( obj != null && obj != existingAccounts )
      {
        if ( obj.GetType() == typeof( ListViewItem ) )
        {
          var selectedAccount = this.accounts[ this.existingAccounts.SelectedIndex ];
          this.restApi = selectedAccount.restApi;
          this.apitoken = selectedAccount.apiToken;
          this.Close();

          break;
        }
        obj = VisualTreeHelper.GetParent( obj );
      }
    }

    private void UseSelected( object sender, RoutedEventArgs e )
    {
      if ( this.existingAccounts.SelectedIndex == -1 )
      {
        MessageBox.Show( "Please select an account first." );
        return;
      }
      var selectedAccount = this.accounts[ this.existingAccounts.SelectedIndex ];
      selectedEmail = selectedAccount.email;
      selectedServer = selectedAccount.serverName;
      this.restApi = selectedAccount.restApi;
      this.apitoken = selectedAccount.apiToken;
      this.Close();
    }
  }

  public class SpeckleAccount
  {
    public string email { get; set; }
    public string apiToken { get; set; }
    public string serverName { get; set; }
    public string restApi { get; set; }
    public string rootUrl { get; set; }
  }
}
