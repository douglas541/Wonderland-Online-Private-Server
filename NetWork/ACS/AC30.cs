using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PServer_v2.NetWork.ACS
{
    public class cAC_30 : cAC
    {
        public cAC_30(cGlobals g)
        {
            this.g = g;
        }
         public void SwitchBoard()
        {
            
            
            switch (g.packet.b)
            {
                case 1:
                    {
                        //request get item from prop keeper
                        byte location = g.packet.GetByte(2);
                        DataExt.cInvItem item = g.packet.character.storage.GetInventoryItem(location);
                        
                        
                        
                        if (item.ID > 0)
                        {
                            if (g.packet.character.inv.PlaceItem(item, item.ammt))
                            {
                                g.packet.character.storage.RemoveInv(location, item.ammt);
                                g.packet.character.storage.Save(g.packet.character.characterID);
                                g.packet.character.storage.Send_Storage();
                                g.packet.character.inv.Send_6(g.packet.character, item);
                                
                            }
                        }
                    }
                    break;
                case 2:
                    {
                        //request put/get item in/from prop keeper (same subcommand for both operations)
                        int length = g.packet.data.Length;
                        g.Log(length.ToString());
                        byte location = g.packet.GetByte(2);
                        DataExt.cInvItem invItem = g.packet.character.inv.GetInventoryItem(location);
                        DataExt.cInvItem storageItem = g.packet.character.storage.GetInventoryItem(location);
                        
                        // Check all storage slots to find items
                        string storageItems = "";
                        int storageItemCount = 0;
                        for (int i = 1; i <= 50; i++)
                        {
                            var testItem = g.packet.character.storage.GetInventoryItem((byte)i);
                            if (testItem.ID > 0)
                            {
                                storageItems += "slot" + i + ":id" + testItem.ID + ":ammt" + testItem.ammt + ",";
                                storageItemCount++;
                            }
                        }
                        
                        
                        
                        // If item is in player inventory, put it in storage
                        if (invItem.ID > 0 && invItem.ammt > 0)
                        {
                            
                            
                            if (g.packet.character.storage.putIteminStorage(invItem))
                            {
                                
                                g.packet.character.inv.RemoveInv(location,invItem.ammt);
                                
                                bool saveResult = g.packet.character.storage.Save(g.packet.character.characterID);
                                
                                g.packet.character.storage.Send_Storage();
                            }
                        }
                        // If item is in storage at the specified location, get it from storage
                        else if (storageItem.ID > 0 && storageItem.ammt > 0)
                        {
                            
                            
                            if (g.packet.character.inv.PlaceItem(storageItem, storageItem.ammt))
                            {
                                g.packet.character.storage.RemoveInv(location, storageItem.ammt);
                                g.packet.character.storage.Save(g.packet.character.characterID);
                                g.packet.character.storage.Send_Storage();
                                g.packet.character.inv.Send_6(g.packet.character, storageItem);
                                
                            }
                        }
                        // If no item in inventory but items exist in storage, get first item from storage
                        else if (storageItemCount > 0)
                        {
                            // Find first item in storage
                            byte foundSlot = 0;
                            DataExt.cInvItem foundItem = null;
                            for (int i = 1; i <= 50; i++)
                            {
                                var testItem = g.packet.character.storage.GetInventoryItem((byte)i);
                                if (testItem.ID > 0 && testItem.ammt > 0)
                                {
                                    foundSlot = (byte)i;
                                    foundItem = testItem;
                                    break;
                                }
                            }
                            
                            if (foundItem != null)
                            {
                                    
                                
                                if (g.packet.character.inv.PlaceItem(foundItem, foundItem.ammt))
                                {
                                    g.packet.character.storage.RemoveInv(foundSlot, foundItem.ammt);
                                    g.packet.character.storage.Save(g.packet.character.characterID);
                                    g.packet.character.storage.Send_Storage();
                                    g.packet.character.inv.Send_6(g.packet.character, foundItem);
                                    
                                }
                            }
                        }
                        else
                        {
                            
                        }
                        
                       
                    }
                    break;
                default:
                    {
                        string str = "";
                        str += "Packet code: " + g.packet.a + ", " + g.packet.b + " [unhandled]\r\n";
                        g.logList.Enqueue(str);
                    } break;
            }
        }
    }
}
