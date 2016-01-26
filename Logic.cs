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

            public const int NotAChoice = 0,
                             NotFinishedYet = 2;
            public static int techniteNeighbors;

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
                    return f(relative, other);
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
                    int yield = Technite.MatterYield[(int)content]; //zero is zero, no exceptions
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
                Technite t = Technite.Find(location);

                foreach (var n in location.GetRelativeDeltaNeighbors((int)delta)) //cast in int überflüssig
                {
                    Grid.CellID cellLocation = location + n;
                    

                    int q = f(n, cellLocation);
                    if (q == 2) t.NotFinishedYet = true;
                    if (q == 1)
                    {
                        total += q;

                        options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
                    }

                }
                if (total == 0)
                {
                    t.NotFinishedYet = false;
                    return Grid.RelativeCell.Invalid;
                }
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
                    Grid.Content content = Grid.World.GetCell(cell).content;
                    if (content != Grid.Content.Foundation && content != Grid.Content.Technite && content != Grid.Content.Water && content != Grid.Content.Clear)
                    //if (Technite.EnoughSupportHere(cell))
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
                    if (Grid.World.GetCell(cell).content != Grid.Content.Technite)
                        return yield;
                    //int energyYield = Technite.EnergyYieldAtLayer[location.Layer];
                    return NotAChoice;
                }
                );
            }

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
                    if (q > 1)
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

            public static Grid.RelativeCell GetDeltaSplitTarget(Grid.CellID location, int delta)
            {
                return EvaluateDeltaChoices(location, (relative, cell) =>
                {
                    if (Grid.World.GetCell(cell).content == Grid.Content.Clear || Grid.World.GetCell(cell).content == Grid.Content.Water)
                    // check for different faction e.g. enemy technites
                    {
                        if (Grid.World.GetCell(cell.BottomNeighbor).content == Grid.Content.Foundation || Grid.World.GetCell(location.BottomNeighbor).content == Grid.Content.Foundation || searchBottomNeighborsForFoundation(cell.BottomNeighbor))
                        {
                            if (Technite.EnoughSupportHere(cell))
                                if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                                    return 1;
                                else return NotFinishedYet;
                        }
                        if (countNeighborTechnites(cell) <= 3)
                        {
                            if (techniteNeighbors == 3) return 1;
                            else return NotFinishedYet;
                        }
                        
                    }
                    return NotAChoice;
                }
                , delta);
            }
            public static bool searchBottomNeighborsForFoundation(Grid.CellID location)
            {
                foreach(var n in location.GetHorizontalNeighbors())
                {
                    if (Grid.World.GetCell(n).content == Grid.Content.Foundation) return true;
                }
                return false;
            }
            public static int countNeighborTechnites(Grid.CellID location)
            {
                techniteNeighbors = 0;
                foreach (var n in location.GetHorizontalNeighbors())
                {
                    if (Grid.World.GetCell(n).content == Grid.Content.Technite) techniteNeighbors++;
                }
                return techniteNeighbors;
            }

            /// <summary>
            /// Determines a feasible, possibly ideal neighbor technite target, based on a given evaluation function
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise. Higher values indicate higher probability</param>
            /// <returns>Chocen relative cell, or Grid.RelativeCell.Invalid if none was found.</returns>
            public static Grid.RelativeCell EvaluateMinResourceNeighborTechnites(Grid.CellID location, Func<Grid.RelativeCell, Technite, int> f)
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
            /// Determines a cell of the upper neighbor
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell GetEnergyNeighbourTechnite(Grid.CellID location)
            {
                return EvaluateMinResourceNeighborTechnites(location, (relative, technite) =>
                {
                    int curEnergy = technite.CurrentResources.Energy;
                    return curEnergy;
                });
            }

            /// <summary>
            /// Determines a cell of the upper neighbor
            /// von uns
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static Grid.RelativeCell GetMatterNeighbourTechnite(Grid.CellID location)
            {
                return EvaluateMinResourceNeighborTechnites(location, (relative, technite) =>
                {
                    int curMatter = technite.CurrentResources.Matter;
                    return curMatter;
                });
            }
            

            /*--------------------------------------------------------------------------------*/
        }

        private static Random random = new Random();
        /// <summary>
        /// Central logic method. Invoked once per round to determine the next task for each technite.
        /// </summary>

        static uint startPosition;
        static bool firstRound = true;
        public enum MyState
        {
            gnawOrConsume = 0,
            transformFoundation = 1,
            growUp = 2,
            growHorizontally = 3,
            consumeAround = 4,
            //transfer = 5,
            doNothing = 255,
        };

        public static void ProcessTechnites()
        {
            Out.Log(Significance.Common, "ProcessTechnites()");

            Grid.RelativeCell target;
            foreach (Technite t in Technite.All)
            {
                if (firstRound)
                {
                    startPosition = t.Location.StackID;
                    firstRound = false;
                    t.grow_horizontally_done = true;
                }
                if (t.selfTransform)
                {
                    if (t.CurrentResources.Matter >= 10)
                        t.mystate = MyState.transformFoundation;
                    else t.mystate = MyState.gnawOrConsume;
                }
                else if (t.CurrentResources.Matter >= 5)
                {
                    if (t.CanSplit)
                    {
                        if (!t.grow_horizontally_done) t.mystate = MyState.growHorizontally;
                        else if (t.Location.StackID == startPosition)
                            if (t.CurrentResources.Matter >= 15) t.mystate = MyState.growUp;
                            else t.mystate = MyState.gnawOrConsume;
                        else
                        {
                            if (t.CurrentResources.Matter >= 10) t.mystate = MyState.transformFoundation;
                            else t.mystate = MyState.gnawOrConsume;
                        }
                    }
                    else t.mystate = MyState.doNothing;             // waiting for energy
                }
                else t.mystate = MyState.gnawOrConsume;


                switch (t.mystate)
                {
                    case MyState.gnawOrConsume: // gnaw or consume if MatterYield <= 1
                        {
                            if (t.CanGnawAt)
                            {
                                target = Helper.GetMaxMatterGnawChoice(t.Location);
                                if (target != Grid.RelativeCell.Invalid)
                                {
                                    t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                                    break;
                                }
                                else
                                {
                                    target = Helper.GetFoodChoice(t.Location);
                                    if(target != Grid.RelativeCell.Invalid)
                                    {
                                        t.SetNextTask(Technite.Task.ConsumeSurroundingCell, target);
                                        break;
                                    }
                                }
                            }
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }
                    case MyState.transformFoundation: // transform to foundation

                            t.SetNextTask(Technite.Task.SelfTransformToType, Grid.RelativeCell.Self, 7);
                            break;

                    case MyState.growUp: // grow up
                        {
                            target = new Grid.RelativeCell(15, 1);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                Grid.CellID absoluteTarget = t.Location + target;
                                if (absoluteTarget.IsValid)
                                {
                                    t.SetNextTask(Technite.Task.GrowTo, target);
                                    t.selfTransform = true;
                                    break;
                                }
                            }
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }

                    case MyState.growHorizontally: // grow horizontal
                        {
                            target = Helper.GetDeltaSplitTarget(t.Location, 0);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                Grid.CellID absoluteTarget = t.Location + target;
                                if (absoluteTarget.IsValid)
                                {
                                    t.SetNextTask(Technite.Task.GrowTo, target);
                                    break;
                                }
                            }
                            else
                            {
                                if (!t.NotFinishedYet)
                                {
                                    t.grow_horizontally_done = true;
                                    if (t.Location.StackID != startPosition) t.selfTransform = true;
                                }
                            }
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }
                    case MyState.consumeAround:
                        break;
                   
                    case MyState.doNothing: // do nothing
                        {
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }
                }
            }
        }
    }
}
