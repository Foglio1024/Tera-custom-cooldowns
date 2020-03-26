﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nostrum.Extensions;
using TCC.Analysis;
using TCC.Interop.Proxy;
using TCC.Utils;
using TeraPacketParser;
using TeraPacketParser.TeraCommon.Sniffing;
using Server = TeraPacketParser.TeraCommon.Game.Server;

namespace TCC.Sniffing
{
    /// <summary>
    /// Uses a <see cref="TcpListener"/> to receive TERA packets sent by <c>ttb-interface-data</c>.
    /// Uses a <see cref="ToolboxControlInterface"/> to interact with <c>ttb-interface-control</c>.
    /// </summary>
    internal class ToolboxSniffer : ITeraSniffer
    {
        /// <summary>
        /// Used to send HTTP requests to Toolbox.
        /// </summary>
        public class ToolboxControlInterface
        {
            private readonly ToolboxHttpClient _client;

            public ToolboxControlInterface(string address)
            {
                _client = new ToolboxHttpClient(address);
            }

            /// <summary>
            /// Requests <c>ttb-interface-control</c> to return current server id.
            /// </summary>
            /// <returns>server id</returns>
            public async Task<uint> GetServerId()
            {
                var resp = await _client.CallAsync("getServer");
                return resp?.Result?.Value<uint>() ?? 0;
            }
            /// <summary>
            /// Requests <c>ttb-interface-control</c> to return current release version.
            /// </summary>
            /// <returns>release version</returns>
            public async Task<int> GetReleaseVersion()
            {
                var resp = await _client.CallAsync("getReleaseVersion");
                return resp?.Result?.Value<int>() ?? 9901;
            }
            /// <summary>
            /// Requests <c>ttb-interface-control</c> to dump a map to file.
            /// </summary>
            /// <param name="path">path the map will be dumped to</param>
            /// <param name="mapType">type of the map:
            ///     <list type="bullet">
            ///         
            ///         <item>
            ///             <term>"protocol"</term>
            ///             <description>opcode map</description>
            ///         </item>
            ///         <item>
            ///             <term>"sysmsg"</term>
            ///             <description>system messages map</description>
            ///         </item>
            ///     </list>
            /// </param>
            /// <returns>true if successful</returns>
            public async Task<bool> DumpMap(string path, string mapType)
            {
                var resp = await _client.CallAsync("dumpMap", new JObject
            {
                { "path", path},
                { "mapType", mapType }
            });
                return resp?.Result != null && resp.Result.Value<bool>();
            }
            /// <summary>
            /// Requests <c>ttb-interface-control</c> to install hooks for the provided list of opcodes.
            /// </summary>
            /// <param name="opcodes">list of opcode names</param>
            /// <returns>true if successful</returns>
            public async Task<bool> AddHooks(List<string> opcodes)
            {
                var jArray = new JArray();
                opcodes.ForEach(opc => jArray.Add(opc));
                var resp = await _client.CallAsync("addHooks", new JObject
            {
                {"hooks", jArray}
            });
                return resp?.Result != null && resp.Result.Value<bool>();
            }
            /// <summary>
            /// Requests <c>ttb-interface-control</c> to uninstall hooks for the provided list of opcodes.
            /// </summary>
            /// <param name="opcodes">list of opcode names</param>
            /// <returns>true if successful</returns>
            public async Task<bool> RemoveHooks(List<string> opcodes)
            {
                var jArray = new JArray();
                opcodes.ForEach(opc => jArray.Add(opc));
                var resp = await _client.CallAsync("removeHooks", new JObject
            {
                {"hooks", jArray}
            });
                return resp?.Result != null && resp.Result.Value<bool>();
            }
            /// <summary>
            /// Requests <c>ttb-interface-control</c> to perform a DC query.
            /// </summary>
            /// <param name="query">query string</param>
            /// <returns>json object containing query result</returns>
            public async Task<JObject> Query(string query)
            {
                var resp = await _client.CallAsync("query", new JObject
            {
                { "query" , query }
            });

                return resp?.Result?.Value<JObject>();
            }
            /// <summary>
            /// Requests <c>ttb-interface-control</c> to return current protocol version.
            /// </summary>
            /// <returns>protocol version</returns>
            public async Task<uint> GetProtocolVersion()
            {
                var resp = await _client.CallAsync("getProtocolVersion");
                return resp?.Result?.Value<uint>() ?? 0;
            }
        }

        private readonly TcpListener _dataConnection;
        public readonly ToolboxControlInterface ControlConnection;
        private readonly bool _failed;
        private bool _enabled;
        private bool _connected;
        
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_enabled) new Thread(Listen).Start();
            }
        }
        public bool Connected
        {
            get => _connected;
            set
            {
                if (_connected == value) return;
                _connected = value;
                if (!_connected) EndConnection?.Invoke();

            }
        }

        public event Action<Message> MessageReceived;
        public event Action<Server> NewConnection;
        public event Action EndConnection;
        
        public ToolboxSniffer()
        {
            _dataConnection = new TcpListener(IPAddress.Parse("127.0.0.60"), 5200);
            try
            {
                _dataConnection.Start();
                ControlConnection = new ToolboxControlInterface("http://127.0.0.61:5200");
            }
            catch (Exception e)
            {
                Log.F($"Failed to start Toolbox sniffer: {e}");
                _failed = true;
            }
        }

        private async void Listen()
        {
            if (_failed) return;
            while (Enabled)
            {
                var client = _dataConnection.AcceptTcpClient();
                var resp = await ControlConnection.GetServerId();
                if (resp != 0)
                {
                    await ControlConnection.AddHooks(PacketAnalyzer.Factory.OpcodesList);
                    PacketAnalyzer.Factory.ReleaseVersion = await ControlConnection.GetReleaseVersion();
                    NewConnection?.Invoke(Game.DB.ServerDatabase.GetServer(resp));
                }
                var stream = client.GetStream();
                while (true)
                {
                    Connected = true;
                    try
                    {
                        var lenBuf = new byte[2];
                        stream.Read(lenBuf, 0, 2);
                        var len = BitConverter.ToUInt16(lenBuf, 0);
                        if (len <= 2)
                        {
                            if (!client.IsConnected())
                            {
                                client.Close();
                                Connected = false;
                                break;
                            }
                            continue;
                        }
                        var length = len - 2;
                        var dataBuf = new byte[length];

                        var progress = 0;
                        while (progress < length)
                        {
                            progress += stream.Read(dataBuf, progress, length - progress);
                        }

                        MessageReceived?.Invoke(new Message(DateTime.Now, dataBuf));

                    }
                    catch (Exception e)
                    {
                        Connected = false;
                        client.Close();
                        //Log.F($"Disconnected: {e}");
                    }
                }
            }
        }
    }
}