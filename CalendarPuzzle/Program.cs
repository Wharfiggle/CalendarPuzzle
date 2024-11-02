//Elijah Southman

using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class Program
{   
    public struct PuzzlePiece
    {
        public int id;
        public String repStr;
        public Vector2[] blocks;
        public Vector2[,] rotations;
        public float[] leftMost;
        public float[] rightMost;
        public float[] topMost;
        public float[] bottomMost;
        public PuzzlePiece(int id, Vector2[] blocks, int[] ignoredRotations)
        {
            this.id = id;
            this.repStr = "P " + id;
            this.blocks = blocks;
            
            //make all different rotations of piece's blocks
            rotations = new Vector2[8, blocks.Count()];
            for(int i = 0; i < blocks.Count(); i++)
            {
                Vector2 b = blocks[i];
                rotations[0, i] = b;
                rotations[1, i] = new Vector2(-b.X, b.Y);
                rotations[2, i] = new Vector2(b.X, -b.Y);
                rotations[3, i] = new Vector2(-b.X, -b.Y);
                rotations[4, i] = new Vector2(b.Y, b.X);
                rotations[5, i] = new Vector2(-b.Y, b.X);
                rotations[6, i] = new Vector2(b.Y, -b.X);
                rotations[7, i] = new Vector2(-b.Y, -b.X);
            }

            //ignore non-unique rotations
            Vector2[,] rots = new Vector2[8 - ignoredRotations.Count(), blocks.Count()];
            int numRots = 0;
            for(int i = 0; i < 8; i++)
            {
                if(!ignoredRotations.Contains(i))
                {
                    for(int j = 0; j < blocks.Count(); j++)
                    {
                        rots[numRots, j] = rotations[i, j];
                    }
                    numRots++;
                }
            }
            rotations = rots;

            //record farthest block values
            leftMost = new float[rotations.GetLength(0)];
            rightMost = new float[rotations.GetLength(0)];
            topMost = new float[rotations.GetLength(0)];
            bottomMost = new float[rotations.GetLength(0)];
            for(int i = 0; i < rotations.GetLength(0); i++)
            {
                leftMost[i] = 100;
                rightMost[i] = -100;
                topMost[i] = 100;
                bottomMost[i] = -100;
                for(int j = 0; j < rotations.GetLength(1); j++)
                {
                    Vector2 rot = rotations[i, j];
                    leftMost[i] = Math.Min(leftMost[i], rot.X);
                    rightMost[i] = Math.Max(rightMost[i], rot.X);
                    topMost[i] = Math.Min(topMost[i], rot.Y);
                    bottomMost[i] = Math.Max(bottomMost[i], rot.Y);
                }
            }
        }
    }

    public static PuzzlePiece[] pieces = {
        new PuzzlePiece(0, new Vector2[] { new Vector2(0,0), new Vector2(1,0),  new Vector2(2,0), new Vector2(3,0) },                       new int[] {1, 2, 3, 5, 6, 7}), //line piece
        new PuzzlePiece(1, new Vector2[] { new Vector2(0,0), new Vector2(0,1),  new Vector2(0,2), new Vector2(1,0), new Vector2(1,2) },     new int[] {2, 3, 6, 7}), // C
        new PuzzlePiece(2, new Vector2[] { new Vector2(0,0), new Vector2(0,1),  new Vector2(1,0), new Vector2(1,1), new Vector2(2,0) },     new int[] {}), //stubby piece
        new PuzzlePiece(3, new Vector2[] { new Vector2(0,0), new Vector2(1,0),  new Vector2(1,1), new Vector2(2,1), new Vector2(3,1) },     new int[] {}), //long lightning bolt
        new PuzzlePiece(4, new Vector2[] { new Vector2(0,0), new Vector2(0,1),  new Vector2(0,2), new Vector2(1,0) },                       new int[] {}), //normal L
        new PuzzlePiece(5, new Vector2[] { new Vector2(0,0), new Vector2(1,0),  new Vector2(2,0), new Vector2(3,0), new Vector2(3,1) },     new int[] {}), //long L
        new PuzzlePiece(6, new Vector2[] { new Vector2(0,0), new Vector2(0,1),  new Vector2(1,1), new Vector2(1,2) },                       new int[] {2, 3, 6, 7}), //lightning bolt
        new PuzzlePiece(7, new Vector2[] { new Vector2(0,0), new Vector2(0,1),  new Vector2(0,2), new Vector2(1,2), new Vector2(2,2) },     new int[] {4, 5, 6, 7}), //big L
        new PuzzlePiece(8, new Vector2[] { new Vector2(0,0), new Vector2(0,1),  new Vector2(1,1), new Vector2(2,1), new Vector2(2,2) },     new int[] {}), //pipe
        new PuzzlePiece(9, new Vector2[] { new Vector2(0,0), new Vector2(1,0),  new Vector2(2,0), new Vector2(1,1), new Vector2(1,2) },     new int[] {1, 3, 5, 7}) //T piece
    };

    // [y, x] for readability but everything else is [x, y]
    public static String[,] boardSlots = {
        {"jan", "feb", "mar", "apr", "may", "jun", ""},
        {"jul", "aug", "sep", "oct", "nov", "dec", ""},
        {"1", "2", "3", "4", "5", "6", "7"},
        {"8", "9", "10", "11", "12", "13", "14"},
        {"15", "16", "17", "18", "19", "20", "21"},
        {"22", "23", "24", "25", "26", "27", "28"},
        {"29", "30", "31", "sun", "mon", "tue", "wed"},
        {"", "", "", "", "thu", "fri", "sat"}
    };
    public struct Board
    {
        public int[,] slotValues;
        public Board()
        {
            //flip dimensions because boardSlots is [y, x]
            slotValues = new int[boardSlots.GetLength(1), boardSlots.GetLength(0)];
            Clear();
        }
        public Board(int[,] slotValues)
        {
            this.slotValues = slotValues.Clone() as int[,];
        }
        //return false if failed to place piece
        public bool PlacePiece(PuzzlePiece piece, int rotation, Vector2 pos)
        {
            for(int i = 0; i < piece.rotations.GetLength(1); i++)
            {
                int x = (int)(piece.rotations[rotation, i].X + pos.X);
                int y = (int)(piece.rotations[rotation, i].Y + pos.Y);
                //check if index out of bounds
                //if(x < 0 || x >= slotValues.GetLength(0))
                //    return false;
                //if(y < 0 || y >= slotValues.GetLength(1))
                //    return false;
                //check if index cant be placed in
                if(boardSlots[y, x] == "" || slotValues[x, y] != -1 || date.Contains(boardSlots[y, x]))
                    return false;
            }
            for(int i = 0; i < piece.rotations.GetLength(1); i++)
            {
                int x = (int)(piece.rotations[rotation, i].X + pos.X);
                int y = (int)(piece.rotations[rotation, i].Y + pos.Y);
                slotValues[x, y] = piece.id;
            }
            return true;
        }
        public void PrintBoard()
        {
            for(int i = 0; i < slotValues.GetLength(0) * 4 + 1; i++)
            {
                Console.Write("-");
            }
            Console.WriteLine();
            for(int y = 0; y < slotValues.GetLength(1); y++)
            {
                Console.Write("|");
                for(int x = 0; x < slotValues.GetLength(0); x++)
                {
                    String str = slotValues[x, y] != -1 ? pieces[slotValues[x, y]].repStr : boardSlots[y, x];
                    Console.Write(str);
                    for(int i = 0; i < 3 - str.Length; i++)
                        Console.Write(" ");
                    Console.Write("|");
                }
                Console.WriteLine();
                for(int i = 0; i < slotValues.GetLength(0) * 4 + 1; i++)
                {
                    Console.Write("-");
                }
                Console.WriteLine();
            }
        }
        public void Clear()
        {
            for(int i = 0; i < slotValues.GetLength(0); i++)
            {
                for(int j = 0; j < slotValues.GetLength(1); j++)
                {
                    slotValues[i, j] = -1;
                }
            }
        }
    }

    public static String[] date = {"jan", "1", "sun"};

    public static bool solved = false;

    public static Board solution;

    //recursive brute force algorithm
    //iterates through every uniquie rotation of each piece and attempts to place them at every possible location
    //if a piece is successfully placed, move on to the next piece and repeat until a piece cannot be placed
    //continue until all pieces are successfully placed
    public static void Solve(Board board, int pieceNum)
    {
        if(pieceNum >= pieces.Count())
        {
            solved = true;
            solution = board;
            return;
        }

        PuzzlePiece piece = pieces[pieceNum];
        for(int rind = 0; rind < piece.rotations.GetLength(0) && !solved; rind++)
        {
            for(int x = -(int)piece.leftMost[rind]; x < board.slotValues.GetLength(0) - piece.rightMost[rind] && !solved; x++)
            {
                for(int y = -(int)piece.topMost[rind]; y < board.slotValues.GetLength(1) - piece.bottomMost[rind] && !solved; y++)
                {
                    Board newBoard = new Board(board.slotValues);
                    bool success = newBoard.PlacePiece(piece, rind, new Vector2(x, y));
                    if(success)
                        Solve(newBoard, pieceNum + 1);
                }
            }
        }
    }

    public static void Main(string[] args)
    {
        Board board = new Board();
        Console.WriteLine("Starting board:");
        board.PrintBoard();

        //main loop
        bool loop = true;
        while(loop)
        {
            //user input and validation
            //get input all at once from user
            Console.WriteLine("\nPlease enter the date in the following format: November 2 Saturday");
            String input = Console.ReadLine();
            
            date = input.Split(null);
            if(date.Count() != 3) //check if input is correctly formatted
            {
                Console.WriteLine("Invalid input format.");
                continue;
            }
            //convert input to all lowercase and only first 3 characters
            for(int i = 0; i < 3; i++)
            {
                date[i] = date[i].Substring(0, Math.Min(3, date[i].Length)).ToLower();
            }
            Console.WriteLine("\nMonth: " + date[0]);
            Console.WriteLine("Day: " + date[1]);
            Console.WriteLine("Day of the Week: " + date[2]);
            Console.WriteLine();
            
            bool invalid = false;
            //check if month is valid
            String[] months = {"jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"};
            if(!months.Contains(date[0]))
            {
                invalid = true;
                Console.WriteLine("Invalid month.");
            }
            //check if day is valid
            try
            {
                int day = Convert.ToInt32(date[1]);
                if(day < 1 || day > 31)
                    throw new FormatException();
            }
            catch
            {
                invalid = true;
                Console.WriteLine("Invalid day.");
            }
            //check if day of week is valid
            String[] weeks = {"sun", "mon", "tue", "wed", "thu", "fri", "sat"};
            if(!weeks.Contains(date[2]))
            {
                invalid = true;
                Console.WriteLine("Invalid day of the week.");
            }
            if(invalid)
                continue;


            //start processing to find a solution
            Solve(board, 0);

            if(solved)
            {
                Console.WriteLine("\nSolution:");
                solution.PrintBoard();
                solution.Clear();
                solved = false;
            }
        }
    }
}