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

            public static Grid.RelativeCell EvaluateYieldChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
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
                {
                    //Out.Log(Significance.ProgramFatal, "Logic error");
                    return Grid.RelativeCell.Invalid;
                }
                if (options.Count == 1)
                    return options[0].Value;
                int maxYield = 0;
                Grid.RelativeCell maxOption = Grid.RelativeCell.Invalid;
                foreach (var o in options)
                {
                   if(maxYield <= o.Key)
                   {
                       maxYield = o.Key;
                       maxOption = o.Value;
                   }
                }
                return maxOption;
                //else
                //{
                //    Out.Log(Significance.ProgramFatal, "Logic error");
                //    return Grid.RelativeCell.Invalid;
                //}
            }

            public static Grid.RelativeCell EvaluateMountainChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            {
                //suche höhergelegene leere Zelle über Matter
                options.Clear();
                int total = 0;
                foreach (var n in location.GetRelativeEqualOrUpperNeighbors())  //GetRelativeUpperNeighbors effizienter
                {
                    Grid.CellID cellLocation = location + n;
                    if(cellLocation.Layer >= location.Layer)
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
                //Out.Log(Significance.ProgramFatal, "Logic error");
                //return Grid.RelativeCell.Invalid;
            }

            public static Grid.RelativeCell EvaluateValleyChoices(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            {
                //suche höhergelegene leere Zelle über Matter
                options.Clear();
                int total = 0;
                foreach (var n in location.GetRelativeEqualOrLowerNeighbors())  //GetRelativeUpperNeighbors effizienter
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
                //Out.Log(Significance.ProgramFatal, "Logic error");
                //return Grid.RelativeCell.Invalid;
            }

            //public static Grid.RelativeCell EvaluateUpper(Grid.CellID location, Func<Grid.RelativeCell, Grid.CellID, int> f)
            //{
            //    options.Clear();
            //    Console.WriteLine("Versucht UpperNeighbor zu finden");
            //    //Grid.RelativeCell n = location.GetMyUpperNeighbor(location);
            //    foreach (var n in location.GetRelativeUpperNeighbors())
            //    {
            //        Grid.CellID cellLocation = location + n;
            //        int q = f(n, cellLocation);
            //        options.Add(new KeyValuePair<int, Grid.RelativeCell>(q, n));
            //        return options[0].Value;
            //    }

            //    //Grid.RelativeCell n = location.GetUpperNeighbor();
            //    Out.Log(Significance.ProgramFatal, "Logic error");
            //    return Grid.RelativeCell.Invalid;
            //}


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
                    int yield = Technite.MatterYield[(int)content];	//zero is zero, no exceptions
                    if (Grid.World.GetCell(cell.BottomNeighbor).content != Grid.Content.Technite)
                        return yield;
                    return NotAChoice;
                }
                );
            }

            public static Grid.RelativeCell GetMyGnawChoice(Grid.CellID location)
            {
                return EvaluateYieldChoices(location, (relative, cell) =>
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

            public static Grid.RelativeCell GetMountainTarget(Grid.CellID location)
            {
                return EvaluateMountainChoices(location, (relative, cell) =>
                {
                    //Grid.Content content = Grid.World.GetCell(cell).content;
                    //int rs = 100;
                    //if (content != Grid.Content.Clear && content != Grid.Content.Water)
                    //    rs -= 90;
                    //if (Grid.World.GetCell(cell.TopNeighbor).content == Grid.Content.Technite)
                    //    return NotAChoice;  //probably a bad idea to split beneath technite

                    //if (Technite.EnoughSupportHere(cell))
                    //    return relative.HeightDelta + rs;

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

            public static Grid.RelativeCell GetValleyTarget(Grid.CellID location)
            {
                return EvaluateValleyChoices(location, (relative, cell) =>
                {
                    //Grid.Content content = Grid.World.GetCell(cell).content;
                    //int rs = 100;
                    //if (content != Grid.Content.Clear && content != Grid.Content.Water)
                    //    rs -= 90;
                    //if (Grid.World.GetCell(cell.TopNeighbor).content == Grid.Content.Technite)
                    //    return NotAChoice;  //probably a bad idea to split beneath technite

                    //if (Technite.EnoughSupportHere(cell))
                    //    return relative.HeightDelta + rs;

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
            /// Determines the top neighbor cell or splits to top
            /// </summary>
            /// /// <param name="location"></param>
            /// <returns></returns>
            //public static Grid.RelativeCell GetTopTarget(Grid.CellID location)
            //{
            //    return EvaluateUpper(location, (relative, cell) =>
            //    {
            //        int rs = 100;
            //        Grid.Content content = Grid.World.GetCell(cell).content;

            //        if (content != Grid.Content.Clear && content != Grid.Content.Water)
            //            rs -= 90;
            //        if (Grid.World.GetCell(cell.TopNeighbor).content == Grid.Content.Technite)
            //            return NotAChoice;  //probably a bad idea to split beneath technite

            //        if (Technite.EnoughSupportHere(cell))
            //            return relative.HeightDelta + rs;

            //        return NotAChoice;
            //    }
            //    );
            //}
        }

        private static Random random = new Random();

        /// <summary>
        /// Central logic method. Invoked once per round to determine the next task for each technite.
        /// </summary>
        public static void ProcessTechnites()
        {
            Out.Log(Significance.Common, "ProcessTechnites()");
            int spielphase;

            foreach (Technite t in Technite.All)
            {
                if (t.Location.Layer > 16)  //t.EnergyYieldPerRound > 4
                {
                    spielphase = 1;
                    if (t.Location.Layer == 17)
                        spielphase = 3;
                }
                else
                    spielphase = 0;

                if (t.Status.TTL <= 2)
                    spielphase = 2;

                switch (spielphase)
                {
                    case 0: 
                        //Wenn spawn unter der Erde -> Hochfressen
                        Grid.RelativeCell target;
                        Grid.Content content;
                        Grid.CellID absoluteTarget;
                        if (t.CanSplit)
                        {
                            target = Helper.GetSplitTarget(t.Location);     //funktion optimieren, indem nur topneighbor ausgewählt werden um nonetask zu vermeiden
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                absoluteTarget = t.Location + target;
                                content = Grid.World.GetCell(absoluteTarget.TopNeighbor).content;
                                if (content != Grid.Content.Technite)
                                {
                                    t.SetNextTask(Technite.Task.GrowTo, target);
                                    break;
                                }
                            }
                        }
                        if (t.CanGnawAt && t.CurrentResources.Matter < 6)
                        {
                            target = Helper.GetMyGnawChoice(t.Location);
                            //target = Helper.GetFoodChoice(t.Location);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                absoluteTarget = t.Location + target;
                                content = Grid.World.GetCell(absoluteTarget).content;
                                int yield = Technite.MatterYield[(int)content];	//zero is zero, no exceptions

                                if (yield > 1)
                                    // nage nicht an zellen, die keine Matter liefern
                                    t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                                else if (yield == 1)// letzter Ausweg, wenn Spawnpunkt im Wasser und keine Matter verfügbar ist
                                    t.SetNextTask(Technite.Task.ConsumeSurroundingCell, target);
                                else
                                    t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                                break;
                            }
                        }
                        else
                        {
                            //Technite spart Energie zum splitten
                        }
                        t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        break;
                    case 1:
                        //an der Planetoberfläche
                        if(t.CanSplit)
                        {
                            target = Helper.GetMountainTarget(t.Location); //optimieren durch leere Zelle

                            //if(target == Grid.RelativeCell.Invalid)
                            //{
                            //    target = Helper.GetSplitTarget(t.Location);
                            //}
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GrowTo, target);
                                break;
                            }

                            target = Helper.GetValleyTarget(t.Location);

                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GrowTo, target);
                                break;
                            }
                        }

                        //auf Berge bauen
                        if (t.CanGnawAt)
                        {
                            target = Helper.GetMyGnawChoice(t.Location);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                                break;
                            }
                        }
                        else t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        goto case 2;

                    case 2:
                        // transfer matter + energy before dead
                        Grid.RelativeCell energyTarget = Helper.GetUnlitOrLowerTechnite(t.Location);    //EnergieTarget sucht ein Technite, welches tiefer liegt, also möglicherweise weniger Energie bekommt
                        Grid.RelativeCell matterTarget = Helper.GetLitOrUpperTechnite(t.Location);      //MatterTarget sucht ein Technite, welchers höher liegt, also möglicherweise Matter benötigt

                        if (t.CurrentResources.Matter >= t.CurrentResources.Energy)
                        {
                            if (matterTarget != Grid.RelativeCell.Invalid && t.CurrentResources.Matter > 0)
                            {
                                t.SetNextTask(Technite.Task.TransferMatterTo, matterTarget, t.CurrentResources.Matter);
                                Console.WriteLine("=====================");
                                Console.WriteLine("> " + t.CurrentResources.Matter + " < Matter transferiert an: " + matterTarget);
                                Console.WriteLine("=====================");
                                break;
                            }
                        }
                        if (energyTarget != Grid.RelativeCell.Invalid && t.CurrentResources.Energy > 0)
                        {
                            t.SetNextTask(Technite.Task.TransferEnergyTo, energyTarget, t.CurrentResources.Energy);

                            Console.WriteLine("=====================");
                            Console.WriteLine("> " + t.CurrentResources.Energy + " < Energie transferiert an: " + energyTarget);
                            Console.WriteLine("=====================");
                            break;
                        }
                        t.SetNextTask(Technite.Task.None, Grid.RelativeCell.Self);
                        break;
                    case 3: //horizontale Ausbreitung, wenn Technite auf NormalNull
                        //von case 0

                        if (t.CanSplit)
                        {
                            // pärfoamänz
                            target = Helper.GetMountainTarget(t.Location);
                            //if(target != Grid.RelativeCell.Invalid)
                            //    goto case 1;
                            //else
                            //{
                            //    target =
                            //    t.SetNextTask(Technite.Task.GrowTo, target);
                            //    break;
                            //}
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GrowTo, target);
                                break;
                            }
                            target = Helper.GetSplitTarget(t.Location);
                            if(target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GrowTo, target);
                                break;
                            }
                        }
                        if(t.CanGnawAt)
                        {
                            target = Helper.GetMyGnawChoice(t.Location);
                            if (target != Grid.RelativeCell.Invalid)
                            {
                                t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
                                break;
                            }
                        }
                        goto case 2;
                        //nach case 1, wenn man an Berg trifft
                }
            }
            ////let's do some simple processing

            ////			bool slightlyVerbose = Technite.All.Count() < 20;


            //int at = 0;
            //foreach (Technite t in Technite.All)
            //{
            //	at++;
            //             Console.WriteLine("Die höhe dieses Technites: " + t.Location.Layer);
            //             if (at < 20)
            //		Out.Log(Significance.Low, "Processing "+t);
            //	else
            //		if (at == 20)
            //			Out.Log(Significance.Low, "...");

            //	if (t.Status.TTL <= 1)
            //		t.SetCustomColor(new Technite.Color(255, 0, 0));
            //	else
            //	{
            //		float r0 = Grid.CellStack.HeightPerLayer * 2f;
            //		float r1 = r0 + Grid.CellStack.HeightPerLayer * 2f;
            //		float r02 = r0*r0,
            //				r12 = r1*r1;
            //		int atRange = 2;
            //		foreach (var obj in Objects.AllGameObjects)
            //		{
            //			float d2 = Vec.QuadraticDistance(obj.ID.Location.WorldPosition,t.Location.WorldPosition);
            //			if (d2 < r12)
            //			{
            //				atRange = 1;
            //				if (d2 < r02)
            //				{
            //					atRange = 0;
            //					break;
            //				}
            //			}
            //		}
            //		//if (atRange == 0)
            //		//	t.SetCustomColor(new Technite.Color(255,0,0));
            //		//else
            //			if (atRange == 0)
            //				t.SetCustomColor(new Technite.Color(32, 32, 32));
            //			else
            //				t.UnsetCustomColor();
            //	}
            //	//this will color technites depending on their up-direction in the world:
            //	//t.SetCustomColor(new Technite.Color(t.Location.UpDirection*0.5f + 0.5f));

            //	if (t.LastTaskResult == Technite.TaskResult.MoreWorkNeeded)
            //	{
            //		bool skip = false;
            //		switch (t.LastTask)
            //		{
            //			case Technite.Task.ConsumeSurroundingCell:
            //				skip = t.CanConsume;
            //				break;
            //			case Technite.Task.GrowTo:
            //				skip = t.CanSplit;
            //				break;
            //		}
            //		if (skip)
            //		{
            //			//Out.Log(Significance.Common, "Still busy doing last job ("+t.LastTask+"). Allowing technite to continue");
            //			continue;
            //		}
            //	}
            //	if (t.CurrentResources.Energy > 9 && random.NextDouble() < 0.1)
            //	{
            //		t.SetNextTask(Technite.Task.Scan, Grid.RelativeCell.Self, t.CurrentResources.Energy);
            //		continue;
            //	}
            //	bool tryTransfer = false;
            //	if (t.CanSplit)
            //	{
            //		Grid.RelativeCell target = Helper.GetSplitTarget(t.Location);
            //		if (target != Grid.RelativeCell.Invalid)
            //		{
            //			t.SetNextTask(Technite.Task.GrowTo, target);
            //		}
            //		else
            //		{
            //			//Out.Log(Significance.Unusual, "Unable to find adequate splitting destination");
            //			tryTransfer = true;
            //		}
            //	}
            //	else
            //	{
            //		bool waitForSplitEnergy = t.Status.Lit && t.CurrentResources.Matter >= Technite.SplitMatterCost;
            //                 if (t.CanGnawAt && !waitForSplitEnergy)
            //		{
            //			Grid.RelativeCell target = Helper.GetFoodChoice(t.Location);
            //			if (target != Grid.RelativeCell.Invalid)
            //			{
            //				t.SetNextTask(Technite.Task.GnawAtSurroundingCell, target);
            //			}
            //			else
            //			{
            //				//Out.Log(Significance.Unusual, "Unable to find adequate eating destination");
            //				tryTransfer = true;
            //			}
            //		}
            //		else
            //		{
            //			//Out.Log(Significance.Unusual, "Insufficient resources to do anything");
            //			tryTransfer = !waitForSplitEnergy && t.CurrentResources != Technite.Resources.Zero;
            //		}
            //	}
            //	if (tryTransfer)
            //	{
            //		Grid.RelativeCell target = Grid.RelativeCell.Invalid;
            //		Technite.Task task;
            //		byte amount = 0;
            //                 if (t.CurrentResources.Matter > t.CurrentResources.Energy)
            //		{
            //			//Out.Log(Significance.Low, "Trying to transfer matter");
            //			task = Technite.Task.TransferMatterTo;
            //			target = Helper.GetLitOrUpperTechnite(t.Location);
            //			amount = t.CurrentResources.Matter;
            //		}
            //		else
            //		{
            //			//Out.Log(Significance.Low, "Trying to transfer energy");
            //			task = Technite.Task.TransferEnergyTo;
            //			target = Helper.GetUnlitOrLowerTechnite(t.Location);
            //			amount = t.CurrentResources.Energy;

            //		}
            //		if (target != Grid.RelativeCell.Invalid)
            //		{
            //			t.SetNextTask(task, target, amount);
            //		}
            //		else
            //		{
            //			//Out.Log(Significance.Unusual, "Unable to find adequate transfer target");
            //			tryTransfer = true;
            //		}


            //	}


            //}


        }
    }
}
