using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Threading;
using CookComputing.XmlRpc;
using Palaso.Services.Dictionary;
using Palaso.Services.ForServers;

namespace Palaso.Services
{
	/// <summary>
	/// Wraps the inter-process communication mechanism so that this code isn't
	/// spread throughout the implementations.  The details here can be changed, with
	/// less impact on the servers and clients if we ever leave the (.net 2)
	/// remoting and move back to the (.net 3) WPF approach.
	/// </summary>
	public class IpcSystem
	{
		public static int StartingPortBase = 5678;

		public static readonly int NumberOfPortsToTry = 4;

		/// <summary>
		/// Because .net remoting (.net 2) requires each application to have its own port,
		/// when providing services, we need to first find an open port, and when we go
		/// looking for providers, we may need to query on serveral ports.
		/// </summary>
		public static int GetStartingPort(string serviceName)
		{
			// Strategy using a hash code to find a starting port for a service
			// This gives us a greater number of services we can support without
			// having to scan very many ports.
			int port = GetStringHash(serviceName);
			port *= NumberOfPortsToTry;
			port += StartingPortBase;
			return port;
		}

		private static byte GetStringHash(string str)
		{
			int hash = 5381;
			foreach (char c in str)
			{
				hash = ((hash << 5) + hash) + c;
			}
			hash = (hash ^ (hash >> 8) ^ (hash >> 16) ^ (hash >> 24)) & 0xff;
			return (byte)hash;
		}

		public static string URLPrefix
		{
			get
			{
				//return "net.pipe://localhost/";
				return "http://localhost";
			}
		}


		public static TServiceInterface GetExistingService<TServiceInterface>(string unescapedServiceName)
			where TServiceInterface : class, IXmlRpcProxy, IPingable
		{
			System.Diagnostics.Debug.Assert(!unescapedServiceName.Contains("%"), "please leave it to this method to figure out the correct escaping to do.");
			System.Diagnostics.Debug.Assert(!IsWellFormedUriStringMonoSafe(unescapedServiceName), "This method needs a service name, not a whole uri.");
			//  System.Diagnostics.Debug.WriteLine("("+unescapedServiceName+") trying to get service: " + unescapedServiceName);


			TServiceInterface serviceProxy = XmlRpcProxyGen.Create<TServiceInterface>();
			int startingPort = GetStartingPort(unescapedServiceName);
			for (int port = startingPort; port < startingPort + NumberOfPortsToTry; port++)
			{
				//nb: this timeout does work, but reducing it to the point where it
				//actually returns earlier just makes it unreliable (consisten test failures to find
				//existing services on my fast machine). 100,000 is default timeout, but
				//you get a "nope, not there" in a second or two

				//serviceProxy.Timeout = 1000;//2000 is safe on my fast machine
				serviceProxy.Url = GetUrlForService(FixupServiceName(unescapedServiceName), port);

				//Mutex m = GetMutexForPortAndService(port, unescapedServiceName);
				try
				{
					Mutex.OpenExisting(GetNameOfMutexForPortAndService(port, unescapedServiceName));
				}
				catch (Exception e)
				{
					// there is no mutex with this name so no one is serving this on that port.
					continue;// loop and check the next port
				}

				try
				{
					Debug.WriteLine("Checking for service at " + serviceProxy.Url);
					//hack: just need some way to see if it's alive, there should be a lower-level way to do that
					((IPingable)serviceProxy).Ping();
					Debug.WriteLine("   Found & pinged.");
					return serviceProxy;//found one
				}
				catch (Exception e) //swallow
				{
					Debug.WriteLine("   " + e.Message + " (not necessarily a problem)");
				}
			}
			return null;
		}



		private static string GetUrlForService(string name, int port)
		{
			return URLPrefix + ":" + port + "/" + FixupServiceName(name);
		}

