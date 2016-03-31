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

            /*-----------------------------------------------------------------------------------------------------------------------------------------------------------*/
            /*-----------------------------------------------------------------Beginn unserer Funktionen-----------------------------------------------------------------*/
            /*-----------------------------------------------------------------------------------------------------------------------------------------------------------*/
            /// <summary>
            /// returns a random cell in the given delta
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise</param>
            /// <returns>chosen relative cell, or Grid.RelativeCell.Invalid if none was found</returns>
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
            /// determines a cell of the specified delta position
            /// </summary>
            /// <param name="location"></param>
            /// <param name="delta">can be -1, 0 or 1</param>
            /// <returns>cell which content is neither a technite nor water or clear</returns>
            public static Grid.RelativeCell GetDeltaConsumeTarget(Grid.CellID location, int delta)
            {
                return EvaluateDeltaChoices(location, (relative, cell) =>
                {
                    Grid.Content content = Grid.World.GetCell(cell).content;
                    if (content != Grid.Content.Foundation && content != Grid.Content.Technite && content != Grid.Content.Water && content != Grid.Content.Clear)
                        return 1;
                    return NotAChoice;
                }
                , delta);
            }

            /// <summary>
            /// determines a gnaw source in the neighborhood of the specified location
            /// </summary>
            /// <param name="location"></param>
            /// <returns>matteryield of the given cell if it's not a technite</returns>
            public static Grid.RelativeCell GetMaxMatterGnawChoice(Grid.CellID location)
            {
                return EvaluateMaxMatterGnawChoices(location, (relative, cell) =>
                {
                    Grid.Content content = Grid.World.GetCell(cell).content;
                    int yield = Technite.MatterYield[(int)content];
                    if (Grid.World.GetCell(cell).content != Grid.Content.Technite)  // prevents technites from splitting over other technites which would result in unlit technites
                        return yield;
                    return NotAChoice;
                }
                );
            }

            /// <summary>
            /// Evaluate all target matter values and return max matter target
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise.</param>
            /// <returns>the neighborcell with the highest matteryield or Grid.RelativeCell.Invalid if no cell is found</returns>
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

            /// <summary>
            /// returns a suitable cell to split to
            /// </summary>
            /// <param name="location"></param>
            /// <param name="delta">can be -1, 0 or 1</param>
            /// <returns>a cell which content is either Clear or Water and is neither Foundation nor Technite that has enough support, otherwise NotAChoice</returns>
            public static Grid.RelativeCell GetDeltaSplitTarget(Grid.CellID location, int delta)
            {
                return EvaluateDeltaChoices(location, (relative, cell) =>
                {
                    if (Grid.World.GetCell(cell).content == Grid.Content.Clear || Grid.World.GetCell(cell).content == Grid.Content.Water)
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

            /// <summary>
            /// DO NOT USE TO SPLIT - just check for possible targets
            /// returns a possibly suitable cell to split to
            /// </summary>
            /// <param name="location"></param>
            /// <param name="delta">can be -1, 0 or 1</param>
            /// <returns>a cell which content is either Clear or Water and is neither Foundation nor Technite, otherwise NotAChoice </returns>
            public static Grid.RelativeCell GetDeltaPossibleSplitTarget(Grid.CellID location, int delta)
            {
                return EvaluateDeltaChoices(location, (relative, cell) =>
                {
                    if (Grid.World.GetCell(cell).content == Grid.Content.Clear || Grid.World.GetCell(cell).content == Grid.Content.Water)
                    // check for different faction e.g. enemy technites
                    {
                        if (Grid.World.GetCell(cell).content != Grid.Content.Foundation && Grid.World.GetCell(cell).content != Grid.Content.Technite) //baut auch in Berge
                            return 1;
                    }
                    return NotAChoice;
                }
                , delta);
            }

            /// <summary>
            /// determines a feasible, possibly ideal neighbor technite target, based on a given evaluation function
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise</param>
            /// <returns>NotAChoice if it's not a technite or the technite could'nt be found in the grid, otherwise the result of function f</returns>
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
            /// searches for the neighbouring technite with the least amount of matter
            /// </summary>
            /// <param name="location"></param>
            /// <param name="f">Evaluation function. Must return 0/NotAChoice if not applicable, and >1 otherwise</param>
            /// <returns> the cell with the lowest resources, otherwise Grid.RelativeCell.Invalid. Can be used for energy and matter.</returns>
            public static Grid.RelativeCell EvaluateMinResourceTransferChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            {
                options.Clear();
                foreach (var n in location.GetRelativeDeltaNeighbors(-1))
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
            }

            /// <summary>
            /// determines the energy of an upper neighbor
            /// </summary>
            /// <param name="location"></param>
            /// <returns> the current energy of the technite</returns>
            public static Grid.RelativeCell GetEnergyNeighbourTechnite(Grid.CellID location)
            {
                return EvaluateMinResourceNeighborTechnites(location, (relative, technite) =>
                {
                    int curEnergy = technite.CurrentResources.Energy;
                    return curEnergy;
                });
            }

            /// <summary>
            /// determines the matter of an upper neighbor
            /// </summary>
            /// <param name="location"></param>
            /// <returns>the current matter of the technite</returns>
            public static Grid.RelativeCell GetMatterNeighbourTechnite(Grid.CellID location)
            {
                return EvaluateMinResourceNeighborTechnites(location, (relative, technite) =>
                {
                    int curMatter = technite.CurrentResources.Matter;
                    return curMatter;
                });
            }


            /*-----------------------------------------------------------------------------------------------------------------------------------------------------------*/
            /*-----------------------------------------------------------------Ende unserer Funktionen-------------------------------------------------------------------*/
            /*-----------------------------------------------------------------------------------------------------------------------------------------------------------*/
        }
            /*-----------------------------------------------------------------------------------------------------------------------------------------------------------*/
            /*-----------------------------------------------------------------Beginn unserer Logik----------------------------------------------------------------------*/
            /*-----------------------------------------------------------------------------------------------------------------------------------------------------------*/
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
            growDown = 3,
            transfer = 5,
            doNothing = 255,
        };

        public static void ProcessTechnites()
        {
            Out.Log(Significance.Common, "ProcessTechnites()");

            Grid.RelativeCell target;
            Grid.CellID absoluteTarget;
            bool hasNeighborTechnite = false;
            foreach (Technite t in Technite.All)
            {
                hasNeighborTechnite = false;
                if (firstRound)
                {
                    startPosition = t.Location.StackID;
                    firstRound = false;
                    t.done = true;
                }

                if (t.selfTransform)
                {
                    if (t.CurrentResources.Matter >= 10)
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
                                        if (Technite.Count == 1)
                                            t.mystate = MyState.growUp;
                                        else
                                            t.mystate = MyState.transfer;
                                    else
                                        t.mystate = MyState.gnawOrConsume;
                                }
                            }
                            else
                            {
                                if (t.done)
                                {
                                    if (t.CurrentResources.Matter >= 10)
                                        t.mystate = MyState.transfer;
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
                                    if (target != Grid.RelativeCell.Invalid)
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
                            t.SetNextTask(Technite.Task.SelfTransformToType, Grid.RelativeCell.Self, 7);
                            break;
                        }
                    case MyState.growUp: // grow up
                        {
                            if (t.CanSplit)
                            {
                                target = new Grid.RelativeCell(15, 1);
                                absoluteTarget = t.Location + target;
                                if (target != Grid.RelativeCell.Invalid)
                                {
                                    if (absoluteTarget.IsValid)
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
                                    Grid.CellID cell = t.Location + target;
                                    if (!Technite.EnoughSupportHere(cell))
                                    {
                                        t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                                        break;
                                    }
                                    t.SetNextTask(Technite.Task.GrowTo, target);
                                    break;
                                }
                                else if (t.Location.StackID == startPosition)
                                {
                                    t.done = true;
                                }
                                else
                                {
                                    target = Helper.GetDeltaPossibleSplitTarget(t.Location, -1);
                                    if (target == Grid.RelativeCell.Invalid)
                                    {
                                        t.done = true;
                                        foreach (var n in t.Location.GetRelativeDeltaNeighbors(-1))
                                        {
                                            absoluteTarget = t.Location + n;
                                            if (absoluteTarget.IsValid)
                                            {
                                                if (Grid.World.GetCell(absoluteTarget).content == Grid.Content.Technite)
                                                {
                                                    hasNeighborTechnite = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (!hasNeighborTechnite)
                                        {
                                            t.selfTransform = true;
                                        }
                                    }
                                    else
                                    {
                                        goto case MyState.transfer;
                                    }
                                }
                            }
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }
                    case MyState.transfer: // check for neighbors on lower level and sent energy up or gnaw matter

                        foreach (var n in t.Location.GetRelativeDeltaNeighbors(-1))
                        {
                            absoluteTarget = t.Location + n;
                            if (absoluteTarget.IsValid)
                            {
                                if (Grid.World.GetCell(absoluteTarget).content == Grid.Content.Technite)
                                {
                                    hasNeighborTechnite = true;
                                    break;
                                }
                            }
                        }
                        if (!hasNeighborTechnite)
                        {
                            if (t.Location.StackID != startPosition)
                                t.selfTransform = true;
                        }
                        else
                        {
                            target = Helper.GetEnergyNeighbourTechnite(t.Location);
                            if (t.CurrentResources.Energy >= 5 && target != Grid.RelativeCell.Invalid && t.LastTask != Technite.Task.TransferEnergyTo)
                            {
                                t.SetNextTask(Technite.Task.TransferEnergyTo, target, 5);
                                break;
                            }
                            if (t.CurrentResources.Matter >= 5 && t.LastTask == Technite.Task.TransferEnergyTo)
                            {
                                target = Helper.GetMatterNeighbourTechnite(t.Location);
                                if (target != Grid.RelativeCell.Invalid)
                                {
                                    t.SetNextTask(Technite.Task.TransferMatterTo, target, 5);
                                    break;
                                }
                            }
                            target = Helper.GetMaxMatterGnawChoice(t.Location);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                                break;
                            }
                        }
                        t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        break;
                    case MyState.doNothing: // do nothing
                        {
                            t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                            break;
                        }
                }
            }
        }

            /*-----------------------------------------------------------------------------------------------------------------------------------------------------------*/
            /*-----------------------------------------------------------------Ende unserer Logik------------------------------------------------------------------------*/
            /*-----------------------------------------------------------------------------------------------------------------------------------------------------------*/
    }
}
