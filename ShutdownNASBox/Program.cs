using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.Diagnostics;

namespace ShutdownNASBox
{
    class Program
    {
        static void Main(string[] args)
        {
            string host_name = "";
            string user_name = "";
            string pass_word = "";

            Console.WriteLine("Netgear ReadyNAS Duo Shutdown Utility (2011.07.04)");
            Console.WriteLine("Shaw Young - http://www.shawson.co.uk/");
            Console.WriteLine("");

            if (args.Length < 1)
            {
                DisplayHelp();
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];

                switch (a)
                {
                    case "/?":
                    case "-?":
                    case "?":
                        DisplayHelp();
                        return;
                    case "-h":
                        i++;
                        if (i > args.Length)
                            Console.WriteLine("Invalid Host Name!");
                        else
                            host_name = args[i];
                        break;
                    case "-u":
                        i++;
                        if (i > args.Length)
                            Console.WriteLine("Invalid User Name!");
                        else
                            user_name = args[i];
                        break;
                    case "-p":
                        i++;
                        if (i > args.Length)
                            Console.WriteLine("Invalid Password!");
                        else
                            pass_word = args[i];
                        break;
                }
            }

            if (host_name.Length < 1)
            {
                Console.WriteLine("Missing host name.");
                return;
            }

            if (user_name.Length < 1)
            {
                Console.WriteLine("Missing user name.");
                return;
            }

            if (pass_word.Length < 1)
            {
                Console.WriteLine("Missing password.");
                return;
            }

            SendShutDownMessage(host_name, user_name, pass_word);
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("Usage : ShutDownNASBox -h <hostname> -u <username> -p <password>");
        }

        private static void SendShutDownMessage(string host_name, string user_name, string pass_word)
        {
            using (WebClient client = new WebClient())
            {
                // add the basic http authentication username & password
                client.Credentials = new NetworkCredential(user_name, pass_word);

                // register the fact that the connection uses SSL
                // (otherwise it defaults to TLS and you get an exception)
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;

                // set it to effectivly ignore the SSL certificate
                ServicePointManager.ServerCertificateValidationCallback =
                                                        delegate { return true; };

                // build a NameValueCollection which holds those 6 values we saw
                // being posted when we used firebug
                NameValueCollection c = new NameValueCollection();
                c.Add("PAGE", "System");
                c.Add("OUTER_TAB", "tab_shutdown");
                c.Add("INNER_TAB", "NONE");
                c.Add("shutdown_option1", "1");
                c.Add("command", "poweroff");
                c.Add("OPERATION", "set");

                try
                {
                    WriteToEventLog(string.Format("Sending Request to {0}", host_name), EventLogEntryType.Information, 0);

                    // Post the data!  This will return as a byte array

                    byte[] bytes =
                        client.UploadValues(string.Format("https://{0}/get_handler", host_name), c);

                    // convert the byte array into text we can read!
                    string result = Encoding.ASCII.GetString(bytes);

                    Console.Write(string.Format("Request to '{0}' Sent Successfully", host_name));
                    //Console.Write(result);

                    WriteToEventLog(string.Format("Request to '{0}' Sent Successfully", host_name), EventLogEntryType.Information, 1);

                }
                catch (WebException we)
                {
                    if (we.Message.Contains("The remote name could not be resolved"))
                    {
                        Console.WriteLine(string.Format("Unable to resolve host name '{0}'.  This could be because;", host_name));
                        Console.WriteLine("     - You have mis-typed the value of the host name parameter");
                        Console.WriteLine("     - The device is already off");
                        Console.WriteLine("     - There is no network connectivity between you and the device");
                        WriteToEventLog(string.Format("Unable to resolve host name '{0}'", host_name), EventLogEntryType.Error, 10);
                    }
                    else if (we.Message.Contains("(401) Unauthorized"))
                    {
                        Console.WriteLine(string.Format("Authorization failed for '{0}'.  Check user name and password.", host_name));
                        WriteToEventLog(string.Format("Authorization failed for '{0}'", host_name), EventLogEntryType.Error, 20);
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Unknown connectivity error sending request to '{0}' : {1}", host_name, we.Message));
                        WriteToEventLog(string.Format("Unknown connectivity error sending request to '{0}' : {1}", host_name, we.Message), EventLogEntryType.Error, 98);
                    }   
                }
                catch (Exception e)
                {
                    Console.Write(string.Format("Unknown error sending request to {0} : {1}", host_name, e.Message));
                    WriteToEventLog(string.Format("Unknown error sending request to {0} : {1}", host_name, e.Message), EventLogEntryType.Error, 99);
                }
            }
        }

        private static void WriteToEventLog(string sEvent, EventLogEntryType type, int EventId)
        {
            try
            {
                string sSource = "ShutdownNASBox";
                string sLog = "Application";

                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, sLog);

                EventLog.WriteEntry(sSource, sEvent, type, EventId);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed write to system event Log.  Make sure this tool is run with Administrator privileges.");
            }
        }
    }
}