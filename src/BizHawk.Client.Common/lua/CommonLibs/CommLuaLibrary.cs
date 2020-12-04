using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BizHawk.Client.Common
{
	[Description("A library for communicating with other programs")]
	public sealed class CommLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		private readonly IDictionary<Guid, ClientWebSocketWrapper> _websockets = new Dictionary<Guid, ClientWebSocketWrapper>();

		public CommLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "comm";

		//TO DO: not fully working yet!
		[LuaMethod("getluafunctionslist", "returns a list of implemented functions")]
		public static string GetLuaFunctionsList()
		{
			var list = new StringBuilder();
			foreach (var function in typeof(CommLuaLibrary<TTable>).GetMethods())
			{
				list.AppendLine(function.ToString());
			}
			return list.ToString();
		}

		[LuaMethod("socketServerIsConnected", "socketServerIsConnected")]
		public bool SocketServerIsConnected() => APIs.Comm.Sockets.Connected;

		[LuaMethod("socketServerScreenShot", "sends a screenshot to the Socket server")]
		public string SocketServerScreenShot()
		{
			CheckSocketServer();
			return APIs.Comm.Sockets?.SendScreenshot();
		}

		[LuaMethod("socketServerScreenShotResponse", "sends a screenshot to the Socket server and retrieves the response")]
		public string SocketServerScreenShotResponse()
		{
			CheckSocketServer();
			return APIs.Comm.Sockets?.SendScreenshot(1000);
		}

		[LuaMethod("socketServerSend", "sends a string to the Socket server")]
		public int SocketServerSend(string SendString)
		{
			if (!CheckSocketServer())
			{
				return -1;
			}
			return APIs.Comm.Sockets.SendString(SendString);
		}

		[LuaMethod("socketServerResponse", "receives a message from the Socket server")]
		public string SocketServerResponse()
		{
			CheckSocketServer();
			return APIs.Comm.Sockets?.ReceiveMessage();
		}

		[LuaMethod("socketServerSuccessful", "returns the status of the last Socket server action")]
		public bool SocketServerSuccessful()
		{
			return CheckSocketServer() && APIs.Comm.Sockets.Successful;
		}

		[LuaMethod("socketServerSetTimeout", "sets the timeout in milliseconds for receiving messages")]
		public void SocketServerSetTimeout(int timeout)
		{
			CheckSocketServer();
			APIs.Comm.Sockets?.SetTimeout(timeout);
		}

		[LuaMethod("socketServerSetIp", "sets the IP address of the Lua socket server")]
		public void SocketServerSetIp(string ip)
		{
			CheckSocketServer();
			APIs.Comm.Sockets.IP = ip;
		}

		[LuaMethod("socketServerSetPort", "sets the port of the Lua socket server")]
		public void SocketServerSetPort(int port)
		{
			CheckSocketServer();
			APIs.Comm.Sockets.Port = port;
		}

		[LuaMethod("socketServerGetIp", "returns the IP address of the Lua socket server")]
		public string SocketServerGetIp()
		{
			return APIs.Comm.Sockets?.IP;
		}

		[LuaMethod("socketServerGetPort", "returns the port of the Lua socket server")]
		public int? SocketServerGetPort()
		{
			return APIs.Comm.Sockets?.Port;
		}

		[LuaMethod("socketServerGetInfo", "returns the IP and port of the Lua socket server")]
		public string SocketServerGetInfo()
		{
			return CheckSocketServer()
				? APIs.Comm.Sockets.GetInfo()
				: "";
		}

		private bool CheckSocketServer()
		{
			if (APIs.Comm.Sockets == null)
			{
				Log("Socket server was not initialized, please initialize it via the command line");
				return false;
			}

			return true;
		}

		// All MemoryMappedFile related methods
		[LuaMethod("mmfSetFilename", "Sets the filename for the screenshots")]
		public void MmfSetFilename(string filename)
		{
			CheckMmf();
			APIs.Comm.MMF.Filename = filename;
		}

		[LuaMethod("mmfGetFilename", "Gets the filename for the screenshots")]
		public string MmfGetFilename()
		{
			CheckMmf();
			return APIs.Comm.MMF?.Filename;
		}

		[LuaMethod("mmfScreenshot", "Saves screenshot to memory mapped file")]
		public int MmfScreenshot()
		{
			CheckMmf();
			return APIs.Comm.MMF.ScreenShotToFile();
		}

		[LuaMethod("mmfWrite", "Writes a string to a memory mapped file")]
		public int MmfWrite(string mmf_filename, string outputString)
		{
			CheckMmf();
			return APIs.Comm.MMF.WriteToFile(mmf_filename, outputString);
		}
		[LuaMethod("mmfRead", "Reads a string from a memory mapped file")]
		public string MmfRead(string mmf_filename, int expectedSize)
		{
			CheckMmf();
			return APIs.Comm.MMF?.ReadFromFile(mmf_filename, expectedSize);
		}

		private void CheckMmf()
		{
			if (APIs.Comm.MMF == null)
			{
				Log("Memory mapped file was not initialized, please initialize it via the command line");
			}
		}

		// All HTTP related methods
		[LuaMethod("httpTest", "tests HTTP connections")]
		public string HttpTest()
		{
			if (APIs.Comm.HTTP == null) throw new NullReferenceException(); // to match previous behaviour
			return APIs.Comm.HttpTest();
		}

		[LuaMethod("httpTestGet", "tests the HTTP GET connection")]
		public string HttpTestGet()
		{
			CheckHttp();
			return APIs.Comm.HttpTestGet();
		}

		[LuaMethod("httpGet", "makes a HTTP GET request")]
		public string HttpGet(string url)
		{
			CheckHttp();
			return APIs.Comm.HTTP?.ExecGet(url);
		}

		[LuaMethod("httpPost", "makes a HTTP POST request")]
		public string HttpPost(string url, string payload)
		{
			CheckHttp();
			return APIs.Comm.HTTP?.ExecPost(url, payload);
		}

		[LuaMethod("httpPostScreenshot", "HTTP POST screenshot")]
		public string HttpPostScreenshot()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.SendScreenshot();
		}

		[LuaMethod("httpSetTimeout", "Sets HTTP timeout in milliseconds")]
		public void HttpSetTimeout(int timeout)
		{
			CheckHttp();
			APIs.Comm.HTTP?.SetTimeout(timeout);
		}

		[LuaMethod("httpSetPostUrl", "Sets HTTP POST URL")]
		public void HttpSetPostUrl(string url)
		{
			CheckHttp();
			APIs.Comm.HTTP.PostUrl = url;
		}

		[LuaMethod("httpSetGetUrl", "Sets HTTP GET URL")]
		public void HttpSetGetUrl(string url)
		{
			CheckHttp();
			APIs.Comm.HTTP.GetUrl = url;
		}

		[LuaMethod("httpGetPostUrl", "Gets HTTP POST URL")]
		public string HttpGetPostUrl()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.PostUrl;
		}

		[LuaMethod("httpGetGetUrl", "Gets HTTP GET URL")]
		public string HttpGetGetUrl()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.GetUrl;
		}

		private void CheckHttp()
		{
			if (APIs.Comm.HTTP == null)
			{
				Log("HTTP was not initialized, please initialize it via the command line");
			}
		}

