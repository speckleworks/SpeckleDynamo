using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using SpeckleCore;

namespace SpecklePopup
{
  /// <summary>
  /// Interaction logic for AccountsUserControl.xaml
  /// </summary>
  public partial class AccountsUserControl : UserControl
  {
    private string _defaultServer = "https://hestia.speckle.works/api/v1";
    private List<string> existingServers = new List<string>();
    private List<string> existingServers_fullDetails = new List<string>();
    internal ObservableCollection<Account> accounts = new ObservableCollection<Account>();
    private bool validationCheckPass = false;
    private Uri ServerAddress;
    private string email;
    private string password;
    private string serverName;
    public string restApi;
    public string apitoken;

    public AccountsUserControl()
    {
      InitializeComponent();

      RegisterServerUrl.Text = _defaultServer;
      LoginServerUrl.Text = _defaultServer;

      //only show in popupmode
      ButonUseSelected.Visibility = Visibility.Collapsed;

      LocalContext.Init();
      LoadAccounts();
    }

    private void LoadAccounts()
    {
      accounts = new ObservableCollection<Account>(LocalContext.GetAllAccounts());
      AccountListBox.ItemsSource = accounts;

      if (accounts.Any(x => x.IsDefault))
      {
        int index = accounts.Select((v, i) => new { acc = v, index = i }).First(x => x.acc.IsDefault).index;
        AccountListBox.SelectedIndex = index;
      }  
    }


    private string ValidateRegister()
    {
      Debug.WriteLine("validating...");
      string validationErrors = "";

      Uri uriResult;
      bool IsUrl = Uri.TryCreate(RegisterServerUrl.Text, UriKind.Absolute, out uriResult) &&
          (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

      if (!IsUrl)
      {
        validationErrors += "Invalid server url. \n";
      }

      ServerAddress = uriResult;

      MailAddress addr = null;
      try
      {
        addr = new System.Net.Mail.MailAddress(RegisterEmail.Text);
      }
      catch
      {
        validationErrors += "Invalid email address. \n";
      }

      string password = RegisterPassword.Password;

      if (password.Length <= 8)
      {
        validationErrors += "Password too short (<8). \n";
      }

      if (password != RegisterPasswordConfirm.Password)
      {
        validationErrors += "Passwords do not match. \n";
      }

      return validationErrors;
    }

    private string ValidateLogin()
    {
      Debug.WriteLine("validating...");
      string validationErrors = "";

      Uri uriResult;
      bool IsUrl = Uri.TryCreate(LoginServerUrl.Text, UriKind.Absolute, out uriResult) &&
          (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

      if (!IsUrl)
      {
        validationErrors += "Invalid server url. \n";
      }

      ServerAddress = uriResult;

      MailAddress addr = null;
      try
      {
        addr = new System.Net.Mail.MailAddress(LoginEmail.Text);
      }
      catch
      {
        validationErrors += "Invalid email address. \n";
      }

      return validationErrors;
    }

    private void saveAccountToDisk(string _email, string _apitoken, string _serverName, string _restApi, string _rootUrl, bool _isDefault)
    {

      string strPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);

      System.IO.Directory.CreateDirectory(strPath + @"\SpeckleSettings");

      strPath = strPath + @"\SpeckleSettings\";

      string fileName = _email + "." + _apitoken.Substring(0, 4) + ".txt";

      string content = _email + "," + _apitoken + "," + _serverName + "," + _restApi + "," + _rootUrl + "," + _isDefault;

      Debug.WriteLine(content);

      System.IO.StreamWriter file = new System.IO.StreamWriter(strPath + fileName);
      file.WriteLine(content);
      file.Close();
    }



    private void AccountListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      restApi = accounts[AccountListBox.SelectedIndex].RestApi;
      apitoken = accounts[AccountListBox.SelectedIndex].Token;
      //this.Close();
    }



    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
      RegisterButton.IsEnabled = false;
      RegisterButton.Content = "Contacting server...";
      var errs = ValidateRegister();
      if (errs != "")
      {
        MessageBox.Show(errs);
        RegisterButton.IsEnabled = true;
        RegisterButton.Content = "Register";
        return;
      }

      var names = RegisterName.Text.Split(' ');
      var myUser = new User()
      {
        Email = RegisterEmail.Text,
        Password = RegisterPassword.Password,
        Name = names?[0],
        Surname = names.Length >= 2 ? names?[1] : null,
        Company = RegisterCompany.Text,
      };

