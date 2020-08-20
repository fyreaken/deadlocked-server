﻿using Deadlocked.Server.Medius.Models.Packets;
using Deadlocked.Server.Medius.Models.Packets.Lobby;
using Deadlocked.Server.SCERT;
using Deadlocked.Server.SCERT.Models;
using Deadlocked.Server.SCERT.Models.Packets;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Medius.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Deadlocked.Server.Medius
{
    public class MUIS : BaseMediusComponent
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<MUIS>();

        protected override IInternalLogger Logger => _logger;
        public override string Name => "MUIS";
        public override int Port => Program.Settings.MUISPort;
        public override PS2_RSA AuthKey => Program.GlobalAuthKey;

        public MUIS()
        {
            _sessionCipher = new PS2_RC4(Utils.FromString(Program.KEY), CipherContext.RC_CLIENT_SESSION);
        }

        protected override async Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ClientObject clientObject)
        {
            // 
            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        Queue(new RT_MSG_SERVER_HELLO(), clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { Key = Utils.FromString(Program.KEY) }, clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_REQUIRE() { Contents = Utils.FromString("024802") }, clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { Key = Utils.FromString(Program.KEY) }, clientObject);
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            UNK_00 = 0,
                            UNK_01 = 0,
                            UNK_02 = 0,
                            UNK_03 = 0,
                            UNK_04 = 0,
                            UNK_05 = 0,
                            UNK_06 = 0x0001,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                        }, clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_TCP clientConnectReadyTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ARG1 = 0x0001 }, clientObject);
                        Queue(new RT_MSG_SERVER_ECHO(), clientObject);
                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientObject);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        ProcessMediusMessage(clientAppToServer.Message, clientChannel, clientObject);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        await clientChannel.DisconnectAsync();
                        break;
                    }
                default:
                    {
                        Logger.Warn($"{Name} UNHANDLED MESSAGE: {message}");
                        break;
                    }
            }

            return;
        }

        protected virtual void ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ClientObject clientObject)
        {
            if (message == null)
                return;

            switch (message)
            {
                case MediusGetUniverseInformationRequest getUniverseInfo:
                    {
                        // 
                        Queue(new RT_MSG_SERVER_APP() { Message = new MediusUniverseVariableSvoURLResponse() { Result = 1 } }, clientObject);

                        // 
                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusUniverseVariableInformationResponse()
                            {
                                MessageID = getUniverseInfo.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                InfoFilter = getUniverseInfo.InfoType,
                                UniverseID = 1,
                                ExtendedInfo = "",
                                UniverseName = "Ratchet: Deadlocked Production",
                                UniverseDescription = "Ratchet: Deadlocked Production",
                                DNS = "ratchetdl-prod.pdonline.scea.com",
                                Port = Program.AuthenticationServer.Port,
                                EndOfList = true
                            }
                        }, clientObject);

                        break;
                    }
                default:
                    {
                        Logger.Warn($"{Name} UNHANDLED MEDIUS MESSAGE: {message}");
                        break;
                    }
            }
        }
    }
}
