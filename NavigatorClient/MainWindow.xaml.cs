///////////////////////////////////////////////////////////////////////////////////
// NavigatorClient.xaml.cs - Demonstrates a remote Code Analyzer                 //
//Author: Dhaval Kumar Sonavaria                                                 //
//                                                                               //
// ver 1.0                                                                       //
//Orignal file by : Dr. Jim Fawcett                                              //
// CSE681 - Software Modeling and Analysis, Fall 2018                           //
///////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines WPF application processing by the client.  The client
 * displays a local FileFolder view, and provides functionality to select files
 * and either do Dependency Analysis or provide Strong Componenets.  
 * 
 * 
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 6 Dec 2018
 * - first release
 */
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
using System.IO;
using System.Threading;
using MessagePassingComm;

namespace Navigator
{
    public partial class MainWindow : Window
    {
        private IFileMgr fileMgr { get; set; } = null;  // note: Navigator just uses interface declarations
        Comm comm { get; set; } = null;
        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        Thread rcvThread = null;

        public MainWindow()
        {
            InitializeComponent();
            initializeEnvironment();
            Console.Title = "Navigator Client";
            fileMgr = FileMgrFactory.create(FileMgrType.Local); // uses Environment
            getTopFiles();
            comm = new Comm(ClientEnvironment.address, ClientEnvironment.port);
            initializeMessageDispatcher();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
            AutomatedTestUnit();
        }
        //----< make Environment equivalent to ClientEnvironment >-------

        void initializeEnvironment()
        {
            Environment.root = ClientEnvironment.root;
            Environment.address = ClientEnvironment.address;
            Environment.port = ClientEnvironment.port;
            Environment.endPoint = ClientEnvironment.endPoint;
        }
        //----< define how to process each message command >-------------

        void initializeMessageDispatcher()
        {
            // load remoteFiles listbox with files from root

            messageDispatcher["getTopFiles"] = (CommMessage msg) =>
            {
                remoteFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    remoteFiles.Items.Add(file);
                }
            };
            // load remoteDirs listbox with dirs from root

            messageDispatcher["getTopDirs"] = (CommMessage msg) =>
            {
                remoteDirs.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    remoteDirs.Items.Add(dir);
                }
            };
            // load remoteFiles listbox with files from folder

