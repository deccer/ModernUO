using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Server.Items;
using Server.Json;
using Server.Utilities;

namespace Server.Commands
{
    public static class Categorization
    {
        private static CategoryEntry m_RootItems, m_RootMobiles;

        private static readonly Type typeofItem = typeof(Item);
        private static readonly Type typeofMobile = typeof(Mobile);
        private static readonly Type typeofConstructible = typeof(ConstructibleAttribute);

        public static CategoryEntry Items
        {
            get
            {
                if (m_RootItems == null)
                {
                    Load();
                }

                return m_RootItems;
            }
        }

        public static CategoryEntry Mobiles
        {
            get
            {
                if (m_RootMobiles == null)
                {
                    Load();
                }

                return m_RootMobiles;
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("RebuildCategorization", AccessLevel.Administrator, RebuildCategorization_OnCommand);
        }

        [Usage("RebuildCategorization")]
        [Description("Rebuilds the categorization data file used by the Add command.")]
        public static void RebuildCategorization_OnCommand(CommandEventArgs e)
        {
            var root = new CategoryEntry(null, "Add Menu", new[] { Items, Mobiles });

            var ceList = new List<CategoryEntry>();
            ceList.AddRange(root.SubCategories);

            Export(ceList, "Data/objects.json");

            e.Mobile.SendMessage("Categorization menu rebuilt.");
        }

        public static void Export(List<CategoryEntry> ceList, string fileName)
        {
            var list = new List<CAGJson>();
            foreach (var ce in ceList)
            {
                RecurseExport(list, ce, null);
            }

            JsonConfig.Serialize(fileName, list);
        }

        public static void RecurseExport(List<CAGJson> list, CategoryEntry ce, string category)
        {
            category = string.IsNullOrWhiteSpace(category) ? ce.Title : $"{category}{ce.Title}";

            if (ce.Matched.Count > 0)
            {
                list.Add(
                    new CAGJson
                    {
                        Category = category,
                        Objects = ce.Matched.Select(
                                cte =>
                                {
                                    if (cte.Object is Item item)
                                    {
                                        var itemID = item.ItemID;

                                        if (item is BaseAddon addon && addon.Components.Count == 1)
                                        {
                                            itemID = addon.Components[0].ItemID;
                                        }

                                        if (itemID > TileData.MaxItemValue)
                                        {
                                            itemID = 1;
                                        }

                                        int? hue = item.Hue & 0x7FFF;

                                        if ((hue & 0x4000) != 0)
                                        {
                                            hue = 0;
                                        }

                                        return new CAGObject
                                        {
                                            Type = cte.Type,
                                            ItemID = itemID,
                                            Hue = hue == 0 ? null : hue
                                        };
                                    }

                                    if (cte.Object is Mobile m)
                                    {
                                        var itemID = ShrinkTable.Lookup(m, 1);

                                        int? hue = m.Hue & 0x7FFF;

                                        if ((hue & 0x4000) != 0)
                                        {
                                            hue = 0;
                                        }

                                        return new CAGObject
                                        {
                                            Type = cte.Type,
                                            ItemID = itemID,
                                            Hue = hue == 0 ? null : hue
                                        };
                                    }

                                    throw new InvalidCastException(
                                        $"Categorization Type Entry: {cte.Type.Name} is not a valid type."
                                    );
                                }
                            )
                            .ToArray()
                    }
                );
            }

            var subCats = new List<CategoryEntry>(ce.SubCategories);

            subCats.Sort(new CategorySorter());

            for (var i = 0; i < subCats.Count; i++)
            {
                var subCat = subCats[i];
                RecurseExport(list, subCat, category);
            }
        }

        public static void Load()
        {
            var types = new List<Type>();

            AddTypes(Core.Assembly, types);

            for (var i = 0; i < AssemblyHandler.Assemblies.Length; ++i)
            {
                AddTypes(AssemblyHandler.Assemblies[i], types);
            }

            m_RootItems = Load(types, "Data/items.cfg");
            m_RootMobiles = Load(types, "Data/mobiles.cfg");
        }

        private static CategoryEntry Load(List<Type> types, string config)
        {
            var lines = CategoryLine.Load(config);

            if (lines.Length <= 0)
            {
                return new CategoryEntry();
            }

            var index = 0;
            var root = new CategoryEntry(null, lines, ref index);

            Fill(root, types);

            return root;
        }

        private static bool IsConstructible(Type type)
        {
            if (!type.IsSubclassOf(typeofItem) && !type.IsSubclassOf(typeofMobile))
            {
                return false;
            }

            var ctor = type.GetConstructor(Type.EmptyTypes);

            return ctor?.IsDefined(typeofConstructible, false) == true;
        }

        private static void AddTypes(Assembly asm, List<Type> types)
        {
            var allTypes = asm.GetTypes();

            for (var i = 0; i < allTypes.Length; ++i)
            {
                var type = allTypes[i];

                if (type.IsAbstract)
                {
                    continue;
                }

                if (IsConstructible(type))
                {
                    types.Add(type);
                }
            }
        }

        private static void Fill(CategoryEntry root, List<Type> list)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var type = list[i];
                var match = GetDeepestMatch(root, type);

                if (match == null)
                {
                    continue;
                }

                try
                {
                    match.Matched.Add(new CategoryTypeEntry(type));
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static CategoryEntry GetDeepestMatch(CategoryEntry root, Type type)
        {
            if (!root.IsMatch(type))
            {
                return null;
            }

            for (var i = 0; i < root.SubCategories.Length; ++i)
            {
                var check = GetDeepestMatch(root.SubCategories[i], type);

                if (check != null)
                {
                    return check;
                }
            }

            return root;
        }
    }

    public class CategorySorter : IComparer<CategoryEntry>
    {
        public int Compare(CategoryEntry x, CategoryEntry y)
        {
            var a = x?.Title;
            var b = y?.Title;

            return a switch
            {
                null when b == null => 0,
                null                => 1,
                _                   => a.CompareTo(b)
            };
        }
    }

    public class CategoryTypeSorter : IComparer<CategoryTypeEntry>
    {
        public int Compare(CategoryTypeEntry x, CategoryTypeEntry y)
        {
            var a = x?.Type.Name;
            var b = y?.Type.Name;

            return a switch
            {
                null when b == null => 0,
                null                => 1,
                _                   => a.CompareTo(b)
            };
        }
    }

    public class CategoryTypeEntry
    {
        public CategoryTypeEntry(Type type)
        {
            Type = type;
            Object = ActivatorUtil.CreateInstance(type);
        }

        public Type Type { get; }

        public object Object { get; }
    }

    public class CategoryEntry
    {
        public CategoryEntry(CategoryEntry parent = null, string title = "(empty)", CategoryEntry[] subCats = null)
        {
            Parent = parent;
            Title = title;
            SubCategories = subCats ?? Array.Empty<CategoryEntry>();
            Matches = Array.Empty<Type>();
            Matched = new List<CategoryTypeEntry>();
        }

        public CategoryEntry(CategoryEntry parent, CategoryLine[] lines, ref int index)
        {
            Parent = parent;

            var text = lines[index].Text;

            var start = text.IndexOf('(');

            if (start < 0)
            {
                throw new FormatException($"Input string not correctly formatted ('{text}')");
            }

            Title = text.Substring(0, start).Trim();

            var end = text.IndexOf(')', ++start);

            if (end < start)
            {
                throw new FormatException($"Input string not correctly formatted ('{text}')");
            }

            text = text.Substring(start, end - start);
            var split = text.Split(';');

            var list = new List<Type>();

            for (var i = 0; i < split.Length; ++i)
            {
                var type = AssemblyHandler.FindFirstTypeForName(split[i].Trim());

                if (type == null)
                {
                    Console.WriteLine("Match type not found ('{0}')", split[i].Trim());
                }
                else
                {
                    list.Add(type);
                }
            }

            Matches = list.ToArray();
            list.Clear();

            var ourIndentation = lines[index].Indentation;

            ++index;

            var entryList = new List<CategoryEntry>();

            while (index < lines.Length && lines[index].Indentation > ourIndentation)
            {
                entryList.Add(new CategoryEntry(this, lines, ref index));
            }

            SubCategories = entryList.ToArray();
            entryList.Clear();

            Matched = new List<CategoryTypeEntry>();
        }

        public string Title { get; }

        public Type[] Matches { get; }

        public CategoryEntry Parent { get; }

        public CategoryEntry[] SubCategories { get; }

        public List<CategoryTypeEntry> Matched { get; }

        public bool IsMatch(Type type)
        {
            var isMatch = false;

            for (var i = 0; !isMatch && i < Matches.Length; ++i)
            {
                isMatch = type == Matches[i] || type.IsSubclassOf(Matches[i]);
            }

            return isMatch;
        }
    }

    public class CategoryLine
    {
        public CategoryLine(string input)
        {
            int index;

            for (index = 0; index < input.Length; ++index)
            {
                if (char.IsLetter(input, index))
                {
                    break;
                }
            }

            if (index >= input.Length)
            {
                throw new FormatException($"Input string not correctly formatted ('{input}')");
            }

            Indentation = index;
            Text = input.Substring(index);
        }

        public int Indentation { get; }

        public string Text { get; }

        public static CategoryLine[] Load(string path)
        {
            var list = new List<CategoryLine>();

            if (File.Exists(path))
            {
                using var ip = new StreamReader(path);
                string line;

                while ((line = ip.ReadLine()) != null)
                {
                    list.Add(new CategoryLine(line));
                }
            }

            return list.ToArray();
        }
    }
}
