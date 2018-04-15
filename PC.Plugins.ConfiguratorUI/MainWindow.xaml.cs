﻿/*!
* (c) 2016-2018 EntIT Software LLC, a Micro Focus company
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
//using log4net;
using PC.Plugins.Common.Rest;
using PC.Plugins.Automation;
using System.Diagnostics;


//using MicroFocus.PC.Api.Core.Connector;
//using MicroFocus.PC.CiPlugins.Tfs.Core.Configuration;
//using MicroFocus.PC.CiPlugins.Tfs.Core.Tools;

namespace PC.Plugins.ConfiguratorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Configurator _configurator;

        //ConnectionDetails _conDetails = new ConnectionDetails();
        private string _instanceId = Guid.NewGuid().ToString();
        private string _workDirectory = @"C:\Temp\PC.Plugins.Automation.Logs\{0}";
        private string _logFileName = "PC.Plugins.Automation.Logs.log";
        public MainWindow()
        {
            try
            {
                Helper.CheckedConnection = false;
                InitializeComponent();
            }
            catch //(Exception ex)
            {
               // Log.Warn("Could not parse existing configuration file", ex);
            }

        }

        private void TestConnectionButton_OnClick(object sender, RoutedEventArgs e)
        {

            ReadFields();
            try
            {
                if (String.IsNullOrEmpty(PCServerURL.Text) || String.IsNullOrEmpty(PCUserName.Text))
                {
                    MessageBox.Show("To test the connection, the PC URL and Username (with adequate password) need to be specified.", "PC", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }
                Helper.CheckedConnection = _configurator.TestConnection();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PC", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }
            if (Helper.CheckedConnection)
                MessageBox.Show("Connection successfull", "PC", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            else
                MessageBox.Show("Connection Failed", "PC", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }


        private void RunButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ReadFields();
                System.Threading.Thread thread = new System.Threading.Thread(_configurator.Perform);
                thread.Start();
                string reportfile = System.IO.Path.Combine(_workDirectory, _logFileName);

                DisplayReportInPSConsole(System.IO.Path.Combine(reportfile));

            }
            catch (Exception ex)
            {
                const string error = "Error while trying to run the test from the plugin. Error: {0}. \n {1}";
                //Log.Error(error, ex);
                MessageBox.Show(string.Format(error, ex.Message, ex), "PC", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void ReadFields()
        {
            string pcServerURL = PCServerURL.Text;
            string pcServerAndPort = PCServerURL.Text.Trim().Replace("https://", "").Replace("http://", "");
            string pcServername = (pcServerAndPort.LastIndexOf(':') == -1) ? pcServerAndPort : pcServerAndPort.Substring(0, (pcServerAndPort.LastIndexOf(':')));
            string webProtocol = PCServerURL.Text.Trim().StartsWith("https") ? "https" : "http";
            string pcUserName = PCUserName.Text;
            string pcPassword = PCPassword.Password;
            string domain = Domain.Text;
            string project = Project.Text;
            string testID = TestID.Text;
            bool autoTestInstance = AutoTestInstance.IsChecked == true;
            string testInstanceID = (autoTestInstance == false && TestInstanceID.Text != "Enter Test Instance ID") ? TestInstanceID.Text : "";
            string pcPostRunAction = PostRunAction.SelectionBoxItem.ToString();
            string proxyURL = ProxyURL.Text;
            string proxyUserName = ProxyUserName.Text;
            string proxyPassword = ProxyPassword.Password;
            string trending = (DoNotTrend.IsChecked == true) ? "DoNotTrend" : (AssociatedTrend.IsChecked == true) ? "AssociatedTrend" : "UseTrendReportID";
            string trendReportID = (trending == "UseTrendReportID" && TrendReportID.Text != "Enter Trend Report ID") ? TrendReportID.Text : "";
            string timeslotDurationHours = "";
            string timeslotDurationMinutes = String.IsNullOrWhiteSpace(TimeslotDurationMinutes.Text) ? "30" : TimeslotDurationMinutes.Text;
            bool useSLAStatus = UseSLAStatus.IsChecked == true;
            bool useVUDs = UseVUDs.IsChecked == true;
            string description = "";

            //_instanceId = string.IsNullOrEmpty(_instanceId) ? Guid.NewGuid().ToString() : _instanceId;

            //IPCRestProxy pcRestProxy = new PCRestProxy(webProtocol, pcServerAndPort, domain, project, proxyURL, proxyUserName, proxyPassword);
            //_pcRestProxy = pcRestProxy;

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            _workDirectory = string.Format(_workDirectory, unixTimestamp.ToString());


            //_pcBuilder = new PCBuilder(pcServerAndPort, pcServername, pcUserName, pcPassword, domain,
            //    project, testID, autoTestInstance == true, testInstanceID, timeslotDurationHours, timeslotDurationMinutes,
            //    pcPostRunAction, useVUDs == true, useSLAStatus == true, description,
            //    trending, trendReportID, webProtocol == "https",
            //    proxyURL, proxyUserName, proxyPassword, _workDirectory, _logFileName);

            _configurator = new Configurator(pcServerURL, pcUserName, pcPassword, domain, project, testID,
                autoTestInstance.ToString(), testInstanceID, pcPostRunAction, 
                proxyURL, proxyUserName, proxyPassword,
                trending, trendReportID, timeslotDurationHours, timeslotDurationMinutes,
                useSLAStatus.ToString(), useVUDs.ToString(), _workDirectory, _logFileName, description);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            //if (!Helper.CheckedConnection)
            //{
            //    var res = MessageBox.Show("Connection was not checked, are you sure you want to exit?", "Warning",
            //        MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //    if (res == MessageBoxResult.No)
            //    {
            //        e.Cancel = true;
            //    }
            //}
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void HandleTestInstanceCheck(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TestInstanceID != null)
                {
                    RadioButton rb = sender as RadioButton;
                    TestInstanceID.IsEnabled = (rb.Name == "SpecifyTestInstance");
                    TestInstanceID.Text = (rb.Name == "SpecifyTestInstance") ? "Enter Test Instance ID" : "";
                    TestInstanceID.SelectAll();
                    MoveToNextUIElement(e);
                }
            }
            catch
            { }
        }

        private void HandleTrendCheck(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TrendReportID != null)
                {
                    RadioButton rb = sender as RadioButton;
                    TrendReportID.IsEnabled = (rb.Name == "UseTrendReportID");
                    TrendReportID.Text = (rb.Name == "UseTrendReportID") ? "Enter Trend Report ID" : "";
                    TrendReportID.SelectAll();
                    MoveToNextUIElement(e);
                }
            }
            catch
            { }
        }

        void MoveToNextUIElement(RoutedEventArgs e)
        {
            // Creating a FocusNavigationDirection object and setting it to a
            // local field that contains the direction selected.
            FocusNavigationDirection focusDirection = FocusNavigationDirection.Next;

            // MoveFocus takes a TraveralReqest as its argument.
            TraversalRequest request = new TraversalRequest(focusDirection);

            // Gets the element with keyboard focus.
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            // Change keyboard focus.
            if (elementWithFocus != null)
            {
                if (elementWithFocus.MoveFocus(request)) e.Handled = true;
            }
        }

        void DisplayReportInPSConsole (string reportFile)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "powershell.exe";
            psi.Arguments = string.Format("Get-Content -Path {0} -Wait", reportFile);
            Process process = new Process();
            process.StartInfo = psi;
            process.Start();
        }
    }
}
