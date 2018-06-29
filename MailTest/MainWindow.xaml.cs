using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.Win32;

namespace MailTest
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            MxList.Items.SortDescriptions.Add(new SortDescription("Priority", ListSortDirection.Ascending));
            MxList.Items.SortDescriptions.Add(new SortDescription("Server", ListSortDirection.Ascending));

            SmtpCodes.ItemsSource = SMTPCode.GetErrorList();

            Options_Load();
        }

        private void About_Show(object sender, RoutedEventArgs e)
        {
            var about = new About();
            about.Show();
        }

        private void Codes_ClearSearchResults(object sender, RoutedEventArgs e)
        {
            var view = (CollectionView) CollectionViewSource.GetDefaultView(SmtpCodes.ItemsSource);
            ErrorCodeSearchResults.Inlines.Clear();
            view.Filter = null;

            // Clear Selection
            SmtpCodes.SelectedItem = null;
            ButtonClearResults.Visibility = Visibility.Hidden;
            CodeLabel.Visibility = Visibility.Hidden;

            ErrorCodeDescription.Text = string.Empty;
            ErrorCodeMessage.Text = string.Empty;
        }

        private void Codes_Search(object sender, RoutedEventArgs e)
        {
            var log = LogSearchCodesResults.IsChecked == true;

            ErrorCodeSearchResults.Inlines.Clear();
            if (log)
            {
                ErrorCodeSearchResults.Inlines.Add(@"SEARCH RESULTS (also on results):");
                Log_Line($"*** SEARCHING SMTP ERRORS FOR: {SearchCriteria.Text}");
            }
            else
            {
                ErrorCodeSearchResults.Inlines.Add(@"SEARCH RESULTS:");
            }

            var somethingFound = false;
            var view = (CollectionView) CollectionViewSource.GetDefaultView(SmtpCodes.ItemsSource);
            view.Filter = item =>
            {
                var find = SearchCriteria.Text;
                var error = (SMTPCode) item;
                var found = error.Code.IndexOf(find, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                            error.Message.IndexOf(find, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                            error.Description.IndexOf(find, StringComparison.InvariantCultureIgnoreCase) != -1;

                if (!found)
                {
                    return false;
                }

                somethingFound = true;

                var link = new Hyperlink {NavigateUri = new Uri($"code://{error.Code}")};
                link.Inlines.Add(error.Code);
                link.RequestNavigate += Codes_ShowCode;

                ErrorCodeSearchResults.Inlines.Add("   ");
                ErrorCodeSearchResults.Inlines.Add(link);

                if (log)
                {
                    Log_Line($"## SMTP ERROR CODE: {error.Code}");
                    Log_Line($"# DESCRIPTION: {error.Description}");
                    Log_Line($"# MESSAGE: {error.Message}");
                }

                return true;
            };

            if (!somethingFound)
            {
                if (log) Log_Line("NO SEARCH RESULTS FOUND");
                ErrorCodeSearchResults.Inlines.Add("NOTHING FOUND!");
            }

            if (log) Log_Line("*** END OF SEARCH RESULTS");
        }

        private void Codes_ShowCode(object sender, SelectionChangedEventArgs e)
        {
            var error = (SMTPCode) SmtpCodes.SelectedItem;

            if (!string.IsNullOrEmpty(error?.Code))
            {
                ErrorCodeDescription.Text = error.Description ?? string.Empty;
                ErrorCodeMessage.Text = error.Message ?? string.Empty;
                ButtonClearResults.Visibility = Visibility.Visible;
                CodeLabel.Text = error.Code;
                CodeLabel.Visibility = Visibility.Visible;
            }
            else
            {
                ButtonClearResults.Visibility = Visibility.Hidden;
                CodeLabel.Visibility = Visibility.Hidden;
            }
        }

        private void Codes_ShowCode(object sender, RequestNavigateEventArgs e)
        {
            var code = e.Uri.ToString().Substring(7).TrimEnd('/');

            for (var index = 0; index < SmtpCodes.Items.Count; index++)
            {
                var item = SmtpCodes.Items[index];
                var error = (SMTPCode) item;
                if (error.Code.Equals(code, StringComparison.InvariantCultureIgnoreCase))
                {
                    SmtpCodes.SelectedIndex = index;
                    return;
                }
            }
        }

        private void Log(string msg) => LogBox.AppendText(msg);

        private void Log_Clear(object sender, RoutedEventArgs e)
        {
            LogBox.Document.Blocks.Clear();
        }

        private void Log_CopyAll(object sender, RoutedEventArgs e)
        {
            var selection = new TextRange(LogBox.Document.ContentStart, LogBox.Document.ContentEnd);
            var text = selection.Text;
            Clipboard.SetDataObject(text, true);
        }

        private void Log_CopySelection(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(LogBox.Selection.Text, true);
        }

        private void Log_Line(string msg)
        {
            Log($"{msg}\r\n");
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) => Options_Save();

        private void MxList_AddHost(object sender, RoutedEventArgs e) =>
            MxList.Items.Add(new DnsRecord {Type = "MX", Priority = "0", Server = MxListNewHost.Text});

        private void MxList_Clear(object sender, RoutedEventArgs e) => MxList.Items.Clear();

        private void MxList_Copy(object sender, RoutedEventArgs e)
        {
            if (MxList.SelectedItems.Count == 0) MxList.SelectAll();
            Clipboard.SetDataObject(string.Join(";",
                ((IEnumerable<DnsRecord>) MxList.SelectedItems).Select(r => r.Server)));
        }

        private void MxList_QueryDns(object sender, RoutedEventArgs e)
        {
            var recordType = "MX";
            try
            {
                var nslookup = new ProcessStartInfo("nslookup")
                {
                    Arguments = $"-type={recordType} {DnsHost.Text} {DnsServer.Text}",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                var output = Process.Start(nslookup)?.StandardOutput;
                var regex = new Regex("preference = (?<pref>[^\\\\\\s]+), mail exchanger = (?<server>[^\\\\\\s]+)");

                var log = LogNameServerLookup.IsChecked == true;
                if (log)
                    Log_Line($"### NSLOOKUP - {nslookup.FileName} {nslookup.Arguments}");

                MxList.Items.Clear();

                while (output != null && output.Peek() > -1)
                {
                    var input = output.ReadLine() ?? string.Empty;

                    if (log) Log_Line(input);

                    var match = regex.Match(input);

                    if (match.Success)
                    {
                        MxList.Items.Add(new DnsRecord
                        {
                            Type = recordType,
                            Priority = match.Groups["pref"].Value,
                            Server = match.Groups["server"].Value
                        });
                    }
                }

                if (log) Log_Line("### NSLOOKUP COMPLETE");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, @"Error:" + ex.Message, @"Error:", MessageBoxButton.OK,
                    MessageBoxImage.Hand);
            }
        }

        private void MxList_SelectAll(object sender, RoutedEventArgs e) => MxList.SelectAll();

        private void MxList_SmptTestAll(object sender, RoutedEventArgs e)
        {
            MxList.SelectAll();
            MxList_SmtpTest(sender, e);
        }

        private void MxList_SmtpTest(object sender, RoutedEventArgs e)
        {
            if (MxList.SelectedItems.Count == 0) MxList.SelectAll();

            SmtpServer.Text = string.Join(";",
                from object item in MxList.SelectedItems select ((DnsRecord) item).Server);

            Tab_SmtpTest();
        }

        private void Options_Load()
        {
            RegistryKey rk;

            if ((rk = Registry.CurrentUser.OpenSubKey("Software")?.OpenSubKey("MailTest", true)) == null)
                return;

            // SMTP Tab
            Options_Registry_ReadString(rk, SmtpServer, "SMTP Server");
            Options_Registry_ReadString(rk, SmtpPort, "SMTP Port");
            Options_Registry_ReadString(rk, SmtpFrom, "SMTP From");
            Options_Registry_ReadString(rk, SmtpTo, "SMTP To");
            Options_Registry_ReadString(rk, SmtpSubject, "SMTP Subject");
            Options_Registry_ReadString(rk, SmtpBody, "SMTP Body");
            Options_Registry_ReadCheckbox(rk, SmtpSaveAuth, "SMTP Save Authentiation");
            if (SmtpSaveAuth.IsChecked == true)
            {
                Options_Registry_ReadCheckbox(rk, SmtpUseSsl, "SMTP Use SSL");
                Options_Registry_ReadCheckbox(rk, SmtpAuthRequired, "SMTP Auth Required");
                SmtpUsername.Text = rk.GetValue("SMTP Username", SmtpUsername.Text).ToString();
                SmtpPassword.Text = rk.GetValue("SMTP Password", SmtpUsername.Text).ToString();
            }

            Options_Registry_ReadCheckbox(rk, SmtpUseSystemDiagnostics, "SMTP Use System Diagnostics");

            // POP Tab

            Options_Registry_ReadString(rk, PopServer, "POP Server");
            Options_Registry_ReadString(rk, PopPort, "POP Port");
            Options_Registry_ReadString(rk, PopUsername, "POP Username");
            Options_Registry_ReadString(rk, PopPassword, "POP Password");
            Options_Registry_ReadCheckbox(rk, PopUseSystemDiagnostics, "POP Use System Diagnostics");
            Options_Registry_ReadCheckbox(rk, PopUseSSL, "POP Use SSL");

            // MX Tab
            Options_Registry_ReadString(rk, DnsServer, "DNS Server");
            Options_Registry_ReadString(rk, DnsHost, "DNS Host Lookup");
            Options_Registry_ReadCheckbox(rk, LogNameServerLookup, "DNS Log");

            //SMTP CODES
            Options_Registry_ReadCheckbox(rk, LogSearchCodesResults, "CODES Log");
        }

        private void Options_Registry_ReadCheckbox(RegistryKey rk, CheckBox control, string vName)
        {
            if (rk == null)
                return;
            control.IsChecked = Convert.ToBoolean(rk.GetValue(vName, control.IsChecked == true).ToString());
        }

        private void Options_Registry_ReadString(RegistryKey rk, InputWithSave control, string vName)
        {
            var text = control.Text;

            if (rk == null)
                return;
            control.Text = rk.GetValue(vName, text).ToString();
            if (text.Equals(control.Text))
                return;
            control.Save = true;
        }

        private void Options_Registry_SaveBoolean(RegistryKey rk, bool save, string vName, bool bit)
        {
            if (save)
                rk.SetValue(vName, bit);
            else
                rk.DeleteValue(vName, false);
        }

        private void Options_Registry_SaveString(RegistryKey rk, bool save, string vName, string vString)
        {
            if (save)
                rk.SetValue(vName, vString);
            else
                rk.DeleteValue(vName, false);
        }

        private void Options_Registry_SaveString(RegistryKey rk, InputWithSave control, string vName) =>
            Options_Registry_SaveString(rk, control.Save, vName, control.Text);

        private void Options_Save()
        {
            RegistryKey subKey;

            if ((subKey = Registry.CurrentUser.OpenSubKey("Software", true)?.CreateSubKey("MailTest")) == null)
                return;

            // SMTP TAB
            Options_Registry_SaveString(subKey, SmtpServer, "SMTP Server");
            Options_Registry_SaveString(subKey, SmtpPort, "SMTP Port");
            Options_Registry_SaveString(subKey, SmtpFrom, "SMTP From");
            Options_Registry_SaveString(subKey, SmtpTo, "SMTP To");
            Options_Registry_SaveString(subKey, SmtpSubject, "SMTP Subject");
            Options_Registry_SaveString(subKey, SmtpBody, "SMTP Body");
            Options_Registry_SaveBoolean(subKey, SmtpSaveAuth.IsChecked == true, "SMTP Save Authentiation",
                SmtpSaveAuth.IsChecked == true);
            Options_Registry_SaveBoolean(subKey, SmtpSaveAuth.IsChecked == true, "SMTP Use SSL",
                SmtpUseSsl.IsChecked == true);
            Options_Registry_SaveBoolean(subKey, SmtpSaveAuth.IsChecked == true, "SMTP Auth Required",
                SmtpAuthRequired.IsChecked == true);
            Options_Registry_SaveString(subKey, SmtpSaveAuth.IsChecked == true, "SMTP Username", SmtpUsername.Text);
            Options_Registry_SaveString(subKey, SmtpSaveAuth.IsChecked == true, "SMTP Password", SmtpPassword.Text);
            subKey.SetValue("SMTP Use System Diagnostics", SmtpUseSystemDiagnostics.IsChecked == true);

            // POP TAB
            Options_Registry_SaveString(subKey, PopServer, "POP Server");
            Options_Registry_SaveString(subKey, PopPort, "POP Port");
            Options_Registry_SaveString(subKey, PopUsername, "POP Username");
            Options_Registry_SaveString(subKey, PopPassword, "POP Password");
            subKey.SetValue("POP Use System Diagnostics", PopUseSystemDiagnostics.IsChecked == true);
            Options_Registry_SaveBoolean(subKey, PopPort.Save, "POP Use SSL", PopUseSSL.IsChecked == true);

            // MX LOOKUP TAB
            Options_Registry_SaveString(subKey, DnsHost, "DNS Host Lookup");
            Options_Registry_SaveString(subKey, DnsServer, "DNS Server");
            subKey.SetValue("DNS Log", LogNameServerLookup.IsChecked == true);

            //SMTP CODES
            subKey.SetValue("CODES Log", LogSearchCodesResults.IsChecked == true);
        }

        private void PopUseSSL_Checked(object sender, RoutedEventArgs e)
        {
            if (PopUseSSL.IsChecked == true) PopUseSystemDiagnostics.IsChecked = true;
        }

        private void RunTest(ITest test)
        {
            try
            {
                test.Run(Log_Line);
            }
            catch (Exception ex)
            {
                LogBox.AppendText(ex.Message);
            }
        }

        private void RunTest_Pop(object sender, RoutedEventArgs e)
        {
            Tab_Log(sender, e);
            if (PopUseSystemDiagnostics.IsChecked == true || PopUseSSL.IsChecked == true)
            {
                foreach (var server in PopServer.Text.Split(';'))
                {
                    RunTest(new POP3ClientTest(Log, server, PopPort.Text, PopUsername.Text, PopPassword.Text,
                        PopUseSSL.IsChecked == true));
                }
            }
            else
            {
                foreach (var server in PopServer.Text.Split(';'))
                {
                    RunTest(new POP3SimpleTest(server, PopPort.Text, PopUsername.Text, PopPassword.Text));
                }
            }
        }

        private void RunTest_Smtp(object sender, RoutedEventArgs e)
        {
            Tab_Log(sender, e);
            if (SmtpAuthRequired.IsChecked == true || SmtpUseSystemDiagnostics.IsChecked == true)
            {
                foreach (var server in SmtpServer.Text.Split(';'))
                {
                    RunTest(new SMTPClientTest(Log, server, SmtpPort.Text,
                        SmtpFrom.Text, SmtpTo.Text, SmtpSubject.Text, SmtpBody.Text,
                        SmtpUseSsl.IsChecked == true, SmtpUsername.Text, SmtpPassword.Text,
                        SmtpAuthRequired.IsChecked == true));
                }
            }
            else
            {
                foreach (var server in SmtpServer.Text.Split(';'))
                {
                    RunTest(new SMTPSimpleTest(server, SmtpPort.Text,
                        SmtpFrom.Text, SmtpTo.Text, SmtpSubject.Text, SmtpBody.Text));
                }
            }
        }

        private void SmtpAuthRequired_Checked(object sender, RoutedEventArgs e)
        {
            if (SmtpUseSsl?.IsChecked == true && SmtpAuthRequired?.IsChecked == true 
                                              && SmtpUseSystemDiagnostics!=null)
                SmtpUseSystemDiagnostics.IsChecked = true;
        }

        private void SmtpUseSSL_Checked(object sender, RoutedEventArgs e)
        {
            if (SmtpUseSsl?.IsChecked == true && SmtpAuthRequired?.IsChecked == true
                                              && SmtpUseSystemDiagnostics != null)
                SmtpUseSystemDiagnostics.IsChecked = true;
        }

        private void Tab_Codes(object sender, RoutedEventArgs e) => Tabs.SelectedIndex = 3;

        private void Tab_Log(object sender, RoutedEventArgs e) => Tabs.SelectedIndex = 4;

        private void Tab_SmtpTest()
        {
            Tabs.SelectedIndex = 0;
        }
    }

    internal class DnsRecord
    {
        public string Priority { get; set; }
        public string Server { get; set; }
        public string Type { get; set; }
    }
}