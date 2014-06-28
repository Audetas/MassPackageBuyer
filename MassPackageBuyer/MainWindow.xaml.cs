using System;
using System.Collections.Generic;
using System.Linq;
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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro;
using System.IO;
using Microsoft.Win32;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Windows.Threading;

namespace MassPackageBuyer
{
    public partial class MainWindow : MetroWindow
    {
        List<string> accounts;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(tbxPath.Text))
            {
                this.ShowMessageAsync("File not found.", "The location entered for your accounts.js file was not found.", MessageDialogStyle.Affirmative);
                return;
            }
            else if (!lstPackages.Items.Contains(tbxPackageID.Text))
            {
                this.ShowMessageAsync("Invalid package.", "The chosen package is not valid, please try selecting it again");
                return;
            }

            btnStart.IsEnabled = false;

            accounts = new List<string>();
            StreamReader jsFile = new StreamReader(tbxPath.Text);

            foreach (string s in jsFile.ReadToEnd().Split('\n'))
            {
                if (s.StartsWith("'") && (s.EndsWith(",") || s.EndsWith("\r")))
                {
                    try
                    {
                        accounts.Add(s.Split('\'')[1] + "," + s.Split('\'')[3]);
                    }
                    catch
                    {
                        lstLog.Items.Add("Error parsing: " + s);
                    }
                }
            }

            jsFile.Close();

            barProgress.Maximum = accounts.Count;
            string package = tbxPackageID.Text.Split('[')[1].Split(']')[0];

            new Thread(() =>
                {
                    WebClient client = new WebClient();
                    foreach (string s in accounts)
                    {
                        try
                        {
                            string response = client.DownloadString(
                                "https://realmofthemadgod.appspot.com/account/purchasePackage?packageId="
                                + package + "&guid=" + s.Split(',')[0] + "&password=" + s.Split(',')[1]);

                            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                                new Action(() =>
                                {
                                    lstLog.Items.Add(s.Split(',')[0] + ": " + response);
                                    barProgress.Value++;

                                    if (barProgress.Value == accounts.Count)
                                        btnStart.IsEnabled = true;
                                }));
                        }
                        catch
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                                new Action(() =>
                                {
                                    lstLog.Items.Add("Operation failed for: " + s.Split(',')[0]);
                                }));
                        }
                    }
                }).Start();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fOpen = new OpenFileDialog();
            fOpen.Filter = "Accounts File|*.js";
            Nullable<bool> completed = fOpen.ShowDialog();

            if (completed == true) //#gay nullables
                tbxPath.Text = fOpen.FileName;
        }

        private void btnChoose_Click(object sender, RoutedEventArgs e)
        {
            flyPackages.IsOpen = true;

            lstPackages.Items.Clear();
            WebResponse response = WebRequest.Create(@"https://realmofthemadgod.appspot.com/package/getPackages").GetResponse();

            XmlDataDocument xmlDataDocument = new XmlDataDocument();
            xmlDataDocument.Load(response.GetResponseStream());
            XmlNodeList elementsByTagName = xmlDataDocument.GetElementsByTagName("Package");
            for (int index = 0; index < elementsByTagName.Count; ++index)
            {
                string _name = elementsByTagName[index].ChildNodes.Item(0).InnerText.Trim();
                string _id = elementsByTagName[index].Attributes["id"].Value;
                string _price = elementsByTagName[index].ChildNodes.Item(1).InnerText.Trim();
                string _url = elementsByTagName[index].ChildNodes.Item(5).InnerText.Trim();
                string _end = elementsByTagName[index].ChildNodes.Item(6).InnerText.Trim();

                lstPackages.Items.Add("[" + _id + "] " + _name + " - " + _price + "gold");
            }
        }

        private void lstPackages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                tbxPackageID.Text = (string)e.AddedItems[0];
                flyPackages.IsOpen = false;
            }
        }
    }
}
