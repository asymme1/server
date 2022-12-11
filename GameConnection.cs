using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using NetCoreServer;
using Newtonsoft.Json;

namespace woke3
{
    public class GameConnection : TcpSession
    {
        private readonly GameSession session;
        private readonly GameServer server;

        public GameConnection(GameServer server, GameSession session) : base(server)
        {
            this.session = session;
            this.server = server;
        }
        
        protected override void OnConnected()
        {
            if (server.ConnectedSessions > 2)
            {
                Console.WriteLine("Detect more than 2 clients, disconnecting");
                SendPacket(PacketType.PKT_END, new byte[] {0}, true);
                server.UpdateClient.SendEndMatchMessage(session.MatchId);
                Disconnect();
                server.Dispose();
            }
        }
        
        protected override void OnDisconnected()
        {
            lock (session)
            {
                if (session.MatchState == MatchState.Started)
                {
                    session.MatchState = MatchState.End;
                    Console.WriteLine(session.P1Id == Id ? "Player 1 disconnected. Player 2 wins" : "Player 2 disconnected. Player 1 wins");
                    if (session.P1Id == Id) session.P1Connected = false;
                    else session.P2Connected = false;
                    SendPacket(PacketType.PKT_END, BitConverter.GetBytes(session.P1Id == Id ? session.P2 : session.P1), true);
                    server.UpdateClient.SendMatchUpdate(server.Session.MatchId, server.Session.GetInfo());
                    server.UpdateClient.SendEndMatchMessage(session.MatchId);
                    Disconnect();
                    server.UpdateClient.Close();
                    server.Dispose();
                }
                else
                {
                    if (session.P1Id == Id)
                    {
                        session.P1Connected = false;
                        session.RegisteredUid = -1;
                    }
                    else if (session.P2Id == Id)
                    {
                        session.P2Connected = false;
                    }
                }
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                OnReceivedInternal(buffer, offset, size);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void OnReceivedInternal(byte[] buffer, long offset, long size)
        {
            var type = (PacketType) BitConverter.ToInt32(buffer[0..4]);
            var len = BitConverter.ToInt32(buffer[4..8]);
            var payload = buffer[8..(8 + len)];

            switch (type)
            {
                case PacketType.PKT_HI:
                {
                    Console.WriteLine($"{PacketType.PKT_HI} received");
                    var uid = BitConverter.ToInt32(payload[0..4]);
                    var keymatch = Encoding.UTF8.GetString(payload[4..]);
                    Console.WriteLine($"UID : {uid}");
                    Console.WriteLine($"Key : {keymatch}");
                    Console.WriteLine($"Current session expected uid and key : {session.Uid1} {session.Uid2} {session.Keymatch}");

                    // p1 go first
                    lock (session)
                    {
                        if (keymatch != session.Keymatch)
                        {
                            Disconnect();
                            break;
                        };
                        if (uid != session.Uid1 && uid != session.Uid2)
                        {
                            Disconnect();
                            break;
                        }
                        List<byte[]> list;
                        if (session.P1Connected)
                        {
                            if (uid == session.RegisteredUid)
                            {
                                Disconnect();
                                break;
                            }
                            list = new List<byte[]>
                            {
                                BitConverter.GetBytes(session.P2),
                                BitConverter.GetBytes(0)
                            };
                            Console.WriteLine($"Sent {PacketType.PKT_ID} {session.P2} as {nameof(session.P2)}");
                            session.P2Connected = true;
                            session.P2Id = Id;
                        }
                        else
                        {
                            list = new List<byte[]>
                            {
                                BitConverter.GetBytes(session.P1),
                                BitConverter.GetBytes(1)
                            };
                            Console.WriteLine($"Sent {PacketType.PKT_ID} {session.P1} as {nameof(session.P1)}");
                            session.P1Connected = true;
                            session.P1Id = Id;
                            session.RegisteredUid = uid;
                        }
                        SendPacket(PacketType.PKT_ID, list.SelectMany(a => a).ToArray());

                        if (session.P1Connected & session.P2Connected)
                        {
                            session.MatchState = MatchState.Started;
                            Console.WriteLine("Both players connected, starting match");
                            server.UpdateClient.SendStartMatchMessage(session.MatchId);
                            // sleep thread for 10 second
                            new Thread(() =>
                            {
                                Thread.Sleep(5000);
                                lock (session)
                                {
                                    Console.WriteLine("Send board to both players");
                                    SendBoard(session.Matrix);
                                    
                                }
                            }).Start();
                        }
                    }
                    break;
                }
                case PacketType.PKT_SEND:
                {
                    Console.WriteLine($"{PacketType.PKT_SEND} received");
                    lock (session)
                    {
                        Console.WriteLine("Payload : {0}", string.Join(" ", payload.Select(a => a.ToString())));
                        var id = BitConverter.ToUInt32(payload[0..4]);
                        Console.WriteLine(id);
                        if (id != session.P1 && id != session.P2)
                        {
                            // let it pass
                            break;
                        }
                        
                        if ((id == session.P1 && !session.P1Turn) || (id == session.P2 && session.P1Turn))
                        {
                            Console.WriteLine("Wrong turn !!! Ignore it");
                            break;
                        }

                        var pos = BitConverter.ToInt32(payload[4..8]);
                        var matrix = session.Matrix;
                        var m = matrix.GetLength(0);
                        var n = matrix.Length / matrix.GetLength(0);
                        int row = pos / n, col = pos % n;
                        if (row < 0 || row > m - 1 || col < 0 || col > n - 1)
                        {
                            Console.WriteLine($"Invalid {row}, {col}, rejecting");
                            SendError();
                            break;
                        }

                        if (matrix[row, col] == session.P1 || matrix[row, col] == session.P2 ||
                            matrix[row, col] == GameSession.BannedCell)
                        {
                            Console.WriteLine($"{row}, {col} already got marked, rejecting");
                            SendError();
                            break;
                        }

                        matrix[row, col] = (int)id;
                        int winner = session.CheckWinner();
                        if (winner != -1)
                        {
                            session.MatchState = MatchState.End;
                            Console.WriteLine($"Player {winner} wins");
                            SendPacket(PacketType.PKT_END, BitConverter.GetBytes(winner), true);
                            //session.MainServer?.UpdateClient?.SendMatchUpdate(session.MatchId, session.MainServer?.RequestMatchInfo(session.MatchId));
                            server.UpdateClient.SendMatchUpdate(server.Session.MatchId, server.Session.GetInfo());
                            server.UpdateClient.SendEndMatchMessage(session.MatchId);
                            //server.Session = new GameSession();
                            Disconnect();
                            server.UpdateClient.Close();
                            server.Dispose();
                        }
                        else
                        {
                            Console.WriteLine($"Marking {row}, {col} belong to player {id}");
                            SendReceive(pos);
                        }
                    }

                    break;
                }
                case PacketType.PKT_SPECIAL_RESET:
                {
                    Console.WriteLine("Resetting session");
                    //server.Session = new GameSession();
                    Disconnect();
                    break;
                }
            }
            base.OnReceived(buffer, offset, size);
        }

        private void SendBoard(int[,] matrix)
        {
            int consecutive = session.LengthToWin;
            var n = matrix.Length / matrix.GetLength(0);
            var m = matrix.GetLength(0);
            var blocked = new List<int>();
            Console.WriteLine(m + " " + n);
            for (var i = 0; i < m; i++)
            {
                for (var k = 0; k < n; k++)
                {
                    if (matrix[i, k] == GameSession.BannedCell) blocked.Add(i * n + k);
                }
            }
            var payload = new List<IEnumerable<byte>>
            {
                BitConverter.GetBytes(n),
                BitConverter.GetBytes(m),
                BitConverter.GetBytes(blocked.Count),
                BitConverter.GetBytes(consecutive),
                blocked.SelectMany(BitConverter.GetBytes)
            };
            SendPacket(PacketType.PKT_BOARD, payload.SelectMany(a => a).ToArray(), true);
        }

        private void SendReceive(int pos)
        {
            var b = new List<byte[]>
            {
                BitConverter.GetBytes((int) PacketType.PKT_RECEIVE),
                BitConverter.GetBytes(4),
                BitConverter.GetBytes(pos)
            };
            server.FindSession(session.P1Turn ? session.P2Id : session.P1Id).Send(b.SelectMany(a => a).ToArray());
            session.P1Turn = !session.P1Turn;
        }
        
        private void SendError()
        {
            Send(BitConverter.GetBytes((int)PacketType.PKT_ERROR));
        }

        private void SendPacket(PacketType type, byte[] payload, bool multicast = false)
        {
            Console.WriteLine($"Sending {type} with payload of length {payload.Length}");
            Console.WriteLine("Payload : {0}",
                type != PacketType.PKT_BOARD
                    ? BitConverter.ToString(payload).Replace("-", " ")
                    : string.Join(" ", payload.Select(a => a.ToString())));
            Console.WriteLine("Payload {0}", string.Join(" ", payload.Select(a => a.ToString())));
            var b = new List<byte[]>
            {
                BitConverter.GetBytes((int) type),
                BitConverter.GetBytes(payload.Length),
                payload
            };
            var final = b.SelectMany(a => a).ToArray();
            if (!multicast)
            {
                Send(final);
            }
            else
            {
                lock (session)
                {
                    server.Multicast(final);
                }
            }
        }
    }
}