      string rawPingReply = "";
      dynamic parsedReply = null;
      using (var client = new WebClient())
      {
        try
        {
          rawPingReply = client.DownloadString(ServerAddress.ToString());
          parsedReply = JsonConvert.DeserializeObject(rawPingReply);
        }
        catch { MessageBox.Show("Failed to contact " + ServerAddress.ToString()); RegisterButton.IsEnabled = true; RegisterButton.Content = "Register"; return; }
      }

      var spkClient = new SpeckleApiClient() { BaseUrl = ServerAddress.ToString() };
      try
      {
        var response = spkClient.UserRegisterAsync(myUser).Result;
        if (response.Success == false)
        {
          MessageBox.Show("Failed to register user. " + response.Message); RegisterButton.IsEnabled = true; RegisterButton.Content = "Register"; return;
        }

        var serverName = parsedReply.serverName;
        var isDefault = accounts.Any() ? false : true;
        var newaccount = new Account { Email = myUser.Email, RestApi = ServerAddress.ToString(), ServerName = (string)serverName, Token = response.Resource.Apitoken, IsDefault = isDefault };
        LocalContext.AddAccount(newaccount);

        MessageBox.Show("Account creation ok: You're good to go.");
        restApi = RegisterServerUrl.Text;
        apitoken = response.Resource.Apitoken;
        RegisterButton.IsEnabled = true;
        RegisterButton.Content = "Register";

        AccoutsTabControl.SelectedIndex = 0;
        LoadAccounts();
        int index = accounts.Select((v, i) => new { acc = v, index = i }).First(x => x.acc.RestApi == RegisterServerUrl.Text && x.acc.Email == RegisterEmail.Text).index;
        AccountListBox.SelectedIndex = index;
        RegisterServerUrl.Text = RegisterEmail.Text = RegisterName.Text = RegisterCompany.Text = RegisterPassword.Password = RegisterPasswordConfirm.Password = "";

      }
      catch (Exception err)
      {
        MessageBox.Show("Failed to register user. " + err.InnerException.ToString()); RegisterButton.IsEnabled = true; RegisterButton.Content = "Register"; return;
      }
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
      var errs = ValidateLogin();
      if (errs != "")
      {
        MessageBox.Show(errs);
        return;
      }

      var myUser = new User()
      {
        Email = LoginEmail.Text,
        Password = LoginPassword.Password,
      };

      var spkClient = new SpeckleApiClient() { BaseUrl = ServerAddress.ToString() };

      string rawPingReply = "";
      dynamic parsedReply = null;
      using (var client = new WebClient())
      {
        try
        {
          rawPingReply = client.DownloadString(ServerAddress.ToString());
          parsedReply = JsonConvert.DeserializeObject(rawPingReply);
        }
        catch { MessageBox.Show("Failed to contact " + ServerAddress.ToString()); RegisterButton.IsEnabled = true; RegisterButton.Content = "Register"; return; }
      }

      var existing = accounts.FirstOrDefault(account => account.Email == myUser.Email && account.RestApi == ServerAddress.ToString());
      if (existing != null)
      {
        MessageBox.Show("You already have an account on " + ServerAddress.ToString() + " with " + myUser.Email + ".");
        return;
      }


      try
      {
        var response = spkClient.UserLoginAsync(myUser).Result;
        if (response.Success == false)
        {
          MessageBox.Show("Failed to login. " + response.Message); return;
        }

        var serverName = parsedReply.serverName;
        var isDefault = accounts.Any() ? false : true;
        var newaccount = new Account { Email = myUser.Email, RestApi = ServerAddress.ToString(), ServerName = (string)serverName, Token = response.Resource.Apitoken, IsDefault = isDefault };
        LocalContext.AddAccount(newaccount);

        MessageBox.Show("Account login ok: You're good to go.");
        restApi = RegisterServerUrl.Text;
        apitoken = response.Resource.Apitoken;

        AccoutsTabControl.SelectedIndex = 0;
        LoadAccounts();
        int index = accounts.Select((v, i) => new { acc = v, index = i }).First(x => x.acc.RestApi == LoginServerUrl.Text && x.acc.Email == LoginEmail.Text).index;
        AccountListBox.SelectedIndex = index;
        LoginServerUrl.Text = LoginEmail.Text = LoginPassword.Password = "";
      }
      catch (Exception err)
      {
        MessageBox.Show("Failed to login user. " + err.InnerException.ToString()); return;
      }

    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      var rb = sender as RadioButton;
      LocalContext.SetDefaultAccount(rb.DataContext as Account);
    }
  }


  public class SpeckleAccount
  {
    public string email { get; set; }
    public string apiToken { get; set; }
    public string serverName { get; set; }
    public string restApi { get; set; }
    public string rootUrl { get; set; }
    public bool isDefault { get; set; }
  }
}
