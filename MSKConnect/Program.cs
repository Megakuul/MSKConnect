using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using System.Threading;
using System.Runtime.CompilerServices;

namespace MSKConnect
{
    internal class Program
    {
        static SshClient client { get; set; }
        static ShellStream shell { get; set; }

        static string hostname { get; set; }
        static string username { get; set; }
        static string password { get; set; }
        static string terminalType { get; set; } = "xterm";

        const string errormsg =
            "An error occurred while reading the arguments.\r\n" +
            "Try again and make sure the arguments are entered as shown below\r\n\r\n";

        const string helpmsg =
            "MSKConnect: command-line ssh connection utility\r\n\r\n" +
            "Options:\r\n" +
            "  -ci      connection info [hostname] [username] [password]\r\n" +
            "  -t       terminal type [terminal] (xterm, vt100) standard: xterm";

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(errormsg);
                Console.WriteLine(helpmsg);
                return;
            }

            if (!validateArguments(args))
            {
                Console.WriteLine(helpmsg);
                Console.ReadLine();
                return;
            }

            if (!connectSSH(hostname, username, password))
            {
                Console.WriteLine("Cannot connect to ssh target... try again");
                return;
            }

            //sends CTRL-C to the shell instead of exiting the program
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                //Checks if the ConsoleSpecialKey is CTRL-C and not BREAK
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    e.Cancel = true;
                    shell.Write("\x03");
                }

            };

            writeToShell();

        }

        /// <summary>
        /// Creates a recursive loop to run Commands
        /// </summary>
        static void writeToShell()
        {
            //get the next Key
            var curKey = Console.ReadKey(true);

            //Check if the Key is an Special Key, if it is send special char-combination to the Stream
            //And btw, for all the little kids who are crying that a switch statement would be better here. I do it this way, and if I do it this way, it's the best way.

            if (curKey.Key == ConsoleKey.UpArrow)
                shell.Write("\x1b" + "[A");
            else if (curKey.Key == ConsoleKey.DownArrow)
                shell.Write("\x1b" + "[B");
            else if (curKey.Key == ConsoleKey.RightArrow)
                shell.Write("\x1b" + "[C");
            else if (curKey.Key == ConsoleKey.LeftArrow)
                shell.Write("\x1b" + "[D");
            else if (curKey.Modifiers == ConsoleModifiers.Control && curKey.Key == ConsoleKey.F1)
                Environment.Exit(0);
            else
                shell.Write(curKey.KeyChar.ToString());

            writeToShell();

        }

        /// <summary>
        /// Controls and parses the args
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static bool validateArguments(string[] args)
        {
            if (args[0] == "help")
                return false;

            if (args.Length < 4)
            {
                Console.WriteLine(errormsg);
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "help":
                        return false;
                    case "-ci":
                        hostname = args[i + 1];
                        username = args[i + 2];
                        password = args[i + 3];
                        i = i + 3;
                        break;
                    case "-t":
                        if (args.Length <= i + 1)
                        {
                            Console.WriteLine(errormsg);
                            return false;
                        }
                        terminalType = args[i + 1];
                        i++;
                        break;
                }
            }
            if (hostname == null || username == null || password == null)
            {
                Console.WriteLine(errormsg);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Connects to a ssh session
        /// </summary>
        /// <param name="host">hostname</param>
        /// <param name="username">username</param>
        /// <param name="password">password</param>
        /// <returns>if connected succesfully => true</returns>
        static bool connectSSH(string host, string username, string password)
        {
            //creates connection client
            try
            {
                client = new SshClient(host, username, password);
                client.Connect();
            }
            catch
            {
                return false;
            }

            //create shell
            try
            {
                shell = client.CreateShellStream(terminalType, 200, 200, 200, 200, 8000);
                shell.DataReceived += Shell_DataReceived;
                shell.Flush();
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Eventhandler to get and decode the data from the stream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Shell_DataReceived(object sender, Renci.SshNet.Common.ShellDataEventArgs e)
        {
            Console.Write(Encoding.Default.GetString(e.Data));
        }
    }
}