		private static string FixupServiceName(string name)
		{
			return name.Replace(@"\", "/");
		}

		public static string GetRemotingNameForService(string name)
		{
			return name;
		}

  #if OptimizeRawPortChoosing
		private static int GetAChannelWeCanUse(out Mutex portMutex)
	{
			portMutex = null;
#else
		  private static int GetAChannelWeCanUse(string serviceName)
		  {

#endif
			int startingPort = GetStartingPort(serviceName);
			IDictionary props = new Hashtable();
			props["bindTo"] = "127.0.0.1"; //local only
			props["name"] = GetChannelName(startingPort);
			props["port"] = startingPort;

			//this only seems to work if the channel belongs to our process!
#if DoWeNeedThis
			if (null != ChannelServices.GetChannel((string)props["name"]))
			{
				return StartingPort; //we already own this port in this process
			}
#endif

			int port = startingPort;
			for (; port < startingPort + NumberOfPortsToTry; port++)
			{
 //       Although this may be sound in practice, in tests it was a problem somehow...
  //         it was only an optimisation, so I'm going to leave it out for now
  #if OptimizeRawPortChoosing
			 portMutex = GetMutexForPort(port);
				if (!portMutex.WaitOne(100, false))
				{
					continue; // don't even try it
				}
#endif

				props["port"] = port;
				props["name"] = GetChannelName(port);
				try
				{
#if DEBUG
					Console.WriteLine("Looking for free port: " + port);
#endif
					IChannel channel = new HttpChannel(props, null, new XmlRpcServerFormatterSinkProvider());
					ChannelServices.RegisterChannel(channel, false);
					break;
				}
				catch (Exception e)
				{
  #if OptimizeRawPortChoosing
					if(portMutex !=null)
						portMutex.ReleaseMutex();
					portMutex = null;
#endif
				}
			}
			if (port < startingPort + NumberOfPortsToTry)
			{
				return port;
			}
			throw new ApplicationException("Could not find a free port on which to create the service.");
		}

		private static Mutex GetMutexForPort(int port)
		{
			return new Mutex(false, "Palaso.IPSystem:" + port.ToString());
		}
		internal static Mutex GetMutexForPortAndService(int port, string serviceName)
		{
			return new Mutex(false, GetNameOfMutexForPortAndService(port, serviceName));
		}

		private static string GetNameOfMutexForPortAndService(int port, string serviceName)
		{
			// mutex names are (may be?) implemeted as files, so we need to make them valid
			foreach (char c in System.IO.Path.GetInvalidPathChars())
			{
			  serviceName = serviceName.Replace(c, '_');
			}
			foreach (char c in System.IO.Path.GetInvalidFileNameChars())
			{
				serviceName = serviceName.Replace(c, '_');
			}
			return "Palaso.IPSystem:" + port.ToString() + "/" + serviceName;
		}

		private static string GetChannelName(int port)
		{
			return "http." + port;
		}

		//        static private void UnregisterHttpChannel(int port)
		//        {
		//            IDictionary props = new Hashtable();
		//            props["port"] = port;
		//            //see if one with this name is registered
		//            IChannel channel = ChannelServices.GetChannel(GetChannelName(port));
		//            if (channel != null)
		//            {
		//                ChannelServices.UnregisterChannel(channel);
		//            }
		//        }


		/// <summary>
		///
		/// </summary>
		/// <returns>Something you must dispose of when you're done, or exitting</returns>
		public static IDisposable StartServingObject(string unescapedServiceName, MarshalByRefObject objectToServe)
		{
			System.Diagnostics.Debug.Assert(!unescapedServiceName.Contains("%"), "please leave it to this method to figure out the correct escaping to do.");
			System.Diagnostics.Debug.Assert(!IsWellFormedUriStringMonoSafe(unescapedServiceName), "This method needs a service name, not a whole uri.");

			Mutex portAndServiceMutex = null;
			Mutex portMutex = null;
			try
			{

#if OptimizeRawPortChoosing
				int port = IpcSystem.GetAChannelWeCanUse(out portMutex);
				Debug.Assert(portMutex != null);
#else
				int port = IpcSystem.GetAChannelWeCanUse(unescapedServiceName);
#endif

#if DEBUG
				Console.WriteLine("registering on port: " + port);
#endif
				portAndServiceMutex = GetMutexForPortAndService(port, unescapedServiceName);
				if (!portAndServiceMutex.WaitOne(10, false))
				{
					throw new ApplicationException("We thought the port was free, but couldn't get the port+serviced name mutex.");
				}
			}
			catch (System.Runtime.Remoting.RemotingException e)
			{
				throw new ApplicationException("An error occured which happens when we try to get a service that was created on the same thread", e);
			}

			//QUESTION: how does the system know to serve up this service on that channel we
			//just made?

			RemotingServices.Marshal(objectToServe, FixupServiceName(unescapedServiceName));
			System.Diagnostics.Debug.WriteLine("Now serving " + FixupServiceName(unescapedServiceName));
			//         File.WriteAllText(@"c:\temp\StartServingObjectLog.txt", "Now serving " + FixupServiceName(unescapedServiceName));

			return new IpcServiceDescriptor(portMutex, portAndServiceMutex);
		}


		/// <summary>
		/// mono bug number 376692
		/// </summary>
		/// <param name="unescapedServiceName"></param>
		/// <returns></returns>
		private static bool IsWellFormedUriStringMonoSafe(string unescapedServiceName)
		{
			return IsWellFormedUriStringMonoSafe(unescapedServiceName.Replace("%5", "_"), UriKind.Absolute);
		}

		private static bool IsWellFormedUriStringMonoSafe(string unescapedServiceName, UriKind uriKind)
		{
			try
			{
				return Uri.IsWellFormedUriString(unescapedServiceName.Replace("%5", "_"), uriKind);
			}
			catch (Exception)
			{
				return false; // mono will, incorrectly, fall through to here
			}
		}
	}


	public interface IPingable
	{
		[XmlRpcMethod("IpcSystem.Ping", Description = "Always returns true.")]
		bool Ping();
	}

}