#if ENABLE_WEBSOCKETS
		[LuaMethod("ws_open", "Opens a websocket and returns the id so that it can be retrieved later.")]
		[LuaMethodExample("local ws_id = comm.ws_open(\"wss://echo.websocket.org\");")]
		public string WebSocketOpen(string uri)
		{
			var wsServer = APIs.Comm.WebSockets;
			if (wsServer == null)
			{
				Log("WebSocket server is somehow not available");
				return null;
			}
			var guid = new Guid();
			_websockets[guid] = wsServer.Open(new Uri(uri));
			return guid.ToString();
		}

		[LuaMethod("ws_send", "Send a message to a certain websocket id (boolean flag endOfMessage)")]
		[LuaMethodExample("local ws = comm.ws_send(ws_id, \"some message\", true);")]
		public void WebSocketSend(string guid, string content, bool endOfMessage)
		{
			if (_websockets.TryGetValue(Guid.Parse(guid), out var wrapper)) wrapper.Send(content, endOfMessage);
		}

		[LuaMethod("ws_receive", "Receive a message from a certain websocket id and a maximum number of bytes to read")]
		[LuaMethodExample("local ws = comm.ws_receive(ws_id, str_len);")]
		public string WebSocketReceive(string guid, int bufferCap) => _websockets.TryGetValue(Guid.Parse(guid), out var wrapper)
			? wrapper.Receive(bufferCap)
			: null;

		[LuaMethod("ws_get_status", "Get a websocket's status")]
		[LuaMethodExample("local ws_status = comm.ws_get_status(ws_id);")]
		public int? WebSocketGetStatus(string guid) => _websockets.TryGetValue(Guid.Parse(guid), out var wrapper)
			? (int) wrapper.State
			: (int?) null;

		[LuaMethod("ws_close", "Close a websocket connection with a close status")]
		[LuaMethodExample("local ws_status = comm.ws_close(ws_id, close_status);")]
		public void WebSocketClose(string guid, WebSocketCloseStatus status, string closeMessage)
		{
			if (_websockets.TryGetValue(Guid.Parse(guid), out var wrapper)) wrapper.Close(status, closeMessage);
		}
#endif
	}
}