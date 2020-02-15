/////////////////////////////////////////////////////////////////////////////////////
// NavigatorServer.cs - File Server for WPF RemoteCodeAnalyzerServer Application   //
// ver 1.0                                                                         //
//  CSE681 - Software Modeling and Analysis, Fall 2018
//Author: Dhaval Sonavaria
/////////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package is a wrapper around the Code Analyzer and uses the Demo Executive 
 * to Analyze files sent by the Client.
 * It uses a message dispatcher that handles processing of all incoming and outgoing
 * messages.
 * 
 * Maintanence History:
 * --------------------
 * ver 1.0 - 6 Dec 2018
 * - added message dispatcher which works very well - see below
 * - added these comments
 * ver 1.0 - 6 Dec 2018
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using CodeAnalysis;

namespace Navigator
{
  public class NavigatorServer
  {
    IFileMgr localFileMgr { get; set; } = null;
    Comm comm { get; set; } = null;

    Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher = 
      new Dictionary<string, Func<CommMessage, CommMessage>>();

    /*----< initialize server processing >-------------------------*/

    public NavigatorServer()
    {
      initializeEnvironment();
      Console.Title = "Navigator Server";
      localFileMgr = FileMgrFactory.create(FileMgrType.Local);
    }
    /*----< set Environment properties needed by server >----------*/

    void initializeEnvironment()
    {
      Environment.root = ServerEnvironment.root;
      Environment.address = ServerEnvironment.address;
      Environment.port = ServerEnvironment.port;
      Environment.endPoint = ServerEnvironment.endPoint;
    }
        /*----< define how each message will be processed >------------*/

        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> getTopFiles = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopFiles"] = getTopFiles;

            Func<CommMessage, CommMessage> getTopDirs = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopDirs";
                reply.arguments = localFileMgr.getDirs().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopDirs"] = getTopDirs;

            Func<CommMessage, CommMessage> moveIntoFolderFiles = (CommMessage msg) =>
            {
                if (msg.arguments.Count() == 1)
                    localFileMgr.currentPath = msg.arguments[0];
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["moveIntoFolderFiles"] = moveIntoFolderFiles;

            Func<CommMessage, CommMessage> Connection = (CommMessage msg) =>
            {
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                List<string> success = new List<string>();
                success.Add("Connection Successful");
                reply.to = msg.from;
                reply.from = msg.to;
                reply.arguments = success;
                reply.command = "Connection";
                return reply;
            };
            messageDispatcher["Connection"] = Connection;

            Func<CommMessage, CommMessage> DependencyAnalysis = (CommMessage msg) =>
            {
                
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "DisplayFile";
                Executive exec = new CodeAnalysis.Executive();
                reply.arguments=exec.ServerCallForDependencyAnalysis(msg.arguments);
                

               return reply;
            };
            messageDispatcher["DependencyAnalysis"] = DependencyAnalysis;

            Func<CommMessage, CommMessage> StrongComponent = (CommMessage msg) =>
            {

                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "DisplayFile";
                Executive exec = new CodeAnalysis.Executive();
                reply.arguments = exec.ServerCallForStrongComponents(msg.arguments);


                return reply;
            };
            messageDispatcher["StrongComponent"] = StrongComponent;


        }
    /*----< Server processing >------------------------------------*/
    /*
     * - all server processing is implemented with the simple loop, below,
     *   and the message dispatcher lambdas defined above.
     */
    static void Main(string[] args)
    {
      TestUtilities.title("Starting Navigation Server", '=');
      try
      {
        NavigatorServer server = new NavigatorServer();
        server.initializeDispatcher();
        server.comm = new MessagePassingComm.Comm(ServerEnvironment.address, ServerEnvironment.port);
        
        while (true)
        {
          CommMessage msg = server.comm.getMessage();
          if (msg.type == CommMessage.MessageType.closeReceiver)
            break;
          msg.show();
          if (msg.command == null)
            continue;
          CommMessage reply = server.messageDispatcher[msg.command](msg);
          reply.show();
          server.comm.postMessage(reply);
        }
      }
      catch(Exception ex)
      {
        Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
      }
    }
  }
}
