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

            /*--------------------------------------------------------------------------------*/

            /// <summary>
            /// Returns a random cell in the given delta
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
            /// <returns>Chocen relative cell, or Grid.RelativeCell.Invalid if none was found.</returns>
            public static Grid.RelativeCell EvaluateDeltaChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f, int delta)
            {
                options.Clear();
                int total = 0;


                foreach (var n in location.GetRelativeDeltaNeighbors((int)delta))
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

            /// <summary>
            /// Determines a cell of the specified delta position
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell GetDeltaConsumeTarget(Grid.CellID location, int delta)
            {
                return EvaluateDeltaChoices(location, (relative, cell) =>
                {
                    //if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                    //{
                        if (Technite.EnoughSupportHere(cell))
                            return 1;
                    //}
                    return NotAChoice;
                }
                , delta);
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
                    int yield = Technite.MatterYield[(int)content]; //zero is zero, no exceptions
                    // verhindert das techniten über andere techniten bauen und damit lit = false werden könnte
                    if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                        return yield;
                    //int energyYield = Technite.EnergyYieldAtLayer[location.Layer];
                    return NotAChoice;
                }
                );
            } /// <summary>
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

            /*--------------------------------------------------------------------------------*/
        }

        private static Random random = new Random();
		/// <summary>
		/// Central logic method. Invoked once per round to determine the next task for each technite.
		/// </summary>

        static bool firstTurn = true;
        static byte pos = 0;

        public static void ProcessTechnites()
        {
            Out.Log(Significance.Common, "ProcessTechnites()");

            Grid.RelativeCell target;

            foreach (Technite t in Technite.All)
            {
                //Grid.RelativeCell n = new Grid.RelativeCell(0, 1);
                Grid.CellID location = t.Location;
                //Technite t_new = Technite.Find(location);
                if(Technite.Count != 1 && (Grid.World.GetCell(location.BottomNeighbor).content != Grid.Content.Technite))
                {
                    t.done = true;
                }
                if (t.done)
                {
                    t.mystate = 255;
                }
                else
                {
                    if(Technite.Count == 1)
                    t.mystate = 52;
                    //else if(t.mystate == 52)
                    //{

                    //}
                    //else
                    //{
                        //Technite t_bottom = Technite.Find(location.BottomNeighbor);
                        //if (t_bottom.grow_left)
                        //{
                        //    t.grow_right = true;
                        //    t.grow_left = false;
                        //    t.mystate = 51;

                        //}
                        //else if (t_bottom.grow_right)
                        //{

                        //    t.grow_left = true;
                        //    t.grow_right = false;
                        //    t.mystate = 50;

                        //} 
                    //}
                }
                //if(firstTurn)
                //{
                //    t.root = true;
                //    firstTurn = false;
                //}

                //if(t.selfTransform)
                //    state = 100;

                switch (t.mystate)
                {
                    case 0:
                        if (t.CanConsume)
                        {
                            if(t.root)
                            {
                                bool clearNeighbors = true;
                                foreach (var n in t.Location.GetRelativeDeltaNeighbors(0))
                                {
                                    Grid.CellID cellLocation = t.Location + n;
                                    if (Grid.World.GetCell(cellLocation).content != Grid.Content.Clear && Grid.World.GetCell(cellLocation).content != Grid.Content.Water)
                                        clearNeighbors = false;
                                    Console.Out.WriteLine(Grid.World.GetCell(cellLocation).content);
                                }
                       
                                if (clearNeighbors && t.CanSplit)
                                {
                                    target = new Grid.RelativeCell(15, 1);
                                    t.SetNextTask(Technite.Task.GrowTo, target);
                                    t.selfTransform = true;
                                }
                                else
                                {
                                    target = Helper.GetDeltaConsumeTarget(t.Location, 0);
                                    t.SetNextTask(Technite.Task.ConsumeSurroundingCell, target);
                                }  
                            }
                        }
                        else
                        {
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        }
                        break;

                    //case 50:                //baue baum
                    //    if (t.CanSplit)
                    //    {
                    //        if (t.grow_left)
                    //        {
                    //            target = new Grid.RelativeCell(0, 0);
                    //            t.SetNextTask(Technite.Task.GrowTo, target);
                    //            t.mystate = 52;
                    //            //t.grow_left = false;
                    //            break;
                    //        }
                    //    }
                    //    else if (t.CanGnawAt && t.CurrentResources.Matter <= 5)
                    //    {
                    //        target = Helper.GetMaxMatterGnawChoice(t.Location);          // maxGnawChoice
                    //        if (target != Grid.RelativeCell.Invalid)
                    //        {
                    //            t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                    //            break;
                    //        }
                    //    }
                    //    else if (t.CurrentResources.Energy > 10)
                    //    {
                    //        target = Helper.GetUnlitOrLowerTechnite(t.Location);
                    //        t.SetNextTask(Technite.Task.TransferEnergyTo, target, 5);
                    //        break;
                    //    }
                    //    t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                    //    break;

                    case 51:
                        if(t.CanSplit)
                        {
                            if (t.grow_side)
                            {
                                target = new Grid.RelativeCell(pos, 0);
                                if (target != Grid.RelativeCell.Invalid)
                                {
                                    t.SetNextTask(Technite.Task.GrowTo, target);
                                    pos++;
                                    if (pos == 6)
                                        pos = 0;
                                    t.mystate = 52;
                                    //t.grow_right = false;
                                    break;
                                }
                            }
                        }
                        if (t.CanGnawAt && t.CurrentResources.Matter <= 5)
                        {
                            target = Helper.GetMaxMatterGnawChoice(t.Location);          // maxGnawChoice
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                                break;
                            }
                        }
                        if(t.CurrentResources.Energy > 10)
                        {
                            target = Helper.GetUnlitOrLowerTechnite(t.Location);
                            t.SetNextTask(Technite.Task.TransferEnergyTo, target, 5);
                            break;
                        }
                        t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        break;

                    case 52:
                        if (t.CanSplit)
                        {
                            if (t.grow_up)
                            {
                                target = new Grid.RelativeCell(15, 1);
                                Grid.CellID absoluteTarget = t.Location + target;
                                if(target != Grid.RelativeCell.Invalid && absoluteTarget.IsValid)
                                {
                                    t.SetNextTask(Technite.Task.GrowTo, target);
                                    t.done = true;
                                    t.grow_up = false;
                                    break;
                                }
                                t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                                break;
                                
                            }
                        }
                        if(t.CanGnawAt && t.CurrentResources.Matter <= 5)          //!!!!CanGnawAt überprüft, ob der technite die nötige energie zum nagen hat, nicht ob es ein nageziel gibt!!!!
                        {
                            target = Helper.GetMaxMatterGnawChoice(t.Location);          // maxGnawChoice
                            if(target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                                break;
                            }
                        }
                        if (t.CurrentResources.Energy > 10)
                        {
                            target = Helper.GetUnlitOrLowerTechnite(t.Location);
                            t.SetNextTask(Technite.Task.TransferEnergyTo, target, 5);
                            break;
                        }
                        t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        break;

                    case 100: // self transform to type
                        Grid.Content content = Grid.World.GetCell(t.Location.BottomNeighbor).content;
                        byte bumms = 1;
                        t.SetNextTask(Technite.Task.SelfTransformToType, Grid.RelativeCell.Self, bumms);
                        Console.Out.WriteLine("bumms = " + Technite.MatterYield[bumms]);
                        break;
                    case 255:           //gnaw matter and sent it up
                        if(t.CurrentResources.Matter >= 5)
                        {
                            target = Helper.GetLitOrUpperTechnite(t.Location);
                            if(target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.TransferMatterTo, target, t.CurrentResources.Matter);
                                break;
                            }
                        }
                        target = Helper.GetMaxMatterGnawChoice(t.Location);
                        if (target != Grid.RelativeCell.Invalid)
                        {
                            t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                            break;
                        }
                        target = Helper.GetUnlitOrLowerTechnite(t.Location);
                        if(t.CurrentResources.Energy > 0 && target != Grid.RelativeCell.Invalid)
                        {
                            t.SetNextTask(Technite.Task.TransferEnergyTo, target, t.CurrentResources.Energy);
                            break;
                        }
                        
                        t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        break;
                }
            }
        }
	}
}
