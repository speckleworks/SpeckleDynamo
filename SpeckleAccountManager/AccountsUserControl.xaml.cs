using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using SpeckleCore;

namespace SpecklePopup
{
  /// <summary>
  /// Interaction logic for AccountsUserControl.xaml
  /// </summary>
  public partial class AccountsUserControl : UserControl
  {
    private string _defaultServer = "https://hestia.speckle.works/api/v1";
    List<string> existingServers = new List<string>();
    List<string> existingServers_fullDetails = new List<string>();
    internal ObservableCollection<SpeckleAccount> accounts = new ObservableCollection<SpeckleAccount>();

    bool validationCheckPass = false;

    Uri ServerAddress;
    string email;
    string password;

    string serverName;
    public string restApi;
    public string apitoken;

    public AccountsUserControl()
    {
      InitializeComponent();

      RegisterServerUrl.Text = _defaultServer;
      LoginServerUrl.Text = _defaultServer;

      //only show in popupmode
      ButonUseSelected.Visibility = Visibility.Collapsed;
      AccountListBox.ItemsSource = accounts;
      LoadAccounts();
    }
    private void LoadAccounts()
    {
      accounts.Clear();
      string strPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\SpeckleSettings";
      if (Directory.Exists(strPath) && Directory.EnumerateFiles(strPath, "*.txt").Count() > 0)
        foreach (string file in Directory.EnumerateFiles(strPath, "*.txt"))
        {
          string content = File.ReadAllText(file);
          string[] pieces = content.TrimEnd('\r', '\n').Split(',');

          try
          {
            if (pieces.Length == 5)
              accounts.Add(new SpeckleAccount() { email = pieces[0], apiToken = pieces[1], serverName = pieces[2], restApi = pieces[3], rootUrl = pieces[4], isDefault = false });
            else if (pieces.Length == 6)
              accounts.Add(new SpeckleAccount() { email = pieces[0], apiToken = pieces[1], serverName = pieces[2], restApi = pieces[3], rootUrl = pieces[4], isDefault = bool.Parse(pieces[5]) });
          }
          catch (Exception e)
          {
            MessageBox.Show(e.Message, "Something went wrong! (╯°□°）╯︵ ┻━┻");
            return;
          }
        }
      if (accounts.Any(x => x.isDefault))
      {
        int index = accounts.Select((v, i) => new { acc = v, index = i }).First(x => x.acc.isDefault).index;
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
        validationErrors += "Invalid server url. \n";

      ServerAddress = uriResult;

      MailAddress addr = null;
      try
      {
        addr = new System.Net.Mail.MailAddress(this.RegisterEmail.Text);
      }
      catch
      {
        validationErrors += "Invalid email address. \n";
      }

      string password = this.RegisterPassword.Password;

      if (password.Length <= 8)
        validationErrors += "Password too short (<8). \n";

      if (password != this.RegisterPasswordConfirm.Password)
        validationErrors += "Passwords do not match. \n";

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
        validationErrors += "Invalid server url. \n";

      ServerAddress = uriResult;

      MailAddress addr = null;
      try
      {
        addr = new System.Net.Mail.MailAddress(this.LoginEmail.Text);
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
      this.restApi = this.accounts[this.AccountListBox.SelectedIndex].restApi;
      this.apitoken = this.accounts[this.AccountListBox.SelectedIndex].apiToken;
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

      var names = this.RegisterName.Text.Split(' ');
      var myUser = new User()
      {
        Email = this.RegisterEmail.Text,
        Password = this.RegisterPassword.Password,
        Name = names?[0],
        Surname = names.Length >= 2 ? names?[1] : null,
        Company = this.RegisterCompany.Text,
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
        saveAccountToDisk(this.RegisterEmail.Text, response.Resource.Apitoken, (string)serverName, this.RegisterServerUrl.Text, this.RegisterServerUrl.Text, isDefault);

        MessageBox.Show("Account creation ok: You're good to go.");
        this.restApi = this.RegisterServerUrl.Text;
        this.apitoken = response.Resource.Apitoken;
        RegisterButton.IsEnabled = true;
        RegisterButton.Content = "Register";

        AccoutsTabControl.SelectedIndex = 0;
        LoadAccounts();
        int index = accounts.Select((v, i) => new { acc = v, index = i }).First(x => x.acc.restApi == RegisterServerUrl.Text && x.acc.email == RegisterEmail.Text).index;
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
        Email = this.LoginEmail.Text,
        Password = this.LoginPassword.Password,
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

      var existing = accounts.FirstOrDefault(account => account.email == myUser.Email && account.restApi == ServerAddress.ToString());
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
        saveAccountToDisk(myUser.Email, response.Resource.Apitoken, (string)serverName, this.ServerAddress.ToString(), this.ServerAddress.ToString(), isDefault);

        MessageBox.Show("Account login ok: You're good to go.");
        this.restApi = this.RegisterServerUrl.Text;
        this.apitoken = response.Resource.Apitoken;

        AccoutsTabControl.SelectedIndex = 0;
        LoadAccounts();
        int index = accounts.Select((v, i) => new { acc = v, index = i }).First(x => x.acc.restApi == LoginServerUrl.Text && x.acc.email == LoginEmail.Text).index;
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
      foreach (var account in accounts)
      {
        //overwrite existing settings with new IsDefaut value
        saveAccountToDisk(account.email, account.apiToken, account.serverName, account.restApi, account.rootUrl, account.isDefault);
      }
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
