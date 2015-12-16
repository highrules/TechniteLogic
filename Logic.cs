using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logging;
using Math3D;

namespace TechniteLogic
{
	public class Logic
	{
		public static class Helper
		{ 
			static List<KeyValuePair<int, Grid.RelativeCell>> options = new List<KeyValuePair<int, Grid.RelativeCell>>();
			static Random random = new Random();

			public const int NotAChoice = 0;

			/// <summary>
			/// Evaluates all possible neighbor cells. The return values of <paramref name="f"/> are used as probability multipliers 
			/// to chose a random a option.
			/// Currently not thread-safe
			/// </summary>
			/// <param name="location">Location to evaluate the neighborhood of</param>
			/// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
			/// <returns>Chocen relative cell, or Grid.RelativeCell.Invalid if none was found.</returns>
			public static Grid.RelativeCell EvaluateChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
			{
				options.Clear();
				int total = 0;
				foreach (var n in location.GetRelativeNeighbors())
				{
					Grid.CellID cellLocation = location + n;
					int q = f(n, cellLocation);
					if (q > 0)
					{
						total += q;
						options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
					}
				}
				if (total == 0)
					return Grid.RelativeCell.Invalid;
				if (options.Count == 1)
					return options[0].Value;
				int c = random.Next(total);
				foreach (var o in options)
				{
					if (c <= o.Key)
						return o.Value;
					c -= o.Key;
				}
				Out.Log(Significance.ProgramFatal, "Logic error");
				return Grid.RelativeCell.Invalid;
			}

            //////////////////////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Evaluated all targets matter values and return max matter target.
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell EvaluateMaxMatterGnawChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            {
                options.Clear();
                foreach (var n in location.GetRelativeNeighbors())
                {
                    Grid.CellID cellLocation = location + n;
                    int q = f(n, cellLocation);
                    if (q > 0)
                    {
                        options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
                    }
                }
                if (options.Count == 0)
                    return Grid.RelativeCell.Invalid;
                if (options.Count == 1)
                    return options[0].Value;
                int maxYield = 0;
                Grid.RelativeCell maxOption = Grid.RelativeCell.Invalid;
                foreach (var o in options)
                {
                    if (maxYield <= o.Key)
                    {
                        maxYield = o.Key;
                        maxOption = o.Value;
                    }
                }
                return maxOption;
            }

            /// <summary>
            /// Returns upper neighbor choice
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell EvaluateUpperChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            {
                options.Clear();
                int total = 0;
                foreach (var n in location.GetRelativeUpperNeighbors())  //GetRelativeUpperNeighbors effizienter mit delta übergabe
                {
                    Grid.CellID cellLocation = location + n;
                    if (cellLocation.Layer >= location.Layer)
                    {
                        int q = f(n, cellLocation);
                        if (q > 0)
                        {
                            total += q;
                            options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
                        }
                    }
                }
                if (total == 0)
                    return Grid.RelativeCell.Invalid;
                if (options.Count == 1)
                    return options[0].Value;
                int c = random.Next(total);
                return options[c].Value;
            }

            /// <summary>
            /// Returns lower neighbor choice
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f"></param>
            /// <returns></returns>
            public static Grid.RelativeCell EvaluateLowerChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            {
                options.Clear();
                int total = 0;
                foreach (var n in location.GetRelativeLowerNeighbors())  //GetRelativeUpperNeighbors effizienter
                {
                    Grid.CellID cellLocation = location + n;
                    if (cellLocation.Layer <= location.Layer)
                    {
                        int q = f(n, cellLocation);
                        if (q > 0)
                        {
                            total += q;
                            options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
                        }
                    }
                }
                if (total == 0)
                    return Grid.RelativeCell.Invalid;
                if (options.Count == 1)
                    return options[0].Value;
                int c = random.Next(total);
                return options[c].Value;
            }

