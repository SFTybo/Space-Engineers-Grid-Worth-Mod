using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using System.Linq;

namespace SFTybo.GridWorth
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SFTybo : MySessionComponentBase
    {
        float maximum = 2f;
        float minimum = .5f;
        string lastWord = "";
        string lastComp = "";
        int timer = 0;
        List<IMyCubeGrid> gridList = new List<IMyCubeGrid>();
        
        private float priceWorth(MyCubeBlockDefinition.Component component, string priceType)
        {
            MyBlueprintDefinitionBase bpDef = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(component.Definition.Id);
            int p = 0;
            float price = 0;
            if (priceType == "Ingot")
            {
                for (p = 0; p < bpDef.Prerequisites.Length; p++)
                {
                    if (bpDef.Prerequisites[p].Id != null)
                    {
                        MyDefinitionBase oreDef = MyDefinitionManager.Static.GetDefinition(bpDef.Prerequisites[p].Id);
                        if (oreDef != null)
                        {
                            MyPhysicalItemDefinition ore = oreDef as MyPhysicalItemDefinition;
                            float amn = Math.Abs(ore.MinimumOfferAmount);
                            amn = (float)Math.Round(amn * 2);
                            price = price + amn;
                            //MyVisualScriptLogicProvider.SendChatMessage(bpDef.Prerequisites[p].Id.ToString() + " - " + amn.ToString() + " SC");
                        }
                    }
                }
            }
            if (priceType == "Component")
            {
                float amn = Math.Abs(component.Definition.MinimumOfferAmount);
                //MyAPIGateway.Utilities.ShowNotification(amn.ToString(), 1, "White");
                amn = (float)Math.Round(amn * 8);
                price = price + amn;
                //MyVisualScriptLogicProvider.SendChatMessage(component.Definition.Id.ToString() + " - " + amn.ToString() + " SC");
            }

            return price;
        }
        private float Calculator(IMyCubeGrid grid, string priceType)
        {
            float worth = 0;
            var blocks = new List<IMySlimBlock>();
            int i = 0;
            float average = 0;
            float compo = 0;
            grid.GetBlocks(blocks, b => b != null);
            for (i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                {
                    var definition = blocks[i].BlockDefinition as MyCubeBlockDefinition;
                    if (definition != null)
                    {
                        int n = 0;
                        for (n = 0; n < definition.Components.Length; n++)
                        {
                            if (definition.Components[n] != null)
                            {
                                float amn = priceWorth(definition.Components[n], priceType);
                                worth = worth + amn;
                                compo++;
                                //MyVisualScriptLogicProvider.SendChatMessage(blocks[i].ToString() + " - " + amn.ToString() + " SC");
                            }
                        }
                    }
                }
            }
            average = worth / compo;
            return worth;
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;
        }

        public override void UpdateBeforeSimulation()
        {
            timer += 1;

            if (timer % 600 == 0)
            {
                for (int i = 0; i < gridList.Count; i++)
                {
                    if (gridList[i] != null)
                    {
                        bool isSelling = false;
                        IMyTextPanel pn = null;
                        var blocklist = new List<IMySlimBlock>();
                        gridList[i].GetBlocks(blocklist, b => b != null);
                        for (int n = 0; n < blocklist.Count; n++)
                        {
                            if (blocklist[n].FatBlock != null)
                            {
                                IMyTextPanel panel = blocklist[n].FatBlock as IMyTextPanel;
                                if (panel != null)
                                {
                                    if (panel.CustomName == "SellLCD")
                                    {
                                        isSelling = true;
                                        pn = panel;
                                    }
                                }
                            }
                        }
                        if (isSelling == true && pn != null)
                        {
                            float ingotworth = Calculator(gridList[i], "Ingot");
                            float compworth = Calculator(gridList[i], "Component");

                            Double avg = Math.Round((ingotworth + compworth) / 2f);

                            float maxWorth = 0;
                            float minWorth = 0;
                            if (ingotworth > compworth)
                            {
                                maxWorth = ingotworth;
                                minWorth = compworth;
                            }
                            if (ingotworth < compworth)
                            {
                                maxWorth = compworth;
                                minWorth = ingotworth;
                            }
                            if (ingotworth == compworth)
                            {
                                maxWorth = compworth;
                                minWorth = ingotworth;
                            }
                            List<string> ex = new List<string>();
                            ex = pn.CustomData.Split('\n').ToList();
                            string listing = "No listing";
                            string status = "Idle";
                            if (ex.Count == 2)
                            {
                                listing = ex[1];
                                if (listing == string.Empty)
                                {
                                    listing = "No listing";
                                }
                                status = ex[0];
                                if (status == string.Empty)
                                {
                                    status = "Idle";
                                }
                            }
                            else if (ex.Count > 2)
                            {
                                listing = "Improper format";
                                status = "Improper format";
                            }
                            else if (ex.Count == 1)
                            {
                                listing = "Missing format";
                                status = "Missing format";
                            }
                            string LCDMessage = gridList[i].CustomName + "\n" + status + "\nListing Price: " + listing + "\nAvg Worth: " + String.Format("{0:n0}", avg) + " SC"
                                + "\nMax Worth: " + String.Format("{0:n0}", maxWorth) + " SC " + "\nMin Worth: " + String.Format("{0:n0}", minWorth) + " SC ";
                            pn.WriteText(LCDMessage);
                        }
                    }
                }
            }
            if (timer % 60 == 0)
            {
                IMyCubeGrid grid = MyAPIGateway.CubeBuilder.FindClosestGrid();
                if (grid != null)
                {
                    //MyVisualScriptLogicProvider.SendChatMessage(grid.CustomName + " grids: " + gridList.Count.ToString());
                    var blocklist = new List<IMySlimBlock>();
                    grid.GetBlocks(blocklist);
                    bool isSelling = false;
                    for (int n = 0; n < blocklist.Count; n++)
                    {
                        if (blocklist[n].FatBlock != null)
                        {
                            //MyAPIGateway.Utilities.ShowNotification(blocklist[n].ToString() + " found", 1, "Red");

                            IMyTextPanel panel = blocklist[n].FatBlock as IMyTextPanel;
                            if (panel != null)
                            {
                                if (panel.CustomName == "SellLCD")
                                {
                                    isSelling = true;
                                }
                            }
                        }
                    }
                    if (isSelling == true)
                    {
                        float ingotworth = Calculator(grid, "Ingot");
                        float compworth = Calculator(grid, "Component");
                        Double avg = Math.Round((ingotworth + compworth) / 2f);
                        string message = "This grid is " + grid.CustomName + " and is worth " + String.Format("{0:n0}", ingotworth) + " SC in ingots and " + String.Format("{0:n0}", compworth) + " SC in components";
                        MyAPIGateway.Utilities.ShowNotification(message, 950, "White");
                    }
                }
            }
        }

        private void Entities_OnEntityAdd(VRage.ModAPI.IMyEntity obj)
        {
            if (obj != null)
            {
                IMyCubeGrid grid = obj as IMyCubeGrid;
                if (obj != null)
                {  
                    //MyVisualScriptLogicProvider.SendChatMessage(grid.CustomName + " grids: " + gridList.Count.ToString());
                    gridList.Add(grid);
                }
            }
        }
        private void Entities_OnEntityRemove(VRage.ModAPI.IMyEntity obj)
        {
            if (obj != null)
            {
                IMyCubeGrid grid = obj as IMyCubeGrid;
                if (obj != null)
                {
                    gridList.Remove(grid);
                }
            }
        }

        protected override void UnloadData() //set lists, dictionaries, etc
        {
            MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;
            gridList = null;
        }
    }

    internal class mystringid
    {
    }
}
