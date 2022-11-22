using System.Collections.Specialized;

namespace woke3
{
    public class GameSession
    {
        public readonly int[,] Matrix;
        public readonly int P1 = Random.Shared.Next();
        public readonly int P2 = Random.Shared.Next();
        public Guid P1Id { get; set; }
        public Guid P2Id { get; set; }

        public bool P1Connected { get; set; } = false;
        public bool P2Connected { get; set; } = false;
        public bool MatchStarted { get; set; } = false;

        public const int BannedCell = -2;

        public GameSession()
        {
            var n = Random.Shared.Next(10, 20);
            var rnd = n - 2;
            Matrix = new int[n, n];


            var indexes = new OrderedDictionary();
            foreach (var p in Enumerable.Range(0, n * n - 1))
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
        }
    }
}