            public static Grid.RelativeCell EvaluateHorizontalChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            {
                options.Clear();
                int total = 0;
                foreach (var n in location.GetRelativeHorizontalNeighbors())  //GetRelativeUpperNeighbors effizienter mit delta übergeben
                {
                    Grid.CellID cellLocation = location + n;

                    int q = f(n, cellLocation);
                    if (q > 0)
                    {
                        total += q;
                        options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
                    }

                }
                if (total == 0)
                    return Grid.RelativeCell.Invalid;
                if (options.Count == 1)
                    return options[0].Value;
                int c = random.Next(total);
                return options[c].Value;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

			/// <summary>
			/// Determines a feasible, possibly ideal neighbor technite target, based on a given evaluation function
			/// </summary>
			/// <param name="location"></param>
			/// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
			/// <returns>Chocen relative cell, or Grid.RelativeCell.Invalid if none was found.</returns>
			public static Grid.RelativeCell EvaluateNeighborTechnites(Grid.CellID location, Func<Grid.RelativeCell, Technite, int> f)
			{
				return EvaluateChoices(location, (relative, cell) =>
				{
					Grid.Content content = Grid.World.GetCell(cell).content;
					if (content != Grid.Content.Technite)
						return NotAChoice;
					Technite other = Technite.Find(cell);
					if (other == null)
					{
						Out.Log(Significance.Unusual, "Located neighboring technite in " + cell + ", but cannot find reference to class instance");
						return NotAChoice;
					}
					return f(relative,other);
				}
				);			
			}

			/// <summary>
			/// Determines a feasible, possibly ideal technite neighbor cell that is at the very least on the same height level.
			/// Higher and/or lit neighbor technites are favored
			/// </summary>
			/// <param name="location"></param>
			/// <returns></returns>
			public static Grid.RelativeCell GetLitOrUpperTechnite(Grid.CellID location)
			{
				return EvaluateNeighborTechnites(location, (relative, technite) =>
				{
					int rs = 0;
					if (technite.Status.Lit)
						rs++;
					rs += relative.HeightDelta;
					return rs;
				});
			}

			/// <summary>
			/// Determines a feasible, possibly ideal technite neighbor cell that is at most on the same height level.
			/// Lower and/or unlit neighbor technites are favored
			/// </summary>
			/// <param name="location"></param>
			/// <returns></returns>
			public static Grid.RelativeCell GetUnlitOrLowerTechnite(Grid.CellID location)
			{
				return EvaluateNeighborTechnites(location, (relative, technite) =>
				{
					int rs = 1;
					if (technite.Status.Lit)
						rs--;
					rs -= relative.HeightDelta;
					return rs;
				}
				);
			}

			/// <summary>
			/// Determines a food source in the neighborhood of the specified location
			/// </summary>
			/// <param name="location"></param>
			/// <returns></returns>
			public static Grid.RelativeCell GetFoodChoice(Grid.CellID location)
			{
				return EvaluateChoices(location, (relative, cell) =>
				{
					Grid.Content content = Grid.World.GetCell(cell).content;
					int yield = Technite.MatterYield[(int)content];	//zero is zero, no exceptions
					if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
						return yield;
					return NotAChoice;
				}
				);
			}

            /// <summary>
            /// Determines a gnaw source in the neighborhood of the specified location
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell GetMaxMatterGnawChoice(Grid.CellID location)
            {
                return EvaluateMaxMatterGnawChoices(location, (relative, cell) =>
                {
                    Grid.Content content = Grid.World.GetCell(cell).content;
                    int yield = Technite.MatterYield[(int)content];	//zero is zero, no exceptions
                    // verhindert das techniten über andere techniten bauen und damit lit = false werden könnte
                    if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                        return yield;
                    return NotAChoice;
                }
                );
            }

            /// <summary>
            /// Determines a cell of the upper neighbor
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell GetUpperSplitTarget(Grid.CellID location)
            {
                return EvaluateUpperChoices(location, (relative, cell) =>
                {
                    if (Grid.World.GetCell(cell).content == Grid.Content.Clear)
                    {
                        if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                        {
                            if (Technite.EnoughSupportHere(cell))
                                return 1;
                        }
                    }
                    return NotAChoice;
                }
                );
            }

            /// <summary>
            /// Determines a cell of the lower neighbor
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell GetLowerSplitTarget(Grid.CellID location)
            {
                return EvaluateLowerChoices(location, (relative, cell) =>
                {
                    if (Grid.World.GetCell(cell).content == Grid.Content.Clear)
                    {
                        if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                        {
                            if (Technite.EnoughSupportHere(cell))
                                return 1;
                        }
                    }
                    return NotAChoice;
                }
                );
            }

			/// <summary>
			/// Determines a feasible neighborhood cell that can work as a replication destination.
			/// </summary>
			/// <param name="location"></param>
			/// <returns></returns>
			public static Grid.RelativeCell GetSplitTarget(Grid.CellID location)
			{
				return EvaluateChoices(location, (relative, cell) =>
				{
					Grid.Content content = Grid.World.GetCell(cell).content;
					int rs = 100;
					if (content != Grid.Content.Clear && content != Grid.Content.Water)
						rs -= 90;
					if (Grid.World.GetCell(cell.TopNeighbor).content == Grid.Content.Technite)
						return NotAChoice;  //probably a bad idea to split beneath technite

					if (Technite.EnoughSupportHere(cell))
						return relative.HeightDelta + rs;

					return NotAChoice;
				}
				);
			}

		}




		private static Random random = new Random();

		/// <summary>
		/// Central logic method. Invoked once per round to determine the next task for each technite.
		/// </summary>
		public static void ProcessTechnites()
		{
			Out.Log(Significance.Common, "ProcessTechnites()");

			//let's do some simple processing

			//			bool slightlyVerbose = Technite.All.Count() < 20;

			
		}
	}
}