﻿using System;
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
			/// Searches for the neighbouring technite with the least amount of matter
            /// von uns
			/// </summary>
			/// <param name="location">Location to evaluate the neighborhood of</param>
			/// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
			/// <returns></returns>
			public static Grid.RelativeCell EvaluateMinResourceTransferChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            {
                options.Clear();
                foreach (var n in location.GetRelativeNeighbors())
                {
                    Grid.CellID cellLocation = location + n;
                    if (Grid.World.GetCell(cellLocation).content == Grid.Content.Technite)
                    {
                        int q = f(n, cellLocation);  // q = current matter of technite n
                        options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
                    }
                    
                }
                if (options.Count == 0)
                    return Grid.RelativeCell.Invalid;
                if (options.Count == 1)
                    return options[0].Value;
                //int c = random.Next(total);
                int minResource = byte.MaxValue;
                Grid.RelativeCell minOption = Grid.RelativeCell.Invalid;
                foreach (var o in options)
                {
                    if (minResource >= o.Key)   // make a new options field with all minResource and choose a random option
                    {
                        minResource = o.Key;
                        minOption = o.Value;
                    }
                }
                return minOption;
                //foreach (var o in options)
                //{
                //    if (c <= o.Key)
                //        return o.Value;
                //    c -= o.Key;
                //}
                //Out.Log(Significance.ProgramFatal, "Logic error");
                //return Grid.RelativeCell.Invalid;
            }

            /// <summary>
			/// Searches for the neighbouring technite with the least amount of energy
            /// von uns
			/// </summary>
			/// <param name="location">Location to evaluate the neighborhood of</param>
			/// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
			/// <returns></returns>
			//public static Grid.RelativeCell EvaluateMinEnergyTransferChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            //{
            //    options.Clear();
            //    foreach (var n in location.GetRelativeNeighbors())
            //    {
            //        Grid.CellID cellLocation = location + n;
            //        if (Grid.World.GetCell(cellLocation).content == Grid.Content.Technite)
            //        {
            //            int q = f(n, cellLocation);  // q = current Energy of technite n
            //            options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
            //        }
            //
            //    }
            //    if (options.Count == 0)
            //        return Grid.RelativeCell.Invalid;
            //    if (options.Count == 1)
            //        return options[0].Value;
            //    //int c = random.Next(total);
            //    int minResource = byte.MaxValue;
            //    Grid.RelativeCell minOption = Grid.RelativeCell.Invalid;
            //    foreach (var o in options)
            //    {
            //        if (minResource >= o.Key)   // make a new options field with all minResource and choose a random option
            //        {
            //            minResource = o.Key;
            //            minOption = o.Value;
            //        }
            //    }
            //    return minOption;
                //foreach (var o in options)
                //{
                //    if (c <= o.Key)
                //        return o.Value;
                //    c -= o.Key;
                //}
                //Out.Log(Significance.ProgramFatal, "Logic error");
                //return Grid.RelativeCell.Invalid;
            //}

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

            ///// <summary>
            ///// Returns upper neighbor choice
            ///// von uns
            ///// </summary>
            ///// <param name="location"></param>
            ///// <returns></returns>
            //public static Grid.RelativeCell EvaluateUpperChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            //{
            //    options.Clear();
            //    int total = 0;
            //    foreach (var n in location.GetRelativeDeltaNeighbors(1))  //GetRelativeUpperNeighbors effizienter mit delta übergabe
            //    {
            //        Grid.CellID cellLocation = location + n;
            //        if (cellLocation.Layer >= location.Layer)
            //        {
            //            int q = f(n, cellLocation);
            //            if (q > 0)
            //            {
            //                total += q;
            //                options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
            //            }
            //        }
            //    }
            //    if (total == 0)
            //        return Grid.RelativeCell.Invalid;
            //    if (options.Count == 1)
            //        return options[0].Value;
            //    int c = random.Next(total);
            //    return options[c].Value;
            //}

            ///// <summary>
            ///// Returns lower neighbor choice
            ///// von uns
            ///// </summary>
            ///// <param name="location"></param>
            ///// <param name="f"></param>
            ///// <returns></returns>
            //public static Grid.RelativeCell EvaluateLowerChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            //{
            //    options.Clear();
            //    int total = 0;
            //    foreach (var n in location.GetRelativeDeltaNeighbors(-1))  //GetRelativeUpperNeighbors effizienter
            //    {
            //        Grid.CellID cellLocation = location + n;
            //        if (cellLocation.Layer <= location.Layer)
            //        {
            //            int q = f(n, cellLocation);
            //            if (q > 0)
            //            {
            //                total += q;
            //                options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
            //            }
            //        }
            //    }
            //    if (total == 0)
            //        return Grid.RelativeCell.Invalid;
            //    if (options.Count == 1)
            //        return options[0].Value;
            //    int c = random.Next(total);
            //    return options[c].Value;
            //}

            //public static Grid.RelativeCell EvaluateHorizontalChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            //{
            //    options.Clear();
            //    int total = 0;
            //    foreach (var n in location.GetRelativeDeltaNeighbors(0))  //GetRelativeUpperNeighbors effizienter mit delta übergeben
            //    {
            //        Grid.CellID cellLocation = location + n;

            //        int q = f(n, cellLocation);
            //        if (q > 0)
            //        {
            //            total += q;
            //            options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
            //        }

            //    }
            //    if (total == 0)
            //        return Grid.RelativeCell.Invalid;
            //    if (options.Count == 1)
            //        return options[0].Value;
            //    int c = random.Next(total);
            //    return options[c].Value;
            //}
            /// <summary>
            /// Returns a random cell in the given delta
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
            /// <returns>Chocen relative cell, or Grid.RelativeCell.Invalid if none was found.</returns>
            public static Grid.RelativeCell EvaluateDeltaChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f, positions delta)
            {
                options.Clear();
                int total = 0;

                
                foreach (var n in location.GetRelativeDeltaNeighbors((int)delta))  //GetRelativeUpperNeighbors effizienter mit delta übergeben
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

                /// For Technites that split to cells with matter
                //int maxYield = 0;
                //Grid.RelativeCell maxOption = Grid.RelativeCell.Invalid;
                //foreach (var o in options)
                //{
                //    if (maxYield <= o.Key)
                //    {
                //        maxYield = o.Key;
                //        maxOption = o.Value;
                //    }
                //}
                //return maxOption;
            }
            /// <summary>
            /// Determines a feasible, possibly ideal neighbor technite target, based on a given evaluation function
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
            /// <returns>Chocen relative cell, or Grid.RelativeCell.Invalid if none was found.</returns>
            public static Grid.RelativeCell EvaluateMinMatterNeighborTechnites(Grid.CellID location, Func<Grid.RelativeCell, Technite, int> f)
            {
                return EvaluateMinResourceTransferChoices(location, (relative, cell) =>
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
                    return f(relative, other);
                }
                );
            }

            /// <summary>
            /// Determines a feasible, possibly ideal neighbor technite target, based on a given evaluation function
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
            /// <returns>Chocen relative cell, or Grid.RelativeCell.Invalid if none was found.</returns>
            public static Grid.RelativeCell EvaluateMinEnergyNeighborTechnites(Grid.CellID location, Func<Grid.RelativeCell, Technite, int> f)
            {
                return EvaluateMinResourceTransferChoices(location, (relative, cell) =>
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
                    return f(relative, other);
                }
                );
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
                    //int energyYield = Technite.EnergyYieldAtLayer[location.Layer];
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
            public static Grid.RelativeCell GetMatterNeighbourTechnite(Grid.CellID location)
            {
                return EvaluateMinMatterNeighborTechnites(location, (relative, technite) =>
                {
                    //int rs = 0;
                    //if (technite.Status.Lit)
                    //    rs++;
                    //rs += relative.HeightDelta;
                    //return rs;
                    //Grid.Content content = Grid.World.GetCell(technite.Location).content;
                    int curMatter = technite.CurrentResources.Matter;
                    // verhindert das techniten über andere techniten bauen und damit lit = false werden könnte
                    //if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                        return curMatter;
                    //int energyYield = Technite.EnergyYieldAtLayer[location.Layer];
                    //return NotAChoice;
                });
            }

            /// <summary>
            /// Determines a cell of the upper neighbor
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell GetEnergyNeighbourTechnite(Grid.CellID location)
            {
                return EvaluateMinEnergyNeighborTechnites(location, (relative, technite) =>
                {
                    //int rs = 0;
                    //if (technite.Status.Lit)
                    //    rs++;
                    //rs += relative.HeightDelta;
                    //return rs;
                    //Grid.Content content = Grid.World.GetCell(technite.Location).content;
                    //int curMatter = technite.CurrentResources.Matter;
                    // verhindert das techniten über andere techniten bauen und damit lit = false werden könnte
                    //if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                    int curEnergy = technite.CurrentResources.Energy;
                    return curEnergy;
                    //return NotAChoice;
                });
            }

            ///// <summary>
            ///// Determines a cell of the upper neighbor
            ///// von uns
            ///// </summary>
            ///// <param name="location"></param>
            ///// <returns></returns>
            //public static Grid.RelativeCell GetUpperSplitTarget(Grid.CellID location)
            //{
            //    return EvaluateDeltaChoices(location, (relative, cell) =>
            //    {
            //        if (Grid.World.GetCell(cell).content == Grid.Content.Clear || Grid.World.GetCell(cell).content == Grid.Content.Water)
            //        {
            //            if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
            //            {
            //                if (Technite.EnoughSupportHere(cell))
            //                    return 1;
            //            }
            //        }
            //        return NotAChoice;
            //    }
            //    , 1);
            //}

            ///// <summary>
            ///// Determines a cell of a horizontal neighbor
            ///// von uns
            ///// </summary>
            ///// <param name="location"></param>
            ///// <returns></returns>
            //public static Grid.RelativeCell GetHorizontalSplitTarget(Grid.CellID location)
            //{
            //    return EvaluateDeltaChoices(location, (relative, cell) =>
            //    {
            //        if (Grid.World.GetCell(cell).content == Grid.Content.Clear || Grid.World.GetCell(cell).content == Grid.Content.Water)
            //        {
            //            if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
            //            {
            //                if (Technite.EnoughSupportHere(cell))
            //                    return 1;
            //            }
            //        }
            //        return NotAChoice;
            //    }
            //    , 0);
            //}

            ///// <summary>
            ///// Determines a cell of the lower neighbor
            ///// von uns
            ///// </summary>
            ///// <param name="location"></param>
            ///// <returns></returns>
            //public static Grid.RelativeCell GetLowerSplitTarget(Grid.CellID location)
            //{
            //    return EvaluateDeltaChoices(location, (relative, cell) =>
            //    {
            //        if (Grid.World.GetCell(cell).content == Grid.Content.Clear || Grid.World.GetCell(cell).content == Grid.Content.Water)
            //        {
            //            if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
            //            {
            //                if (Technite.EnoughSupportHere(cell))
            //                    return 1;
            //            }
            //        }
            //        return NotAChoice;
            //    }
            //    , -1);
            //}

            /// <summary>
            /// Determines a cell of the specified delta position
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell GetDeltaSplitTarget(Grid.CellID location, positions delta)
            {
                return EvaluateDeltaChoices(location, (relative, cell) =>
                {
                    if (Grid.World.GetCell(cell).content == Grid.Content.Clear || Grid.World.GetCell(cell).content == Grid.Content.Water)
                        // check for different faction e.g. enemy technites
                    {
                        if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                        {
                            if (Technite.EnoughSupportHere(cell))
                                return 1;
                        }
                    }
                    return NotAChoice;
                }
                , delta);
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

        public static int SetGamePhase(Technite t)                 // private public?
        {
            int gamePhase = 4;

            if (t.CanSplit)
            {
                gamePhase = 1;
            }
            else if(t.CurrentResources.Matter == 255)
            {
                gamePhase = 2;
            }
            else if (t.CanGnawAt)
            {
                gamePhase = 0;
            }
            else if (t.CurrentResources.Matter >= t.CurrentResources.Energy)
            {
                gamePhase = 2;
            }
            else
            {
                gamePhase = 3;
            }

            if (t.Status.TTL <= 2)
                gamePhase = 4;
             
            return gamePhase;
        }

        public enum positions
        {
            upper = 1,
            lower = -1,
            horizontal = 0,
        }

		/// <summary>
		/// Central logic method. Invoked once per round to determine the next task for each technite.
		/// </summary>
		public static void ProcessTechnites()
		{
			Out.Log(Significance.Common, "ProcessTechnites()");
            int gamePhase;
            Grid.RelativeCell target;
            Grid.Content content;
            Grid.CellID absoluteTarget;
            foreach (Technite t in Technite.All)
            {
                gamePhase = SetGamePhase(t);
                switch (gamePhase)
                {
                        
                    case 0: // nagen
                        if (t.CanGnawAt)
                        {
                            if (t.CurrentResources.Energy <= 5) //splitmatter available, wait for energy
                            {
                                goto case 99;
                            }
                            target = Helper.GetMaxMatterGnawChoice(t.Location);

                            if (target != Grid.RelativeCell.Invalid)
                            {

                                absoluteTarget = t.Location + target;
                                content = Grid.World.GetCell(absoluteTarget).content;
                                int yield = Technite.MatterYield[(int)content];	//zero is zero, no exceptions

                                if (yield > 1)
                                {
                                    // nage nicht an zellen, die keine Matter liefern
                                    t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                                    break;
                                }
                                else if (yield == 1)
                                {
                                    // letzter Ausweg, wenn Spawnpunkt im Wasser und keine Matter verfügbar ist
                                    t.SetNextTask(Technite.Task.ConsumeSurroundingCell, target);
                                    break;
                                }
                            }
                        }
                        goto case 1;
                    case 1: // split
                        if (t.CanSplit)
                        {
                            target = Helper.GetDeltaSplitTarget(t.Location, positions.horizontal);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GrowTo, target);
                                break;
                            }

                            target = Helper.GetDeltaSplitTarget(t.Location, positions.upper);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GrowTo, target);
                                break;
                            }
                            
                            target = Helper.GetDeltaSplitTarget(t.Location, positions.lower);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GrowTo, target);
                                break;
                            }
                        }
                        goto case 2;
                    case 2: // transfer half matter to top technites toDo: function evaluate best transfer choice (neighbour has max matter or ttl is low)
                        if(t.CurrentResources.Energy == 0 && t.CurrentResources.Matter == 0 && t.Status.Lit == false)
                        {
                            goto case 99;
                        }
                        target = Helper.GetMatterNeighbourTechnite(t.Location); // maybe filter invalid targets 
                        byte halfRes = (byte)(t.CurrentResources.Matter / 2);

                        if (target != Grid.RelativeCell.Invalid && halfRes > 0)
                        {
                            t.SetNextTask(Technite.Task.TransferMatterTo, target, halfRes);
                            break;
                        }
                        goto case 3;
                    case 3: // transfer energy half
                        target = Helper.GetEnergyNeighbourTechnite(t.Location);
                        halfRes = (byte)(t.CurrentResources.Energy / 2);
                        if (target != Grid.RelativeCell.Invalid && halfRes > 0)
                        {
                            t.SetNextTask(Technite.Task.TransferEnergyTo, target, halfRes);
                            break;
                        }
                        goto case 4;
                    case 4: // ttl transfer all energy and matter
                        //target sucht zuerst ein Technite, welches tiefer liegt, also möglicherweise weniger Energie bekommt
                        target = Helper.GetLitOrUpperTechnite(t.Location);    
 
                        if (t.CurrentResources.Matter >= t.CurrentResources.Energy)
                        {
                            if (target != Grid.RelativeCell.Invalid && t.CurrentResources.Matter > 0)
                            {
                                t.SetNextTask(Technite.Task.TransferMatterTo, target, t.CurrentResources.Matter);
                                break;
                            }
                        }
                        //target sucht danach ein Technite, welchers höher liegt, also möglicherweise Matter benötigt
                        target = Helper.GetUnlitOrLowerTechnite(t.Location);
                        if (target != Grid.RelativeCell.Invalid && t.CurrentResources.Energy > 0)
                        {
                            t.SetNextTask(Technite.Task.TransferEnergyTo, target, t.CurrentResources.Energy);
                            break;
                        }
                        goto case 99;
                    case 99:
                        t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        break;  
                    //case 123: // sudden death mode
                    //    target = Helper.GetFoodChoice(t.Location);
                    //    if (target != Grid.RelativeCell.Invalid)
                    //        t.SetNextTask(Technite.Task.ConsumeSurroundingCell, target);
                    //    else
                    //        goto case 99;
                    //    break;
                };
            }
            
			//let's do some simple processing

			//			bool slightlyVerbose = Technite.All.Count() < 20;

			
		}
	}
}