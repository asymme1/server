using System.Collections.Specialized;
using NetCoreServer;

namespace woke3
{
    public class GameConnection : TcpSession
    {
        private readonly GameSession session;

        public GameConnection(TcpServer server, GameSession gameSession) : base(server)
        {
            session = gameSession;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            var type = (PacketType) BitConverter.ToInt32(buffer[0..4]);
            var len = BitConverter.ToInt32(buffer[4..8]);
            var payload = buffer[8..(8 + len)];
            
            switch (type)
            {
                case PacketType.PKT_HI:
                {
                    Console.WriteLine($"{PacketType.PKT_HI} received");
                    Console.WriteLine($"UID : {Convert.ToHexString(payload[0..4])}");
                    Console.WriteLine($"Key : {Convert.ToHexString(payload[4..])}");
                    
                    // p1 go first
                    List<byte[]> list;
                    lock (session)
                    {
                        if (session.P1Connected)
                        {
                            list = new List<byte[]>
                            {
                                BitConverter.GetBytes(session.P2),
                                BitConverter.GetBytes(0)
                            };
                            Console.WriteLine($"Sent {PacketType.PKT_ID} {session.P2} as {nameof(session.P2)}");
                            session.P2Connected = true;
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
                        }   
                    }
                    SendPacket(PacketType.PKT_ID, list.SelectMany(a => a).ToArray());
                    lock (session.Matrix)
                    {
                        SendBoard(session.Matrix);
                    }
                    break;
                }
                case PacketType.PKT_SEND:
                {
                    lock (session)
                    {
                        var id = BitConverter.ToInt32(payload[0..4]);
                        if (id != session.P1 && id != session.P2)
                        {
                            // let it pass
                            break;
                        }
                        var pos = BitConverter.ToInt32(payload[4..8]);
                        var matrix = session.Matrix;
                        var n = matrix.GetLength(0);
                        int row = pos / n, col = pos % n;
                        if (row < 0 || row > n - 1 || col < 0 || row > n - 1)
                        {
                            Console.WriteLine($"Invalid {row}, {col}, rejecting");
                            SendError();
                            break;
                        }
                        if (matrix[row, col] == session.P1 || matrix[row, col] == session.P2 || matrix[row, col] == GameSession.BannedCell)
                        {
                            Console.WriteLine($"{row}, {col} already got marked, rejecting");
                            SendError();
                            break;
                        }

                        matrix[row, col] = id;
                        Console.WriteLine($"Marking {row}, {col} belong to player {id}");
                        SendBoard(session.Matrix);
                    }

                    break;
                }
            }
            base.OnReceived(buffer, offset, size);
        }

        private void SendBoard(int[,] matrix)
        {
            var n = matrix.GetLength(0);
            var blocked = new List<int>();
            for (var i = 0; i < n; i++)
            {
                for (var k = 0; k < n; k++)
                {
                    if (matrix[i, k] == GameSession.BannedCell) blocked.Add(i * n + k);
                }
            }
            var payload = new List<IEnumerable<byte>>
            {
                BitConverter.GetBytes(n),
                BitConverter.GetBytes(n),
                BitConverter.GetBytes(blocked.Count),
                BitConverter.GetBytes(5),
                blocked.SelectMany(BitConverter.GetBytes)
            };
            Send(payload.SelectMany(a => a).ToArray());
        }
        
        private void SendError()
        {
            Send(BitConverter.GetBytes((int)PacketType.PKT_ERROR));
        }

        private void SendPacket(PacketType type, byte[] payload)
        {
            var b = new List<byte[]>
            {
                BitConverter.GetBytes((int) type),
                payload
            };
            Send(b.SelectMany(a => a).ToArray());
        }
    }
}
