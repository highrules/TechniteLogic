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


                foreach (var n in location.GetRelativeDeltaNeighbors((int)delta)) //cast in int überflüssig
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
                    // if (Grid.World.GetCell(cell).content == Grid.Content.Clear || Grid.World.GetCell(cell).content == Grid.Content.Water)
                    // check for different faction e.g. enemy technites
                    {
                        if (Grid.World.GetCell(cell).content != Grid.Content.Foundation && Grid.World.GetCell(cell).content != Grid.Content.Technite) //baut auch in Berge
                        {
                            if (Technite.EnoughSupportHere(cell))
                                return 1;
                        }
                    }
                    return NotAChoice;
                }
                , delta);
            }

            /*--------------------------------------------------------------------------------*/
        }

        private static Random random = new Random();
        /// <summary>
        /// Central logic method. Invoked once per round to determine the next task for each technite.
        /// </summary>

        static uint startPosition;
        static bool firstRound = true;
        static byte counter = 0;
        public enum MyState
        {
            gnawOrConsume = 0,
            transformFoundation = 1,
            growUp = 2,
            growDown = 3,
            consumeAround = 4,
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
                    t.done = true;
                }

                if(t.selfTransform)
                {
                    // t.mystate = t.CurrentResources.Matter >= 10 ? t.mystate = 1 : t.mystate = 0;
                    if(t.CurrentResources.Matter >= 10)
                        t.mystate = MyState.transformFoundation;
                    else
                        t.mystate = MyState.gnawOrConsume;
                }
                else
                {
                    if (t.CurrentResources.Matter >= 5)
                    {
                        if (t.CanSplit)
                        {
                            if (t.Location.StackID == startPosition)
                            {
                                if (!t.done)
                                    t.mystate = MyState.growDown;
                                else
                                {
                                    if (t.CurrentResources.Matter >= 15) // split and enough Matter to transform
                                        if (Technite.Count == 1 || counter == 100)
                                            t.mystate = MyState.growUp;
                                        else 
                                            counter++;
                                    else
                                        t.mystate = MyState.gnawOrConsume;
                                }
                            }
                            else
                            {
                                if (t.done)
                                {
                                    if (t.consumeAround)
                                        t.mystate = MyState.consumeAround;
                                    else if (t.CurrentResources.Matter >= 10)
                                        t.mystate = MyState.transformFoundation;
                                    else
                                        t.mystate = MyState.gnawOrConsume;
                                }
                                else
                                    t.mystate = MyState.growDown;
                            }
                        }
                        else
                            t.mystate = MyState.doNothing;
                    }
                    else
                        t.mystate = MyState.gnawOrConsume;
                }


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
                        {
                            // if(t.CanTransform) //gibts nicht
                            t.SetNextTask(Technite.Task.SelfTransformToType, Grid.RelativeCell.Self, 7);
                            // t.done = true;
                            break;
                        }
                    case MyState.growUp: // grow up
                        {
                            if (t.CanSplit)
                            {
                                target = new Grid.RelativeCell(15, 1);
                                Grid.CellID absoluteTarget = t.Location + target;
                                if (target != Grid.RelativeCell.Invalid)
                                {
                                    if(absoluteTarget.IsValid)
                                    {
                                        t.SetNextTask(Technite.Task.GrowTo, target);
                                        t.selfTransform = true;
                                        break;
                                    }
                                    else
                                    {
                                        // top of grid
                                        t.selfTransform = true;
                                    }
                                }
                            }
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }
                    case MyState.growDown: // grow down
                        {
                            if (t.CanSplit)
                            {
                                target = Helper.GetDeltaSplitTarget(t.Location, -1);
                                if (target != Grid.RelativeCell.Invalid)
                                {
                                    t.SetNextTask(Technite.Task.GrowTo, target);
                                    break;
                                }
                                else
                                {
                                    t.done = true;
                                    t.consumeAround = true;
                                    
                                }
                            }
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }
                    case MyState.consumeAround :
                        {
                            target = Helper.GetDeltaConsumeTarget(t.Location, 0);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.ConsumeSurroundingCell, target);
                                break;
                            }
                            else if (t.Location.StackID != startPosition)
                            {
                                t.selfTransform = true;
                                t.consumeAround = false;
                            }
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }
                    //case 5: // gnaw matter and sent it up
                    //    if (t.CurrentResources.Matter >= 5)
                    //    {
                    //        target = Helper.GetLitOrUpperTechnite(t.Location);
                    //        if (target != Grid.RelativeCell.Invalid)
                    //        {
                    //            t.SetNextTask(Technite.Task.TransferMatterTo, target, t.CurrentResources.Matter);
                    //            break;
                    //        }
                    //    }
                    //    target = Helper.GetMaxMatterGnawChoice(t.Location);
                    //    if (target != Grid.RelativeCell.Invalid)
                    //    {
                    //        t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                    //        break;
                    //    }
                    //    target = Helper.GetUnlitOrLowerTechnite(t.Location);
                    //    if (t.CurrentResources.Energy > 0 && target != Grid.RelativeCell.Invalid)
                    //    {
                    //        t.SetNextTask(Technite.Task.TransferEnergyTo, target, t.CurrentResources.Energy);
                    //        break;
                    //    }

                    //    t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                    //    break;
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
