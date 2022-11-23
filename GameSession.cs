using System.Collections.Specialized;

namespace woke3
{
    public class GameSession
    {
        public readonly int[,] Matrix;
        public readonly int LengthToWin;
        public readonly int P1 = Random.Shared.Next();
        public readonly int P2 = Random.Shared.Next();
        public Guid P1Id { get; set; }
        public Guid P2Id { get; set; }
        public bool P1Turn { get; set; } = true;

        public bool P1Connected { get; set; } = false;
        public bool P2Connected { get; set; } = false;
        public bool MatchStarted { get; set; } = false;

        public const int BannedCell = -2;

        public GameSession()
        {
            var m = Random.Shared.Next(3, 20);
            var n = Random.Shared.Next(3, 20);
            var rnd = Random.Shared.Next(0, Math.Min(m, n) - 2);
            Matrix = new int[m, n];
            Console.WriteLine($"Initialized matrix {m}x{n} with {rnd} banned cells");


            var indexes = new OrderedDictionary();
            foreach (var p in Enumerable.Range(0, m * n - 1))
            {
                indexes.Add(p, p);
            }
            for (var i = 0; i < rnd; i++)
            {
                var indexToPick = Random.Shared.Next(0, indexes.Count - 1);
                var index = (int) indexes[indexToPick]!;
                Matrix[index / n, index % n] = BannedCell;
                indexes.RemoveAt(indexToPick);
            }

            LengthToWin = Random.Shared.Next(3, Math.Min(n, m));
        }

        public int CheckWinner()
        {
            if (MaxConsecutive(P1) == LengthToWin) return P1;
            if (MaxConsecutive(P2) == LengthToWin) return P2;

            bool draw = true;
            for (var i = 0; i < Matrix.GetLength(0); i++)
            {
                for (var j = 0; j < Matrix.GetLength(1); j++)
                {
                    if (Matrix[i, j] == 0)
                    {
                        draw = false;
                        break;
                    }
                }
            }
            
            return draw ? 0 : -1;
        }

        private int MaxConsecutive(int p)
        {
            int n = Matrix.Length / Matrix.GetLength(0);
            int m = Matrix.GetLength(0);
            int max = 0;
            
            for (int i = 0; i < m; ++i)
            {
                int count = 0;
                for (int j = 0; j < n; ++j)
                {
                    if (Matrix[i, j] == p) count++;
                    else count = 0;
                    max = Math.Max(max, count);
                }
            }

            for (int j = 0; j < n; ++j)
            {
                int count = 0;
                for (int i = 0; i < m; ++i)
                {
                    if (Matrix[i, j] == p) count++;
                    else count = 0;
                    max = Math.Max(max, count);
                }
            }

            for (int line = -m + 1; line <= n - 1; ++line)
            {
                int x = 0;
                int y = line - x;
                int count = 0;
                if (line < 0)
                {
                    y = 0;
                    x = y - line;
                }

                for (; x < m && y < n; ++x, ++y)
                {
                    if (Matrix[x, y] == p) count++;
                    else count = 0;
                    max = Math.Max(max, count);
                }
            }

            for (int line = 0; line <= m + n - 2; ++line)
            {
                int x = 0;
                int y = line - x;
                int count = 0;
                if (line >= n)
                {
                    y = n - 1;
                    x = line - y;
                }

                for (; x >= 0 && y >= 0; --x, --y)
                {
                    if (Matrix[x, y] == p) count++;
                    else count = 0;
                    max = Math.Max(max, count);
                }
            }

            return max;
        }
    }
}