            messageDispatcher["moveIntoFolderFiles"] = (CommMessage msg) =>
            {
                remoteFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    remoteFiles.Items.Add(file);
                }
            };
            // load remoteDirs listbox with dirs from folder

            messageDispatcher["moveIntoFolderDirs"] = (CommMessage msg) =>
            {
                remoteDirs.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    remoteDirs.Items.Add(dir);
                }
            };
            //Sends request to connect to server//
            messageDispatcher["Connection"] = (CommMessage msg) =>
            {
                CodePopUp cp = new CodePopUp();
                string contents = msg.arguments[0];

                cp.codeView.Text = contents;
                cp.Show();
            };
            //Displays any type of File received from Server
            messageDispatcher["DisplayFile"] = (CommMessage msg) =>
            {
                string text = System.IO.File.ReadAllText(msg.arguments[0]);
                CodePopUp popup = new CodePopUp();
                
                popup.codeView.Text = text;
                popup.Show();
            };
        }           
            //----< define processing for GUI's receive thread >-------------
        
    void rcvThreadProc()
            {
                Console.Write("\n  starting client's receive thread");
                while (true)
                {
                    CommMessage msg = comm.getMessage();
                    msg.show();
                    if (msg.command == null)
                        continue;

                    // pass the Dispatcher's action value to the main thread for execution

                    Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
                }
            }
            //----< shut down comm when the main window closes >-------------

            private void Window_Closed(object sender, EventArgs e)
            {
                comm.close();

                // The step below should not be nessary, but I've apparently caused a closing event to 
                // hang by manually renaming packages instead of getting Visual Studio to rename them.

                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            //----< not currently being used >-------------------------------

            private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
            {
            }
            //----< show files and dirs in root path >-----------------------

            public void getTopFiles()
            {
                List<string> files = fileMgr.getFiles().ToList<string>();
                localFiles.Items.Clear();
                foreach (string file in files)
                {
                    localFiles.Items.Add(file);
                }
                List<string> dirs = fileMgr.getDirs().ToList<string>();
                localDirs.Items.Clear();
                foreach (string dir in dirs)
                {
                    localDirs.Items.Add(dir);
                }
            }
            //----< move to directory root and display files and subdirs >---

            private void localTop_Click(object sender, RoutedEventArgs e)
            {
                fileMgr.currentPath = "";
                getTopFiles();
            }
           //Add file to analyze dependency or Strong Components
           
            private void localFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            {
                string fileName = localFiles.SelectedValue as string;
                remoteDirs.Items.Add(fileName);
                
            }
            //----< move to parent directory and show files and subdirs >----

            private void localUp_Click(object sender, RoutedEventArgs e)
            {
                if (fileMgr.currentPath == "")
                    return;
                fileMgr.currentPath = fileMgr.pathStack.Peek();
                fileMgr.pathStack.Pop();
                getTopFiles();
            }
            //----< move into subdir and show files and subdirs >------------

            private void localDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            {
                string dirName = localDirs.SelectedValue as string;
                fileMgr.pathStack.Push(fileMgr.currentPath);
                fileMgr.currentPath = dirName;
                getTopFiles();
                string fileName = localFiles.SelectedValue as string;
                remoteDirs.Items.Add(fileName);
            }
            
           
            //----< move to root of remote directories >---------------------
            /*
             * - sends a message to server to get files from root
             * - recv thread will create an Action<CommMessage> for the UI thread
             *   to invoke to load the remoteFiles listbox
             */
            private void RemoteTop_Click(object sender, RoutedEventArgs e)
            {
                CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
                msg1.from = ClientEnvironment.endPoint;
                msg1.to = ServerEnvironment.endPoint;
                msg1.author = "Jim Fawcett";
                msg1.command = "getTopFiles";
                msg1.arguments.Add("");
                comm.postMessage(msg1);
                CommMessage msg2 = msg1.clone();
                msg2.command = "getTopDirs";
                comm.postMessage(msg2);
            }
        //----< download file and display source in popup window >-------


        private void remoteDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = remoteDirs.SelectedValue as string;
            remoteDirs.Items.Remove(fileName);
        }

            private void ConnecttoServer_Click(object sender, RoutedEventArgs e)
            {
                CommMessage msg1 = new CommMessage(CommMessage.MessageType.connect);
                msg1.from = ClientEnvironment.endPoint;
                msg1.author = "Dhaval Sonavaria";
                msg1.to = ServerEnvironment.endPoint;
                msg1.command = "Connection";
                comm.postMessage(msg1);
            }

            private void DependencyAnalysisButton_Click(object sender, RoutedEventArgs e)
            {
                if (remoteDirs.Items.Count==0)
                {
                    CodePopUp cp = new CodePopUp();
                    string contents = "Error: No File Selected!";

                    cp.codeView.Text = contents;
                    cp.Show();
                     return;
                }
                CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
                msg1.from = ClientEnvironment.endPoint;
                msg1.to = ServerEnvironment.endPoint;
                msg1.command = "DependencyAnalysis";
                msg1.author = "Dhaval Sonavaria";
                List<string> files = new List<string>();
                foreach (string var in remoteDirs.Items)
                {
                string filelocation = ServerEnvironment.root + var;
                string completelocation = System.IO.Path.GetFullPath(filelocation);
                msg1.arguments.Add(completelocation);
                }
                
                comm.postMessage(msg1);
            }

        private void StrongComponent_Click(object sender, RoutedEventArgs e)
        {
            if (remoteDirs.Items.Count == 0)
            {
                CodePopUp cp = new CodePopUp();
                string contents = "Error: No File Selected!";

                cp.codeView.Text = contents;
                cp.Show();
                return;
            }
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "StrongComponent";
            msg1.author = "Dhaval Sonavaria";
            List<string> files = new List<string>();
            foreach (string var in remoteDirs.Items)
            {
                string filelocation = ServerEnvironment.root + var;
                string completelocation = System.IO.Path.GetFullPath(filelocation);
                msg1.arguments.Add(completelocation);
            }

            comm.postMessage(msg1);
        }

       async void AutomatedTestUnit()
        {
            //Shows Successful connection
            ConnecttoServer_Click(ConnecttoServer, null);
            
            //Shows an error as no file is selected
            DependencyAnalysisButton_Click(DependencyAnalysis, null);
            
            //add files to selection
            foreach (string var in localFiles.Items)
            {
                remoteDirs.Items.Add(var);
            }
            
            //Calculate Dependencies of selectedd files
            DependencyAnalysisButton_Click(DependencyAnalysis, null);
            await Task.Delay(30);
            //Calculate Strong Component of Selected files
            StrongComponent_Click(StrongComponents, null);
            
        }
    }
}