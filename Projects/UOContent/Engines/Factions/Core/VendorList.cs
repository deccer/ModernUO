using System.Collections.Generic;
using Server.Utilities;

namespace Server.Factions
{
    public class VendorList
    {
        public VendorList(VendorDefinition definition)
        {
            Definition = definition;
            Vendors = new List<BaseFactionVendor>();
        }

        public VendorDefinition Definition { get; }

        public List<BaseFactionVendor> Vendors { get; }

        public BaseFactionVendor Construct(Town town, Faction faction)
        {
            try
            {
                return ActivatorUtil.CreateInstance(Definition.Type, town, faction) as BaseFactionVendor;
            }
            catch
            {
                return null;
            }
        }
    }
}
