using System.Collections.Generic;
using UnityEngine;

namespace tmkoc.threeinarow
{
    /// <summary>Pure board logic: the 8 winning lines, win / draw checks, and the pet's move choice per difficulty.</summary>
    public static class BotAI
    {
        public static readonly int[][] Lines =
        {
            new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },   // rows
            new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },   // columns
            new[] { 0, 4, 8 }, new[] { 2, 4, 6 }                       // diagonals
        };

        /// <returns>The winning line (3 indices) for <paramref name="mark"/>, or null.</returns>
        public static int[] FindWin(Mark[] board, Mark mark)
        {
            foreach (var l in Lines)
                if (board[l[0]] == mark && board[l[1]] == mark && board[l[2]] == mark) return l;
            return null;
        }

        public static bool IsFull(Mark[] board)
        {
            foreach (var m in board) if (m == Mark.None) return false;
            return true;
        }

        /// <summary>Empty square that would complete a line for <paramref name="mark"/>, or -1.</summary>
        public static int CompletingSquare(Mark[] board, Mark mark)
        {
            foreach (var l in Lines)
            {
                int count = 0, empty = -1;
                foreach (int i in l)
                {
                    if (board[i] == mark) count++;
                    else if (board[i] == Mark.None) empty = i;
                    else { count = -9; break; }
                }
                if (count == 2 && empty >= 0) return empty;
            }
            return -1;
        }

        public static int RandomEmpty(Mark[] board)
        {
            var empty = new List<int>();
            for (int i = 0; i < board.Length; i++) if (board[i] == Mark.None) empty.Add(i);
            return empty.Count == 0 ? -1 : empty[Random.Range(0, empty.Count)];
        }

        /// <summary>The pet's move for the level's difficulty.</summary>
        public static int ChoosePetMove(Mark[] board, ThreeLevelConfig cfg)
        {
            if (Random.value < cfg.randomMove) return RandomEmpty(board);

            if (Random.value < cfg.takeWin)
            {
                int win = CompletingSquare(board, Mark.Pet);
                if (win >= 0) return win;
            }
            if (Random.value < cfg.block)
            {
                int stop = CompletingSquare(board, Mark.Tapu);
                if (stop >= 0) return stop;
            }

            // centre, then corners, then anything
            if (board[4] == Mark.None) return 4;
            int[] corners = { 0, 2, 6, 8 };
            var free = new List<int>();
            foreach (int c in corners) if (board[c] == Mark.None) free.Add(c);
            if (free.Count > 0) return free[Random.Range(0, free.Count)];
            return RandomEmpty(board);
        }
    }
}
