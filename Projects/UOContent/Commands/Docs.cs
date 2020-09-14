using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Server.Commands.Generic;
using Server.Engines.BulkOrders;
using Server.Items;
using Server.Network;

namespace Server.Commands
{
    public class Docs
    {
        private const int Iron = 0xCCCCDD;
        private const int DullCopper = 0xAAAAAA;
        private const int ShadowIron = 0x777799;
        private const int Copper = 0xDDCC99;
        private const int Bronze = 0xAA8866;
        private const int Gold = 0xDDCC55;
        private const int Agapite = 0xDDAAAA;
        private const int Verite = 0x99CC77;
        private const int Valorite = 0x88AABB;

        private const int Cloth = 0xDDDDDD;
        private const int Plain = 0xCCAA88;
        private const int SpinedAOS = 0x99BBBB;
        private const int HornedAOS = 0xCC8888;
        private const int BarbedAOS = 0xAABBAA;
        private const int SpinedLBR = 0xAA8833;
        private const int HornedLBR = 0xBBBBAA;
        private const int BarbedLBR = 0xCCAA88;

        private const string HtmlNewLine = "&#13;";

        private const string RefString = "<font color=\"blue\">ref</font> ";
        private const string GetString = " <font color=\"blue\">get</font>;";
        private const string SetString = " <font color=\"blue\">set</font>;";

        private const string InString = "<font color=\"blue\">in</font> ";
        private const string OutString = "<font color=\"blue\">out</font> ";

        private const string VirtString = "<font color=\"blue\">virtual</font> ";
        private const string CtorString = "(<font color=\"blue\">ctor</font>) ";
        private const string StaticString = "(<font color=\"blue\">static</font>) ";
        private static Dictionary<Type, TypeInfo> m_Types;
        private static Dictionary<string, List<TypeInfo>> m_Namespaces;

        private static readonly char[] ReplaceChars = "<>".ToCharArray();

        private static readonly string m_RootDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

        private static readonly string[,] m_Aliases =
        {
            { "System.Object", "<font color=\"blue\">object</font>" },
            { "System.String", "<font color=\"blue\">string</font>" },
            { "System.Boolean", "<font color=\"blue\">bool</font>" },
            { "System.Byte", "<font color=\"blue\">byte</font>" },
            { "System.SByte", "<font color=\"blue\">sbyte</font>" },
            { "System.Int16", "<font color=\"blue\">short</font>" },
            { "System.UInt16", "<font color=\"blue\">ushort</font>" },
            { "System.Int32", "<font color=\"blue\">int</font>" },
            { "System.UInt32", "<font color=\"blue\">uint</font>" },
            { "System.Int64", "<font color=\"blue\">long</font>" },
            { "System.UInt64", "<font color=\"blue\">ulong</font>" },
            { "System.Single", "<font color=\"blue\">float</font>" },
            { "System.Double", "<font color=\"blue\">double</font>" },
            { "System.Decimal", "<font color=\"blue\">decimal</font>" },
            { "System.Char", "<font color=\"blue\">char</font>" },
            { "System.Void", "<font color=\"blue\">void</font>" }
        };

        private static readonly int m_AliasLength = m_Aliases.GetLength(0);

        private static readonly Type typeofItem = typeof(Item);
        private static readonly Type typeofMobile = typeof(Mobile);
        private static readonly Type typeofMap = typeof(Map);
        private static readonly Type typeofCustomEnum = typeof(CustomEnumAttribute);

        private static readonly object[,] m_Tooltips =
        {
            { typeof(byte), "Numeric value in the range from 0 to 255, inclusive." },
            { typeof(sbyte), "Numeric value in the range from negative 128 to positive 127, inclusive." },
            { typeof(ushort), "Numeric value in the range from 0 to 65,535, inclusive." },
            { typeof(short), "Numeric value in the range from negative 32,768 to positive 32,767, inclusive." },
            { typeof(uint), "Numeric value in the range from 0 to 4,294,967,295, inclusive." },
            { typeof(int), "Numeric value in the range from negative 2,147,483,648 to positive 2,147,483,647, inclusive." },
            { typeof(ulong), "Numeric value in the range from 0 through about 10^20." },
            { typeof(long), "Numeric value in the approximate range from negative 10^19 through 10^19." },
            {
                typeof(string),
                "Text value. To specify a value containing spaces, encapsulate the value in quote characters:{0}{0}&quot;Spaced text example&quot;"
            },
            { typeof(bool), "Boolean value which can be either True or False." },
            { typeof(Map), "Map or facet name. Possible values include:{0}{0}- Felucca{0}- Trammel{0}- Ilshenar{0}- Malas" },
            {
                typeof(Poison),
                "Poison name or level. Possible values include:{0}{0}- Lesser{0}- Regular{0}- Greater{0}- Deadly{0}- Lethal"
            },
            {
                typeof(Point3D),
                "Three-dimensional coordinate value. Format as follows:{0}{0}&quot;(<x value>, <y value>, <z value>)&quot;"
            }
        };

        public static void Initialize()
        {
            CommandSystem.Register("DocGen", AccessLevel.Administrator, DocGen_OnCommand);
        }

        [Usage("DocGen")]
        [Description("Generates RunUO documentation.")]
        private static void DocGen_OnCommand(CommandEventArgs e)
        {
            World.Broadcast(0x35, true, "Documentation is being generated, please wait.");
            Console.WriteLine("Documentation is being generated, please wait.");

            NetState.Pause();

            var startTime = DateTime.UtcNow;

            var generated = Document();

            var endTime = DateTime.UtcNow;

            NetState.Resume();

            if (generated)
            {
                World.Broadcast(
                    0x35,
                    true,
                    "Documentation has been completed. The entire process took {0:F1} seconds.",
                    (endTime - startTime).TotalSeconds
                );
                Console.WriteLine("Documentation complete.");
            }
            else
            {
                World.Broadcast(
                    0x35,
                    true,
                    "Docmentation failed: Documentation directories are locked and in use. Please close all open files and directories and try again."
                );
                Console.WriteLine("Documentation failed.");
            }
        }

        private static void LoadTypes(Assembly a, Assembly[] asms)
        {
            var types = a.GetTypes();

            for (var i = 0; i < types.Length; ++i)
            {
                var type = types[i];

                var nspace = type.Namespace;

                if (nspace == null || type.IsSpecialName)
                {
                    continue;
                }

                var info = new TypeInfo(type);
                m_Types[type] = info;

                if (!m_Namespaces.TryGetValue(nspace, out var nspaces))
                {
                    m_Namespaces[nspace] = nspaces = new List<TypeInfo>();
                }

                nspaces.Add(info);

                var baseType = info.m_BaseType;

                if (baseType != null && InAssemblies(baseType, asms))
                {
                    m_Types.TryGetValue(baseType, out var baseInfo);

                    if (baseInfo == null)
                    {
                        m_Types[baseType] = baseInfo = new TypeInfo(baseType);
                    }

                    baseInfo.m_Derived ??= new List<TypeInfo>();

                    baseInfo.m_Derived.Add(info);
                }

                var decType = info.m_Declaring;

                if (decType != null)
                {
                    m_Types.TryGetValue(decType, out var decInfo);

                    if (decInfo == null)
                    {
                        m_Types[decType] = decInfo = new TypeInfo(decType);
                    }

                    decInfo.m_Nested ??= new List<TypeInfo>();

                    decInfo.m_Nested.Add(info);
                }

                for (var j = 0; j < info.m_Interfaces.Length; ++j)
                {
                    var iface = info.m_Interfaces[j];

                    if (!InAssemblies(iface, asms))
                    {
                        continue;
                    }

                    m_Types.TryGetValue(iface, out var ifaceInfo);

                    if (ifaceInfo == null)
                    {
                        m_Types[iface] = ifaceInfo = new TypeInfo(iface);
                    }

                    ifaceInfo.m_Derived ??= new List<TypeInfo>();

                    ifaceInfo.m_Derived.Add(info);
                }
            }
        }

        private static bool InAssemblies(Type t, Assembly[] asms)
        {
            var a = t.Assembly;

            for (var i = 0; i < asms.Length; ++i)
            {
                if (a == asms[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static void DocumentLoadedTypes()
        {
            using var indexHtml = GetWriter("docs/", "overview.html");
            indexHtml.WriteLine("<html>");
            indexHtml.WriteLine("   <head>");
            indexHtml.WriteLine("      <title>RunUO Documentation - Class Overview</title>");
            indexHtml.WriteLine("   </head>");
            indexHtml.WriteLine(
                "   <body bgcolor=\"white\" style=\"font-family: Courier New\" text=\"#000000\" link=\"#000000\" vlink=\"#000000\" alink=\"#808080\">"
            );
            indexHtml.WriteLine("      <h4><a href=\"index.html\">Back to the index</a></h4>");
            indexHtml.WriteLine("      <h2>Namespaces</h2>");

            var nspaces = new SortedList<string, List<TypeInfo>>(m_Namespaces);

            foreach (var kvp in nspaces)
            {
                kvp.Value.Sort(new TypeComparer());

                SaveNamespace(kvp.Key, kvp.Value, indexHtml);
            }

            indexHtml.WriteLine("   </body>");
            indexHtml.WriteLine("</html>");
        }

        private static void SaveNamespace(string name, List<TypeInfo> types, StreamWriter indexHtml)
        {
            var fileName = GetFileName("docs/namespaces/", name, ".html");

            indexHtml.WriteLine("      <a href=\"namespaces/{0}\">{1}</a><br>", fileName, name);

            using var nsHtml = GetWriter("docs/namespaces/", fileName);
            nsHtml.WriteLine("<html>");
            nsHtml.WriteLine("   <head>");
            nsHtml.WriteLine("      <title>RunUO Documentation - Class Overview - {0}</title>", name);
            nsHtml.WriteLine("   </head>");
            nsHtml.WriteLine(
                "   <body bgcolor=\"white\" style=\"font-family: Courier New\" text=\"#000000\" link=\"#000000\" vlink=\"#000000\" alink=\"#808080\">"
            );
            nsHtml.WriteLine("      <h4><a href=\"../overview.html\">Back to the namespace index</a></h4>");
            nsHtml.WriteLine("      <h2>{0}</h2>", name);

            for (var i = 0; i < types.Count; ++i)
            {
                SaveType(types[i], nsHtml, fileName, name);
            }

            nsHtml.WriteLine("   </body>");
            nsHtml.WriteLine("</html>");
        }

        private static void SaveType(TypeInfo info, StreamWriter nsHtml, string nsFileName, string nsName)
        {
            if (info.m_Declaring == null)
            {
                nsHtml.WriteLine($"      <!-- DBG-ST -->{info.LinkName("../types/")}<br>");
            }

            using var typeHtml = GetWriter(info.FileName);
            typeHtml.WriteLine("<html>");
            typeHtml.WriteLine("   <head>");
            typeHtml.WriteLine("      <title>RunUO Documentation - Class Overview - {0}</title>", info.TypeName);
            typeHtml.WriteLine("   </head>");
            typeHtml.WriteLine(
                "   <body bgcolor=\"white\" style=\"font-family: Courier New\" text=\"#000000\" link=\"#000000\" vlink=\"#000000\" alink=\"#808080\">"
            );
            typeHtml.WriteLine("      <h4><a href=\"../namespaces/{0}\">Back to {1}</a></h4>", nsFileName, nsName);

            if (info.m_Type.IsEnum)
            {
                WriteEnum(info, typeHtml);
            }
            else
            {
                WriteType(info, typeHtml);
            }

            typeHtml.WriteLine("   </body>");
            typeHtml.WriteLine("</html>");
        }

        public static void FormatGeneric(Type type, out string typeName, out string fileName, out string linkName)
        {
            string name = null;
            string fnam = null;
            string link = null;

            if (type.IsGenericType)
            {
                var index = type.Name.IndexOf('`');

                if (index > 0)
                {
                    var rootType = type.Name.Substring(0, index);

                    var nameBuilder = new StringBuilder(rootType);
                    var fnamBuilder = new StringBuilder($"docs/types/{SanitizeType(rootType)}");
                    StringBuilder linkBuilder;
                    linkBuilder = DontLink(type)
                        ? new StringBuilder($"<font color=\"blue\">{rootType}</font>")
                        : new StringBuilder($"<a href=\"@directory@{rootType}-T-.html\">{rootType}</a>");

                    nameBuilder.Append("&lt;");
                    fnamBuilder.Append("-");
                    linkBuilder.Append("&lt;");

                    var typeArguments = type.GetGenericArguments();

                    for (var i = 0; i < typeArguments.Length; i++)
                    {
                        if (i != 0)
                        {
                            nameBuilder.Append(',');
                            fnamBuilder.Append(',');
                            linkBuilder.Append(',');
                        }

                        var sanitizedName = SanitizeType(typeArguments[i].Name);
                        var aliasedName = AliasForName(sanitizedName);

                        nameBuilder.Append(sanitizedName);
                        fnamBuilder.Append("T");
                        if (DontLink(typeArguments[i]))
                        {
                            linkBuilder.Append($"<font color=\"blue\">{aliasedName}</font>");
                        }
                        else
                        {
                            linkBuilder.Append(
                                $"<a href=\"@directory@{aliasedName}.html\">{aliasedName}</a>"
                            );
                        }
                    }

                    nameBuilder.Append("&gt;");
                    fnamBuilder.Append("-");
                    linkBuilder.Append("&gt;");

                    name = nameBuilder.ToString();
                    fnam = fnamBuilder.ToString();
                    link = linkBuilder.ToString();
                }
            }

            typeName = name ?? type.Name;

            fileName = fnam == null ? $"docs/types/{SanitizeType(type.Name)}.html" : $"{fnam}.html";

            if (link == null)
            {
                linkName = DontLink(type)
                    ? $"<font color=\"blue\">{SanitizeType(type.Name)}</font>"
                    : $"<a href=\"@directory@{SanitizeType(type.Name)}.html\">{SanitizeType(type.Name)}</a>";
            }
            else
            {
                linkName = link;
            }

            // Console.WriteLine( typeName+":"+fileName+":"+linkName );
        }

        public static string SanitizeType(string name)
        {
            var anonymousType = name.Contains("<");
            var sb = new StringBuilder(name);
            for (var i = 0; i < ReplaceChars.Length; ++i)
            {
                sb.Replace(ReplaceChars[i], '-');
            }

            if (anonymousType)
            {
                return $"(Anonymous-Type){sb}";
            }

            return sb.ToString();
        }

        public static string AliasForName(string name)
        {
            for (var i = 0; i < m_AliasLength; ++i)
            {
                if (m_Aliases[i, 0] == name)
                {
                    return m_Aliases[i, 1];
                }
            }

            return name;
        }

        /*
        // For stuff we don't want to links to
        private static string[] m_DontLink = new string[]
        {
            "List",
            "Stack",
            "Queue",
            "Dictionary",
            "LinkedList",
            "SortedList",
            "SortedDictionary",
            "IComparable",
            "IComparer",
            "ICloneable",
            "Type"
        };
    
        public static bool DontLink( string name )
        {
          foreach( string dontLink in m_DontLink )
            if (dontLink == name ) return true;
          return false;
        }
        */
        public static bool DontLink(Type type)
        {
            if (type.Name == "T" || string.IsNullOrEmpty(type.Namespace) || m_Namespaces == null)
            {
                return true;
            }

            if (type.Namespace.StartsWith("Server"))
            {
                return false;
            }

            return !m_Namespaces.ContainsKey(type.Namespace);
        }

        public static string GetFileName(string root, string name, string ext)
        {
            if (name.IndexOfAny(ReplaceChars) >= 0)
            {
                var sb = new StringBuilder(name);

                for (var i = 0; i < ReplaceChars.Length; ++i)
                {
                    sb.Replace(ReplaceChars[i], '-');
                }

                name = sb.ToString();
            }

            var index = 0;
            var file = string.Concat(name, ext);

            while (File.Exists(Path.Combine(root, file)))
            {
                file = string.Concat(name, ++index, ext);
            }

            return file;
        }

        private static void EnsureDirectory(string path)
        {
            path = Path.Combine(m_RootDirectory, path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void DeleteDirectory(string path)
        {
            path = Path.Combine(m_RootDirectory, path);

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static StreamWriter GetWriter(string root, string name) =>
            new StreamWriter(Path.Combine(Path.Combine(m_RootDirectory, root), name));

        private static StreamWriter GetWriter(string path) => new StreamWriter(Path.Combine(m_RootDirectory, path));

        public static string GetPair(Type varType, string name, bool ignoreRef)
        {
            var prepend = "";
            var append = new StringBuilder();

            var realType = varType;

            if (varType.IsByRef)
            {
                if (!ignoreRef)
                {
                    prepend = RefString;
                }

                realType = varType.GetElementType();
            }

            if (realType?.IsPointer == true)
            {
                if (realType.IsArray)
                {
                    append.Append('*');

                    do
                    {
                        append.Append('[');

                        for (var i = 1; i < realType.GetArrayRank(); ++i)
                        {
                            append.Append(',');
                        }

                        append.Append(']');

                        realType = realType.GetElementType();
                    } while (realType?.IsArray == true);

                    append.Append(' ');
                }
                else
                {
                    realType = realType.GetElementType();
                    append.Append(" *");
                }
            }
            else if (realType?.IsArray == true)
            {
                do
                {
                    append.Append('[');

                    for (var i = 1; i < realType.GetArrayRank(); ++i)
                    {
                        append.Append(',');
                    }

                    append.Append(']');

                    realType = realType.GetElementType();
                } while (realType?.IsArray == true);

                append.Append(' ');
            }
            else
            {
                append.Append(' ');
            }

            var fullName = realType?.FullName ?? "(-null-)";
            string aliased = null; // = realType.Name;

            if (realType != null && m_Types.TryGetValue(realType, out var info))
            {
                aliased = $"<!-- DBG-0 -->{info.LinkName(null)}";
            }
            else
            {
                if (realType?.IsGenericType == true)
                {
                    FormatGeneric(realType, out _, out _, out var linkName);
                    aliased = linkName.Replace("@directory@", null);
                }
                else
                {
                    for (var i = 0; i < m_AliasLength; ++i)
                    {
                        if (m_Aliases[i, 0] == fullName)
                        {
                            aliased = m_Aliases[i, 1];
                            break;
                        }
                    }
                }

                aliased ??= realType?.Name ?? "";
            }

            return string.Concat(prepend, aliased, append, name);
        }

        private static bool Document()
        {
            try
            {
                DeleteDirectory("docs/");
            }
            catch
            {
                return false;
            }

            EnsureDirectory("docs/");
            EnsureDirectory("docs/namespaces/");
            EnsureDirectory("docs/types/");
            EnsureDirectory("docs/bods/");

            GenerateStyles();
            GenerateIndex();

            DocumentCommands();
            DocumentKeywords();
            DocumentBodies();

            DocumentBulkOrders();

            m_Types = new Dictionary<Type, TypeInfo>();
            m_Namespaces = new Dictionary<string, List<TypeInfo>>();

            var assemblies = new List<Assembly> { Core.Assembly };

            foreach (var asm in AssemblyHandler.Assemblies)
            {
                assemblies.Add(asm);
            }

            var asms = assemblies.ToArray();

            for (var i = 0; i < asms.Length; ++i)
            {
                LoadTypes(asms[i], asms);
            }

            DocumentLoadedTypes();
            DocumentConstructibleObjects();

            return true;
        }

        private static void AddIndexLink(StreamWriter html, string filePath, string label, string desc)
        {
            html.WriteLine("      <h2><a href=\"{0}\" title=\"{1}\">{2}</a></h2>", filePath, desc, label);
        }

        private static void GenerateStyles()
        {
            using var css = GetWriter("docs/", "styles.css");
            css.WriteLine("body { background-color: #FFFFFF; font-family: verdana, arial; font-size: 11px; }");
            css.WriteLine("a { color: #28435E; }");
            css.WriteLine("a:hover { color: #4878A9; }");
            css.WriteLine("td.header { background-color: #9696AA; font-weight: bold; font-size: 12px; }");
            css.WriteLine("td.lentry { background-color: #D7D7EB; width: 10%; }");
            css.WriteLine("td.rentry { background-color: #FFFFFF; width: 90%; }");
            css.WriteLine("td.entry { background-color: #FFFFFF; }");
            css.WriteLine("td { font-size: 11px; }");
            css.WriteLine(".tbl-border { background-color: #46465A; }");

            css.WriteLine("td.ir {{ background-color: #{0:X6}; }}", Iron);
            css.WriteLine("td.du {{ background-color: #{0:X6}; }}", DullCopper);
            css.WriteLine("td.sh {{ background-color: #{0:X6}; }}", ShadowIron);
            css.WriteLine("td.co {{ background-color: #{0:X6}; }}", Copper);
            css.WriteLine("td.br {{ background-color: #{0:X6}; }}", Bronze);
            css.WriteLine("td.go {{ background-color: #{0:X6}; }}", Gold);
            css.WriteLine("td.ag {{ background-color: #{0:X6}; }}", Agapite);
            css.WriteLine("td.ve {{ background-color: #{0:X6}; }}", Verite);
            css.WriteLine("td.va {{ background-color: #{0:X6}; }}", Valorite);

            css.WriteLine("td.cl {{ background-color: #{0:X6}; }}", Cloth);
            css.WriteLine("td.pl {{ background-color: #{0:X6};  }}", Plain);
            css.WriteLine("td.sp {{ background-color: #{0:X6}; }}", Core.AOS ? SpinedAOS : SpinedLBR);
            css.WriteLine("td.ho {{ background-color: #{0:X6}; }}", Core.AOS ? HornedAOS : HornedLBR);
            css.WriteLine("td.ba {{ background-color: #{0:X6}; }}", Core.AOS ? BarbedAOS : BarbedLBR);
        }

        private static void GenerateIndex()
        {
            using var html = GetWriter("docs/", "index.html");
            html.WriteLine("<html>");
            html.WriteLine("   <head>");
            html.WriteLine("      <title>RunUO Documentation - Index</title>");
            html.WriteLine("      <link rel=\"stylesheet\" type=\"text/css\" href=\"styles.css\" />");
            html.WriteLine("   </head>");
            html.WriteLine("   <body>");

            AddIndexLink(
                html,
                "commands.html",
                "Commands",
                "Every available command. This contains command name, usage, aliases, and description."
            );
            AddIndexLink(
                html,
                "objects.html",
                "Constructible Objects",
                "Every constructible item or npc. This contains object name and usage. Hover mouse over parameters to see type description."
            );
            AddIndexLink(
                html,
                "keywords.html",
                "Speech Keywords",
                "Lists speech keyword numbers and associated match patterns. These are used in some scripts for multi-language matching of client speech."
            );
            AddIndexLink(
                html,
                "bodies.html",
                "Body List",
                "Every usable body number and name. Table is generated from a UO:3D client datafile. If you do not have UO:3D installed, this may be blank."
            );
            AddIndexLink(
                html,
                "overview.html",
                "Class Overview",
                "Scripting reference. Contains every class type and contained methods in the core and scripts."
            );
            AddIndexLink(
                html,
                "bods/bod_smith_rewards.html",
                "Bulk Order Rewards: Smithing",
                "Reference table for large and small smithing bulk order deed rewards."
            );
            AddIndexLink(
                html,
                "bods/bod_tailor_rewards.html",
                "Bulk Order Rewards: Tailoring",
                "Reference table for large and small tailoring bulk order deed rewards."
            );

            html.WriteLine("   </body>");
            html.WriteLine("</html>");
        }

        private static void DocumentBulkOrders()
        {
            using (var html = GetWriter("docs/bods/", "bod_smith_rewards.html"))
            {
                html.WriteLine("<html>");
                html.WriteLine("   <head>");
                html.WriteLine("      <title>RunUO Documentation - Bulk Orders - Smith Rewards</title>");
                html.WriteLine("      <link rel=\"stylesheet\" type=\"text/css\" href=\"../styles.css\" />");
                html.WriteLine("   </head>");
                html.WriteLine("   <body>");

                SmallBOD sbod = new SmallSmithBOD();

                sbod.Type = typeof(Katana);
                sbod.Material = BulkMaterialType.None;
                sbod.AmountMax = 10;

                WriteSmithBODHeader(html, "(Small) Weapons");
                sbod.RequireExceptional = false;
                DocumentSmithBOD(html, sbod.ComputeRewards(true), "10, 15, 20: Normal", sbod.Material);
                sbod.RequireExceptional = true;
                DocumentSmithBOD(html, sbod.ComputeRewards(true), "10, 15, 20: Exceptional", sbod.Material);
                WriteSmithBODFooter(html);

                html.WriteLine("      <br><br>");
                html.WriteLine("      <br><br>");

                sbod.Type = typeof(PlateArms);

                WriteSmithBODHeader(html, "(Small) Armor: Normal");

                sbod.RequireExceptional = false;
                for (var mat = BulkMaterialType.None; mat <= BulkMaterialType.Valorite; ++mat)
                {
                    sbod.Material = mat;
                    sbod.AmountMax = 10;
                    DocumentSmithBOD(html, sbod.ComputeRewards(true), "10, 15, 20", sbod.Material);
                }

                WriteSmithBODFooter(html);

                html.WriteLine("      <br><br>");

                WriteSmithBODHeader(html, "(Small) Armor: Exceptional");

                sbod.RequireExceptional = true;
                for (var mat = BulkMaterialType.None; mat <= BulkMaterialType.Valorite; ++mat)
                {
                    sbod.Material = mat;

                    for (var amt = 15; amt <= 20; amt += 5)
                    {
                        sbod.AmountMax = amt;
                        DocumentSmithBOD(html, sbod.ComputeRewards(true), amt == 20 ? "20" : "10, 15", sbod.Material);
                    }
                }

                WriteSmithBODFooter(html);

                html.WriteLine("      <br><br>");
                html.WriteLine("      <br><br>");

                sbod.Delete();

                WriteSmithLBOD(html, "Ringmail", LargeBulkEntry.LargeRing);
                WriteSmithLBOD(html, "Chainmail", LargeBulkEntry.LargeChain);
                WriteSmithLBOD(html, "Platemail", LargeBulkEntry.LargePlate);

                html.WriteLine("   </body>");
                html.WriteLine("</html>");
            }

            using (var html = GetWriter("docs/bods/", "bod_tailor_rewards.html"))
            {
                html.WriteLine("<html>");
                html.WriteLine("   <head>");
                html.WriteLine("      <title>RunUO Documentation - Bulk Orders - Tailor Rewards</title>");
                html.WriteLine("      <link rel=\"stylesheet\" type=\"text/css\" href=\"../styles.css\" />");
                html.WriteLine("   </head>");
                html.WriteLine("   <body>");

                SmallBOD sbod = new SmallTailorBOD();

                WriteTailorBODHeader(html, "Small Bulk Order");

                html.WriteLine("         <tr>");
                html.WriteLine("            <td width=\"850\" colspan=\"21\" class=\"entry\"><b>Regular: 10, 15</b></td>");
                html.WriteLine("         </tr>");

                sbod.AmountMax = 10;
                sbod.RequireExceptional = false;

                sbod.Type = typeof(SkullCap);
                sbod.Material = BulkMaterialType.None;
                DocumentTailorBOD(html, sbod.ComputeRewards(true), "10, 15", sbod.Material, sbod.Type);

                sbod.Type = typeof(LeatherCap);
                for (var mat = BulkMaterialType.None; mat <= BulkMaterialType.Barbed; ++mat)
                {
                    if (mat >= BulkMaterialType.DullCopper && mat <= BulkMaterialType.Valorite)
                    {
                        continue;
                    }

                    sbod.Material = mat;
                    DocumentTailorBOD(html, sbod.ComputeRewards(true), "10, 15", sbod.Material, sbod.Type);
                }

                html.WriteLine("         <tr>");
                html.WriteLine("            <td width=\"850\" colspan=\"21\" class=\"entry\"><b>Regular: 20</b></td>");
                html.WriteLine("         </tr>");

                sbod.AmountMax = 20;
                sbod.RequireExceptional = false;

                sbod.Type = typeof(SkullCap);
                sbod.Material = BulkMaterialType.None;
                DocumentTailorBOD(html, sbod.ComputeRewards(true), "20", sbod.Material, sbod.Type);

                sbod.Type = typeof(LeatherCap);
                for (var mat = BulkMaterialType.None; mat <= BulkMaterialType.Barbed; ++mat)
                {
                    if (mat >= BulkMaterialType.DullCopper && mat <= BulkMaterialType.Valorite)
                    {
                        continue;
                    }

                    sbod.Material = mat;
                    DocumentTailorBOD(html, sbod.ComputeRewards(true), "20", sbod.Material, sbod.Type);
                }

                html.WriteLine("         <tr>");
                html.WriteLine(
                    "            <td width=\"850\" colspan=\"21\" class=\"entry\"><b>Exceptional: 10, 15</b></td>"
                );
                html.WriteLine("         </tr>");

                sbod.AmountMax = 10;
                sbod.RequireExceptional = true;

                sbod.Type = typeof(SkullCap);
                sbod.Material = BulkMaterialType.None;
                DocumentTailorBOD(html, sbod.ComputeRewards(true), "10, 15", sbod.Material, sbod.Type);

                sbod.Type = typeof(LeatherCap);
                for (var mat = BulkMaterialType.None; mat <= BulkMaterialType.Barbed; ++mat)
                {
                    if (mat >= BulkMaterialType.DullCopper && mat <= BulkMaterialType.Valorite)
                    {
                        continue;
                    }

                    sbod.Material = mat;
                    DocumentTailorBOD(html, sbod.ComputeRewards(true), "10, 15", sbod.Material, sbod.Type);
                }

                html.WriteLine("         <tr>");
                html.WriteLine("            <td width=\"850\" colspan=\"21\" class=\"entry\"><b>Exceptional: 20</b></td>");
                html.WriteLine("         </tr>");

                sbod.AmountMax = 20;
                sbod.RequireExceptional = true;

                sbod.Type = typeof(SkullCap);
                sbod.Material = BulkMaterialType.None;
                DocumentTailorBOD(html, sbod.ComputeRewards(true), "20", sbod.Material, sbod.Type);

                sbod.Type = typeof(LeatherCap);
                for (var mat = BulkMaterialType.None; mat <= BulkMaterialType.Barbed; ++mat)
                {
                    if (mat >= BulkMaterialType.DullCopper && mat <= BulkMaterialType.Valorite)
                    {
                        continue;
                    }

                    sbod.Material = mat;
                    DocumentTailorBOD(html, sbod.ComputeRewards(true), "20", sbod.Material, sbod.Type);
                }

                WriteTailorBODFooter(html);

                html.WriteLine("      <br><br>");
                html.WriteLine("      <br><br>");

                sbod.Delete();

                WriteTailorLBOD(html, "Large Bulk Order: 4-part", LargeBulkEntry.Gypsy, true, true);
                WriteTailorLBOD(html, "Large Bulk Order: 5-part", LargeBulkEntry.TownCrier, true, true);
                WriteTailorLBOD(html, "Large Bulk Order: 6-part", LargeBulkEntry.MaleLeatherSet, false, true);

                html.WriteLine("   </body>");
                html.WriteLine("</html>");
            }
        }

        private static void WriteTailorLBOD(
            StreamWriter html, string name, SmallBulkEntry[] entries, bool expandCloth,
            bool expandPlain
        )
        {
            WriteTailorBODHeader(html, name);

            LargeBOD lbod = new LargeTailorBOD();

            lbod.Entries = LargeBulkEntry.ConvertEntries(lbod, entries);

            var type = entries[0].Type;

            var showCloth = !(type.IsSubclassOf(typeof(BaseArmor)) || type.IsSubclassOf(typeof(BaseShoes)));

            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"850\" colspan=\"21\" class=\"entry\"><b>Regular</b></td>");
            html.WriteLine("         </tr>");

            lbod.RequireExceptional = false;
            lbod.AmountMax = 10;

            if (showCloth)
            {
                lbod.Material = BulkMaterialType.None;

                if (expandCloth)
                {
                    lbod.AmountMax = 10;
                    DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15", lbod.Material, type);
                    lbod.AmountMax = 20;
                    DocumentTailorBOD(html, lbod.ComputeRewards(true), "20", lbod.Material, type);
                }
                else
                {
                    lbod.AmountMax = 10;
                    DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15, 20", lbod.Material, type);
                }
            }

            lbod.Material = BulkMaterialType.None;

            if (expandPlain)
            {
                lbod.AmountMax = 10;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15, 20", lbod.Material, typeof(LeatherCap));
                lbod.AmountMax = 20;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "20", lbod.Material, typeof(LeatherCap));
            }
            else
            {
                lbod.AmountMax = 10;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15, 20", lbod.Material, typeof(LeatherCap));
            }

            for (var mat = BulkMaterialType.Spined; mat <= BulkMaterialType.Barbed; ++mat)
            {
                lbod.Material = mat;
                lbod.AmountMax = 10;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15", lbod.Material, type);
                lbod.AmountMax = 20;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "20", lbod.Material, type);
            }

            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"850\" colspan=\"21\" class=\"entry\"><b>Exceptional</b></td>");
            html.WriteLine("         </tr>");

            lbod.RequireExceptional = true;
            lbod.AmountMax = 10;

            if (showCloth)
            {
                lbod.Material = BulkMaterialType.None;

                if (expandCloth)
                {
                    lbod.AmountMax = 10;
                    DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15", lbod.Material, type);
                    lbod.AmountMax = 20;
                    DocumentTailorBOD(html, lbod.ComputeRewards(true), "20", lbod.Material, type);
                }
                else
                {
                    lbod.AmountMax = 10;
                    DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15, 20", lbod.Material, type);
                }
            }

            lbod.Material = BulkMaterialType.None;

            if (expandPlain)
            {
                lbod.AmountMax = 10;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15, 20", lbod.Material, typeof(LeatherCap));
                lbod.AmountMax = 20;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "20", lbod.Material, typeof(LeatherCap));
            }
            else
            {
                lbod.AmountMax = 10;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15, 20", lbod.Material, typeof(LeatherCap));
            }

            for (var mat = BulkMaterialType.Spined; mat <= BulkMaterialType.Barbed; ++mat)
            {
                lbod.Material = mat;
                lbod.AmountMax = 10;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "10, 15", lbod.Material, type);
                lbod.AmountMax = 20;
                DocumentTailorBOD(html, lbod.ComputeRewards(true), "20", lbod.Material, type);
            }

            WriteTailorBODFooter(html);

            html.WriteLine("      <br><br>");
            html.WriteLine("      <br><br>");
        }

        private static void WriteTailorBODHeader(StreamWriter html, string title)
        {
            html.WriteLine("      <table width=\"850\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
            html.WriteLine("      <tr><td class=\"tbl-border\">");
            html.WriteLine("      <table border=\"0\" width=\"850\" cellpadding=\"0\" cellspacing=\"1\">");
            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"250\" rowspan=\"2\" class=\"entry\"><center>{0}</center></td>", title);
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_1.jpg\" alt=\"Colored Cloth (Level 1)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_2.jpg\" alt=\"Colored Cloth (Level 2)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_3.jpg\" alt=\"Colored Cloth (Level 3)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_4.jpg\" alt=\"Colored Cloth (Level 4)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_5.jpg\" alt=\"Colored Cloth (Level 5)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_sandals_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_sandals.jpg\" alt=\"Colored Sandals\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"100\" colspan=\"4\" class=\"entry\"><center>Power Scrolls</center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_smallhides.jpg\" alt=\"Small Stretched Hide\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_mediumhides.jpg\" alt=\"Medium Stretched Hide\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_lighttapestry.jpg\" alt=\"Light Flower Tapestry\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_darktapestry.jpg\" alt=\"Dark Flower Tapestry\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_brownbearrug.jpg\" alt=\"Brown Bear Rug\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_polarbearrug.jpg\" alt=\"Polar Bear Rug\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_clothingbless.jpg\" alt=\"Clothing Bless Deed\"></center></td>"
            );
            html.WriteLine("            <td width=\"75\" colspan=\"3\" class=\"entry\"><center>Runic Kits</center></td>");
            html.WriteLine("         </tr>");
            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+5</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+10</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+15</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+20</small></center></td>");
            html.WriteLine(
                "            <td width=\"25\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_runic_spined.jpg\" alt=\"Runic Sewing Kit: Spined\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_runic_horned.jpg\" alt=\"Runic Sewing Kit: Horned\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_runic_barbed.jpg\" alt=\"Runic Sewing Kit: Barbed\"></center></td>"
            );
            html.WriteLine("         </tr>");
        }

        private static void WriteTailorBODFooter(StreamWriter html)
        {
            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"250\" rowspan=\"2\" class=\"entry\">&nbsp;</td>");
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_1.jpg\" alt=\"Colored Cloth (Level 1)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_2.jpg\" alt=\"Colored Cloth (Level 2)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_3.jpg\" alt=\"Colored Cloth (Level 3)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_4.jpg\" alt=\"Colored Cloth (Level 4)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_cloth_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_cloth_5.jpg\" alt=\"Colored Cloth (Level 5)\" border=\"0\"></a></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><a href=\"http://www.runuo.com/images/bodreward_sandals_full.jpg\"><img src=\"http://www.runuo.com/images/bodreward_sandals.jpg\" alt=\"Colored Sandals\" border=\"0\"></a></center></td>"
            );
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+5</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+10</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+15</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+20</small></center></td>");
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_smallhides.jpg\" alt=\"Small Stretched Hide\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_mediumhides.jpg\" alt=\"Medium Stretched Hide\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_lighttapestry.jpg\" alt=\"Light Flower Tapestry\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_darktapestry.jpg\" alt=\"Dark Flower Tapestry\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_brownbearrug.jpg\" alt=\"Brown Bear Rug\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_polarbearrug.jpg\" alt=\"Polar Bear Rug\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_clothingbless.jpg\" alt=\"Clothing Bless Deed\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_runic_spined.jpg\" alt=\"Runic Sewing Kit: Spined\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_runic_horned.jpg\" alt=\"Runic Sewing Kit: Horned\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_runic_barbed.jpg\" alt=\"Runic Sewing Kit: Barbed\"></center></td>"
            );
            html.WriteLine("         </tr>");
            html.WriteLine("         <tr>");
            html.WriteLine(
                "            <td width=\"100\" colspan=\"4\" class=\"entry\"><center>Power Scrolls</center></td>"
            );
            html.WriteLine("            <td width=\"75\" colspan=\"3\" class=\"entry\"><center>Runic Kits</center></td>");
            html.WriteLine("         </tr>");
            html.WriteLine("      </table></td></tr></table>");
        }

        private static void DocumentTailorBOD(
            StreamWriter html, List<RewardItem> items, string amt, BulkMaterialType material,
            Type type
        )
        {
            var rewards = new bool[20];

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i].Construct();

                if (item is Sandals)
                {
                    rewards[5] = true;
                }
                else if (item is SmallStretchedHideEastDeed || item is SmallStretchedHideSouthDeed)
                {
                    rewards[10] = rewards[11] = true;
                }
                else if (item is MediumStretchedHideEastDeed || item is MediumStretchedHideSouthDeed)
                {
                    rewards[10] = rewards[11] = true;
                }
                else if (item is LightFlowerTapestryEastDeed || item is LightFlowerTapestrySouthDeed)
                {
                    rewards[12] = rewards[13] = true;
                }
                else if (item is DarkFlowerTapestryEastDeed || item is DarkFlowerTapestrySouthDeed)
                {
                    rewards[12] = rewards[13] = true;
                }
                else if (item is BrownBearRugEastDeed || item is BrownBearRugSouthDeed)
                {
                    rewards[14] = rewards[15] = true;
                }
                else if (item is PolarBearRugEastDeed || item is PolarBearRugSouthDeed)
                {
                    rewards[14] = rewards[15] = true;
                }
                else if (item is ClothingBlessDeed)
                {
                    rewards[16] = true;
                }
                else if (item is PowerScroll ps)
                {
                    if (ps.Value == 105.0)
                    {
                        rewards[6] = true;
                    }
                    else if (ps.Value == 110.0)
                    {
                        rewards[7] = true;
                    }
                    else if (ps.Value == 115.0)
                    {
                        rewards[8] = true;
                    }
                    else if (ps.Value == 120.0)
                    {
                        rewards[9] = true;
                    }
                }
                else if (item is UncutCloth)
                {
                    if (item.Hue == 0x483 || item.Hue == 0x48C || item.Hue == 0x488 || item.Hue == 0x48A)
                    {
                        rewards[0] = true;
                    }
                    else if (item.Hue == 0x495 || item.Hue == 0x48B || item.Hue == 0x486 || item.Hue == 0x485)
                    {
                        rewards[1] = true;
                    }
                    else if (item.Hue == 0x48D || item.Hue == 0x490 || item.Hue == 0x48E || item.Hue == 0x491)
                    {
                        rewards[2] = true;
                    }
                    else if (item.Hue == 0x48F || item.Hue == 0x494 || item.Hue == 0x484 || item.Hue == 0x497)
                    {
                        rewards[3] = true;
                    }
                    else
                    {
                        rewards[4] = true;
                    }
                }
                else if (item is RunicSewingKit rkit)
                {
                    rewards[16 + CraftResources.GetIndex(rkit.Resource)] = true;
                }

                item.Delete();
            }

            string style = null;
            string name = null;

            switch (material)
            {
                case BulkMaterialType.None:
                    {
                        if (type.IsSubclassOf(typeof(BaseArmor)) || type.IsSubclassOf(typeof(BaseShoes)))
                        {
                            style = "pl";
                            name = "Plain";
                        }
                        else
                        {
                            style = "cl";
                            name = "Cloth";
                        }

                        break;
                    }
                case BulkMaterialType.Spined:
                    style = "sp";
                    name = "Spined";
                    break;
                case BulkMaterialType.Horned:
                    style = "ho";
                    name = "Horned";
                    break;
                case BulkMaterialType.Barbed:
                    style = "ba";
                    name = "Barbed";
                    break;
            }

            html.WriteLine("         <tr>");
            html.WriteLine(
                "            <td width=\"250\" class=\"entry\">&nbsp;- {0} <font size=\"1pt\">{1}</font></td>",
                name,
                amt
            );

            var index = 0;

            while (index < 20)
            {
                if (rewards[index])
                {
                    html.WriteLine("            <td width=\"25\" class=\"{0}\"><center><b>X</b></center></td>", style);
                    ++index;
                }
                else
                {
                    var count = 0;

                    while (index < 20 && !rewards[index])
                    {
                        ++count;
                        ++index;

                        if (index == 5 || index == 6 || index == 10 || index == 17)
                        {
                            break;
                        }
                    }

                    html.WriteLine(
                        "            <td width=\"{0}\"{1} class=\"entry\">&nbsp;</td>",
                        count * 25,
                        count == 1 ? "" : $" colspan=\"{count}\""
                    );
                }
            }

            html.WriteLine("         </tr>");
        }

        private static void WriteSmithLBOD(StreamWriter html, string name, SmallBulkEntry[] entries)
        {
            LargeBOD lbod = new LargeSmithBOD();

            lbod.Entries = LargeBulkEntry.ConvertEntries(lbod, entries);

            WriteSmithBODHeader(html, $"(Large) {name}: Normal");

            lbod.RequireExceptional = false;
            for (var mat = BulkMaterialType.None; mat <= BulkMaterialType.Valorite; ++mat)
            {
                lbod.Material = mat;
                lbod.AmountMax = 10;
                DocumentSmithBOD(html, lbod.ComputeRewards(true), "10, 15, 20", lbod.Material);
            }

            WriteSmithBODFooter(html);

            html.WriteLine("      <br><br>");

            WriteSmithBODHeader(html, $"(Large) {name}: Exceptional");

            lbod.RequireExceptional = true;
            for (var mat = BulkMaterialType.None; mat <= BulkMaterialType.Valorite; ++mat)
            {
                lbod.Material = mat;

                for (var amt = 15; amt <= 20; amt += 5)
                {
                    lbod.AmountMax = amt;
                    DocumentSmithBOD(html, lbod.ComputeRewards(true), amt == 20 ? "20" : "10, 15", lbod.Material);
                }
            }

            WriteSmithBODFooter(html);

            html.WriteLine("      <br><br>");
            html.WriteLine("      <br><br>");
        }

        private static void WriteSmithBODHeader(StreamWriter html, string title)
        {
            html.WriteLine("      <table width=\"850\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
            html.WriteLine("      <tr><td class=\"tbl-border\">");
            html.WriteLine("      <table border=\"0\" width=\"850\" cellpadding=\"0\" cellspacing=\"1\">");
            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"250\" rowspan=\"2\" class=\"entry\"><center>{0}</center></td>", title);
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_sturdytool.jpg\" alt=\"Sturdy Pickaxe/Shovel (150 uses)\"></center></td>"
            );
            html.WriteLine("            <td width=\"75\" colspan=\"3\" class=\"entry\"><center>Gloves</center></td>");
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_gargaxe.jpg\" alt=\"Gargoyles Pickaxe (100 uses)\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_prospectortool.jpg\" alt=\"Prospectors Tool (50 uses)\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_powder.jpg\" alt=\"Powder of Temperament (10 uses)\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_anvil.jpg\" alt=\"Colored Anvil\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"100\" colspan=\"4\" class=\"entry\"><center>Power Scrolls</center></td>"
            );
            html.WriteLine(
                "            <td width=\"200\" colspan=\"8\" class=\"entry\"><center>Runic Hammers</center></td>"
            );
            html.WriteLine(
                "            <td width=\"100\" colspan=\"4\" class=\"entry\"><center>Ancient Hammers</center></td>"
            );
            html.WriteLine("         </tr>");
            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+1</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+3</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+5</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+5</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+10</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+15</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+20</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"du\"><center><small>Du</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"sh\"><center><small>Sh</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"co\"><center><small>Co</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"br\"><center><small>Br</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"go\"><center><small>Go</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"ag\"><center><small>Ag</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"ve\"><center><small>Ve</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"va\"><center><small>Va</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+10</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+15</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+30</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+60</small></center></td>");
            html.WriteLine("         </tr>");
        }

        private static void WriteSmithBODFooter(StreamWriter html)
        {
            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"250\" rowspan=\"2\" class=\"entry\">&nbsp;</td>");
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_sturdytool.jpg\" alt=\"Sturdy Pickaxe/Shovel (150 uses)\"></center></td>"
            );
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+1</small></center>&nbsp;</td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+3</small></center>&nbsp;</td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+5</small></center>&nbsp;</td>");
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_gargaxe.jpg\" alt=\"Gargoyles Pickaxe (100 uses)\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_prospectortool.jpg\" alt=\"Prospectors Tool (50 uses)\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_powder.jpg\" alt=\"Powder of Temperament (10 uses)\"></center></td>"
            );
            html.WriteLine(
                "            <td width=\"25\" rowspan=\"2\" class=\"entry\"><center><img src=\"http://www.runuo.com/images/bodreward_anvil.jpg\" alt=\"Colored Anvil\"></center></td>"
            );
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+5</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+10</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+15</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+20</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"du\"><center><small>Du</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"sh\"><center><small>Sh</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"co\"><center><small>Co</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"br\"><center><small>Br</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"go\"><center><small>Go</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"ag\"><center><small>Ag</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"ve\"><center><small>Ve</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"va\"><center><small>Va</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+10</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+15</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+30</small></center></td>");
            html.WriteLine("            <td width=\"25\" class=\"entry\"><center><small>+60</small></center></td>");
            html.WriteLine("         </tr>");
            html.WriteLine("         <tr>");
            html.WriteLine("            <td width=\"75\" colspan=\"3\" class=\"entry\"><center>Gloves</center></td>");
            html.WriteLine(
                "            <td width=\"100\" colspan=\"4\" class=\"entry\"><center>Power Scrolls</center></td>"
            );
            html.WriteLine(
                "            <td width=\"200\" colspan=\"8\" class=\"entry\"><center>Runic Hammers</center></td>"
            );
            html.WriteLine(
                "            <td width=\"100\" colspan=\"4\" class=\"entry\"><center>Ancient Hammers</center></td>"
            );
            html.WriteLine("         </tr>");
            html.WriteLine("      </table></td></tr></table>");
        }

        private static void DocumentSmithBOD(
            StreamWriter html, List<RewardItem> items, string amt, BulkMaterialType material
        )
        {
            var rewards = new bool[24];

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i].Construct();

                if (item is SturdyPickaxe || item is SturdyShovel)
                {
                    rewards[0] = true;
                }
                else if (item is LeatherGlovesOfMining)
                {
                    rewards[1] = true;
                }
                else if (item is StuddedGlovesOfMining)
                {
                    rewards[2] = true;
                }
                else if (item is RingmailGlovesOfMining)
                {
                    rewards[3] = true;
                }
                else if (item is GargoylesPickaxe)
                {
                    rewards[4] = true;
                }
                else if (item is ProspectorsTool)
                {
                    rewards[5] = true;
                }
                else if (item is PowderOfTemperament)
                {
                    rewards[6] = true;
                }
                else if (item is ColoredAnvil)
                {
                    rewards[7] = true;
                }
                else if (item is PowerScroll ps)
                {
                    if (ps.Value == 105.0)
                    {
                        rewards[8] = true;
                    }
                    else if (ps.Value == 110.0)
                    {
                        rewards[9] = true;
                    }
                    else if (ps.Value == 115.0)
                    {
                        rewards[10] = true;
                    }
                    else if (ps.Value == 120.0)
                    {
                        rewards[11] = true;
                    }
                }
                else if (item is RunicHammer rh)
                {
                    rewards[11 + CraftResources.GetIndex(rh.Resource)] = true;
                }
                else if (item is AncientSmithyHammer ash)
                {
                    if (ash.Bonus == 10)
                    {
                        rewards[20] = true;
                    }
                    else if (ash.Bonus == 15)
                    {
                        rewards[21] = true;
                    }
                    else if (ash.Bonus == 30)
                    {
                        rewards[22] = true;
                    }
                    else if (ash.Bonus == 60)
                    {
                        rewards[23] = true;
                    }
                }

                item.Delete();
            }

            string style = null;
            string name = null;

            switch (material)
            {
                case BulkMaterialType.None:
                    style = "ir";
                    name = "Iron";
                    break;
                case BulkMaterialType.DullCopper:
                    style = "du";
                    name = "Dull Copper";
                    break;
                case BulkMaterialType.ShadowIron:
                    style = "sh";
                    name = "Shadow Iron";
                    break;
                case BulkMaterialType.Copper:
                    style = "co";
                    name = "Copper";
                    break;
                case BulkMaterialType.Bronze:
                    style = "br";
                    name = "Bronze";
                    break;
                case BulkMaterialType.Gold:
                    style = "go";
                    name = "Gold";
                    break;
                case BulkMaterialType.Agapite:
                    style = "ag";
                    name = "Agapite";
                    break;
                case BulkMaterialType.Verite:
                    style = "ve";
                    name = "Verite";
                    break;
                case BulkMaterialType.Valorite:
                    style = "va";
                    name = "Valorite";
                    break;
            }

            html.WriteLine("         <tr>");
            html.WriteLine(
                "            <td width=\"250\" class=\"entry\">{0} <font size=\"1pt\">{1}</font></td>",
                name,
                amt
            );

            var index = 0;

            while (index < 24)
            {
                if (rewards[index])
                {
                    html.WriteLine("            <td width=\"25\" class=\"{0}\"><center><b>X</b></center></td>", style);
                    ++index;
                }
                else
                {
                    var count = 0;

                    while (index < 24 && !rewards[index])
                    {
                        ++count;
                        ++index;

                        if (index == 4 || index == 8 || index == 12 || index == 20)
                        {
                            break;
                        }
                    }

                    html.WriteLine(
                        "            <td width=\"{0}\"{1} class=\"entry\">&nbsp;</td>",
                        count * 25,
                        count == 1 ? "" : $" colspan=\"{count}\""
                    );
                }
            }

            html.WriteLine("         </tr>");
        }

        public static List<BodyEntry> LoadBodies()
        {
            var list = new List<BodyEntry>();

            var path = Path.Combine(Core.BaseDirectory, "Data/models.txt");

            if (File.Exists(path))
            {
                using var ip = new StreamReader(path);
                string line;

                while ((line = ip.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length == 0 || line.StartsWith("#"))
                    {
                        continue;
                    }

                    var split = line.Split('\t');

                    if (split.Length >= 9)
                    {
                        Body body = Utility.ToInt32(split[0]);
                        var type = (ModelBodyType)Utility.ToInt32(split[1]);
                        var name = split[8];

                        var entry = new BodyEntry(body, type, name);

                        if (!list.Contains(entry))
                        {
                            list.Add(entry);
                        }
                    }
                }
            }

            return list;
        }

        private static void DocumentBodies()
        {
            var list = LoadBodies();

            using var html = GetWriter("docs/", "bodies.html");
            html.WriteLine("<html>");
            html.WriteLine("   <head>");
            html.WriteLine("      <title>RunUO Documentation - Body List</title>");
            html.WriteLine("      <link rel=\"stylesheet\" type=\"text/css\" href=\"styles.css\" />");
            html.WriteLine("   </head>");
            html.WriteLine("   <body>");
            html.WriteLine("      <a name=\"Top\" />");
            html.WriteLine("      <h4><a href=\"index.html\">Back to the index</a></h4>");

            if (list.Count > 0)
            {
                html.WriteLine("      <h2>Body List</h2>");

                list.Sort(new BodyEntrySorter());

                var lastType = ModelBodyType.Invalid;

                for (var i = 0; i < list.Count; ++i)
                {
                    var entry = list[i];
                    var type = entry.BodyType;

                    if (type != lastType)
                    {
                        if (lastType != ModelBodyType.Invalid)
                        {
                            html.WriteLine("      </table></td></tr></table><br>");
                        }

                        lastType = type;

                        html.WriteLine("      <a name=\"{0}\" />", type);

                        switch (type)
                        {
                            case ModelBodyType.Monsters:
                                html.WriteLine(
                                    "      <b>Monsters</b> | <a href=\"#Sea\">Sea</a> | <a href=\"#Animals\">Animals</a> | <a href=\"#Human\">Human</a> | <a href=\"#Equipment\">Equipment</a><br><br>"
                                );
                                break;
                            case ModelBodyType.Sea:
                                html.WriteLine(
                                    "      <a href=\"#Top\">Monsters</a> | <b>Sea</b> | <a href=\"#Animals\">Animals</a> | <a href=\"#Human\">Human</a> | <a href=\"#Equipment\">Equipment</a><br><br>"
                                );
                                break;
                            case ModelBodyType.Animals:
                                html.WriteLine(
                                    "      <a href=\"#Top\">Monsters</a> | <a href=\"#Sea\">Sea</a> | <b>Animals</b> | <a href=\"#Human\">Human</a> | <a href=\"#Equipment\">Equipment</a><br><br>"
                                );
                                break;
                            case ModelBodyType.Human:
                                html.WriteLine(
                                    "      <a href=\"#Top\">Monsters</a> | <a href=\"#Sea\">Sea</a> | <a href=\"#Animals\">Animals</a> | <b>Human</b> | <a href=\"#Equipment\">Equipment</a><br><br>"
                                );
                                break;
                            case ModelBodyType.Equipment:
                                html.WriteLine(
                                    "      <a href=\"#Top\">Monsters</a> | <a href=\"#Sea\">Sea</a> | <a href=\"#Animals\">Animals</a> | <a href=\"#Human\">Human</a> | <b>Equipment</b><br><br>"
                                );
                                break;
                        }

                        html.WriteLine("      <table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
                        html.WriteLine("      <tr><td class=\"tbl-border\">");
                        html.WriteLine("      <table width=\"100%\" cellpadding=\"4\" cellspacing=\"1\">");
                        html.WriteLine(
                            "         <tr><td width=\"100%\" colspan=\"2\" class=\"header\">{0}</td></tr>",
                            type
                        );
                    }

                    html.WriteLine(
                        "         <tr><td class=\"lentry\">{0}</td><td class=\"rentry\">{1}</td></tr>",
                        entry.Body.BodyID,
                        entry.Name
                    );
                }

                html.WriteLine("      </table>");
            }
            else
            {
                html.WriteLine("      This feature requires a UO:3D installation.");
            }

            html.WriteLine("   </body>");
            html.WriteLine("</html>");
        }

        private static void DocumentKeywords()
        {
            var tables = LoadSpeechFile();

            using var html = GetWriter("docs/", "keywords.html");
            html.WriteLine("<html>");
            html.WriteLine("   <head>");
            html.WriteLine("      <title>RunUO Documentation - Speech Keywords</title>");
            html.WriteLine("      <link rel=\"stylesheet\" type=\"text/css\" href=\"styles.css\" />");
            html.WriteLine("   </head>");
            html.WriteLine("   <body>");
            html.WriteLine("      <h4><a href=\"index.html\">Back to the index</a></h4>");
            html.WriteLine("      <h2>Speech Keywords</h2>");

            for (var p = 0; p < 1 && p < tables.Count; ++p)
            {
                var table = tables[p];

                html.WriteLine("      <table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
                html.WriteLine("      <tr><td class=\"tbl-border\">");
                html.WriteLine("      <table width=\"100%\" cellpadding=\"4\" cellspacing=\"1\">");
                html.WriteLine("         <tr><td class=\"header\">Number</td><td class=\"header\">Text</td></tr>");

                var list = new List<SpeechEntry>(table.Values);
                list.Sort(new SpeechEntrySorter());

                for (var i = 0; i < list.Count; ++i)
                {
                    var entry = list[i];

                    html.Write("         <tr><td class=\"lentry\">0x{0:X4}</td><td class=\"rentry\">", entry.Index);

                    entry.Strings.Sort(); // ( new EnglishPrioStringSorter() );

                    for (var j = 0; j < entry.Strings.Count; ++j)
                    {
                        if (j > 0)
                        {
                            html.Write("<br>");
                        }

                        var v = entry.Strings[j];

                        for (var k = 0; k < v.Length; ++k)
                        {
                            var c = v[k];

                            if (c == '<')
                            {
                                html.Write("&lt;");
                            }
                            else if (c == '>')
                            {
                                html.Write("&gt;");
                            }
                            else if (c == '&')
                            {
                                html.Write("&amp;");
                            }
                            else if (c == '"')
                            {
                                html.Write("&quot;");
                            }
                            else if (c == '\'')
                            {
                                html.Write("&apos;");
                            }
                            else if (c >= 0x20 && c < 0x7F)
                            {
                                html.Write(c);
                            }
                            else
                            {
                                html.Write("&#{0};", (int)c);
                            }
                        }
                    }

                    html.WriteLine("</td></tr>");
                }

                html.WriteLine("      </table></td></tr></table>");
            }

            html.WriteLine("   </body>");
            html.WriteLine("</html>");
        }

        private static List<Dictionary<int, SpeechEntry>> LoadSpeechFile()
        {
            var tables = new List<Dictionary<int, SpeechEntry>>();
            var lastIndex = -1;

            Dictionary<int, SpeechEntry> table = null;

            var path = Core.FindDataFile("speech.mul", false);

            if (File.Exists(path))
            {
                using var ip = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var bin = new BinaryReader(ip);

                while (bin.PeekChar() >= 0)
                {
                    var index = (bin.ReadByte() << 8) | bin.ReadByte();
                    var length = (bin.ReadByte() << 8) | bin.ReadByte();
                    var text = Encoding.UTF8.GetString(bin.ReadBytes(length)).Trim();

                    if (text.Length == 0)
                    {
                        continue;
                    }

                    if (table == null || lastIndex > index)
                    {
                        if (index == 0 && text == "*withdraw*")
                        {
                            tables.Insert(0, table = new Dictionary<int, SpeechEntry>());
                        }
                        else
                        {
                            tables.Add(table = new Dictionary<int, SpeechEntry>());
                        }
                    }

                    lastIndex = index;

                    if (!table.TryGetValue(index, out var entry))
                    {
                        table[index] = entry = new SpeechEntry(index);
                    }

                    entry.Strings.Add(text);
                }
            }

            return tables;
        }

        private static void DocumentCommands()
        {
            using var html = GetWriter("docs/", "commands.html");
            html.WriteLine("<html>");
            html.WriteLine("   <head>");
            html.WriteLine("      <title>RunUO Documentation - Commands</title>");
            html.WriteLine("      <link rel=\"stylesheet\" type=\"text/css\" href=\"styles.css\" />");
            html.WriteLine("   </head>");
            html.WriteLine("   <body>");
            html.WriteLine("      <a name=\"Top\" />");
            html.WriteLine("      <h4><a href=\"index.html\">Back to the index</a></h4>");
            html.WriteLine("      <h2>Commands</h2>");

            var commands = new List<CommandEntry>(CommandSystem.Entries.Values);
            var list = new List<DocCommandEntry>();

            commands.Sort();
            commands.Reverse();
            Clean(commands);

            for (var i = 0; i < commands.Count; ++i)
            {
                var e = commands[i];

                var mi = e.Handler.Method;

                var attrs = mi.GetCustomAttributes(typeof(UsageAttribute), false);

                if (attrs.Length == 0)
                {
                    continue;
                }

                var usage = attrs[0] as UsageAttribute;

                attrs = mi.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs.Length == 0)
                {
                    continue;
                }

                if (usage == null || !(attrs[0] is DescriptionAttribute desc))
                {
                    continue;
                }

                attrs = mi.GetCustomAttributes(typeof(AliasesAttribute), false);

                var aliases = attrs.Length == 0 ? null : attrs[0] as AliasesAttribute;

                var descString = desc.Description.Replace("<", "&lt;").Replace(">", "&gt;");

                if (aliases == null)
                {
                    list.Add(new DocCommandEntry(e.AccessLevel, e.Command, null, usage.Usage, descString));
                }
                else
                {
                    list.Add(new DocCommandEntry(e.AccessLevel, e.Command, aliases.Aliases, usage.Usage, descString));
                }
            }

            for (var i = 0; i < TargetCommands.AllCommands.Count; ++i)
            {
                var command = TargetCommands.AllCommands[i];

                var usage = command.Usage;
                var desc = command.Description;

                if (usage == null || desc == null)
                {
                    continue;
                }

                var cmds = command.Commands;
                var cmd = cmds[0];
                var aliases = new string[cmds.Length - 1];

                for (var j = 0; j < aliases.Length; ++j)
                {
                    aliases[j] = cmds[j + 1];
                }

                desc = desc.Replace("<", "&lt;").Replace(">", "&gt;");

                if (command.Supports != CommandSupport.Single)
                {
                    var sb = new StringBuilder(50 + desc.Length);

                    sb.Append("Modifiers: ");

                    if ((command.Supports & CommandSupport.Global) != 0)
                    {
                        sb.Append("<i><a href=\"#Global\">Global</a></i>, ");
                    }

                    if ((command.Supports & CommandSupport.Online) != 0)
                    {
                        sb.Append("<i><a href=\"#Online\">Online</a></i>, ");
                    }

                    if ((command.Supports & CommandSupport.Region) != 0)
                    {
                        sb.Append("<i><a href=\"#Region\">Region</a></i>, ");
                    }

                    if ((command.Supports & CommandSupport.Contained) != 0)
                    {
                        sb.Append("<i><a href=\"#Contained\">Contained</a></i>, ");
                    }

                    if ((command.Supports & CommandSupport.Multi) != 0)
                    {
                        sb.Append("<i><a href=\"#Multi\">Multi</a></i>, ");
                    }

                    if ((command.Supports & CommandSupport.Area) != 0)
                    {
                        sb.Append("<i><a href=\"#Area\">Area</a></i>, ");
                    }

                    if ((command.Supports & CommandSupport.Self) != 0)
                    {
                        sb.Append("<i><a href=\"#Self\">Self</a></i>, ");
                    }

                    sb.Remove(sb.Length - 2, 2);
                    sb.Append("<br>");
                    sb.Append(desc);

                    desc = sb.ToString();
                }

                list.Add(new DocCommandEntry(command.AccessLevel, cmd, aliases, usage, desc));
            }

            var commandImpls = BaseCommandImplementor.Implementors;

            for (var i = 0; i < commandImpls.Count; ++i)
            {
                var command = commandImpls[i];

                var usage = command.Usage;
                var desc = command.Description;

                if (usage == null || desc == null)
                {
                    continue;
                }

                var cmds = command.Accessors;
                var cmd = cmds[0];
                var aliases = new string[cmds.Length - 1];

                for (var j = 0; j < aliases.Length; ++j)
                {
                    aliases[j] = cmds[j + 1];
                }

                desc = desc.Replace("<", "&lt;").Replace(">", "&gt;");

                list.Add(new DocCommandEntry(command.AccessLevel, cmd, aliases, usage, desc));
            }

            list.Sort(new CommandEntrySorter());

            var last = AccessLevel.Player;

            foreach (var e in list)
            {
                if (e.AccessLevel != last)
                {
                    if (last != AccessLevel.Player)
                    {
                        html.WriteLine("      </table></td></tr></table><br>");
                    }

                    last = e.AccessLevel;

                    html.WriteLine("      <a name=\"{0}\" />", last);

                    switch (last)
                    {
                        case AccessLevel.Administrator:
                            html.WriteLine(
                                "      <b>Administrator</b> | <a href=\"#GameMaster\">Game Master</a> | <a href=\"#Counselor\">Counselor</a> | <a href=\"#Player\">Player</a><br><br>"
                            );
                            break;
                        case AccessLevel.GameMaster:
                            html.WriteLine(
                                "      <a href=\"#Top\">Administrator</a> | <b>Game Master</b> | <a href=\"#Counselor\">Counselor</a> | <a href=\"#Player\">Player</a><br><br>"
                            );
                            break;
                        case AccessLevel.Seer:
                            html.WriteLine(
                                "      <a href=\"#Top\">Administrator</a> | <a href=\"#GameMaster\">Game Master</a> | <a href=\"#Counselor\">Counselor</a> | <a href=\"#Player\">Player</a><br><br>"
                            );
                            break;
                        case AccessLevel.Counselor:
                            html.WriteLine(
                                "      <a href=\"#Top\">Administrator</a> | <a href=\"#GameMaster\">Game Master</a> | <b>Counselor</b> | <a href=\"#Player\">Player</a><br><br>"
                            );
                            break;
                        case AccessLevel.Player:
                            html.WriteLine(
                                "      <a href=\"#Top\">Administrator</a> | <a href=\"#GameMaster\">Game Master</a> | <a href=\"#Counselor\">Counselor</a> | <b>Player</b><br><br>"
                            );
                            break;
                    }

                    html.WriteLine("      <table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
                    html.WriteLine("      <tr><td class=\"tbl-border\">");
                    html.WriteLine("      <table width=\"100%\" cellpadding=\"4\" cellspacing=\"1\">");
                    html.WriteLine(
                        "         <tr><td colspan=\"2\" width=\"100%\" class=\"header\">{0}</td></tr>",
                        last == AccessLevel.GameMaster ? "Game Master" : last.ToString()
                    );
                }

                DocumentCommand(html, e);
            }

            html.WriteLine("      </table></td></tr></table>");
            html.WriteLine("   </body>");
            html.WriteLine("</html>");
        }

        public static void Clean(List<CommandEntry> list)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var e = list[i];

                for (var j = i + 1; j < list.Count; ++j)
                {
                    var c = list[j];

                    if (e.Handler.Method == c.Handler.Method)
                    {
                        list.RemoveAt(j);
                        --j;
                    }
                }
            }
        }

        private static void DocumentCommand(StreamWriter html, DocCommandEntry e)
        {
            var usage = e.Usage;
            var desc = e.Description;
            var aliases = e.Aliases;

            html.Write("         <tr><a name=\"{0}\" /><td class=\"lentry\">{0}</td>", e.Name);

            if (aliases == null || aliases.Length == 0)
            {
                html.Write(
                    "<td class=\"rentry\"><b>Usage: {0}</b><br>{1}</td>",
                    usage.Replace("<", "&lt;").Replace(">", "&gt;"),
                    desc
                );
            }
            else
            {
                html.Write(
                    "<td class=\"rentry\"><b>Usage: {0}</b><br>Alias{1}: ",
                    usage.Replace("<", "&lt;").Replace(">", "&gt;"),
                    aliases.Length == 1 ? "" : "es"
                );

                for (var i = 0; i < aliases.Length; ++i)
                {
                    if (i != 0)
                    {
                        html.Write(", ");
                    }

                    html.Write(aliases[i]);
                }

                html.Write("<br>{0}</td>", desc);
            }

            html.WriteLine("</tr>");
        }

        private static bool IsConstructible(Type t, out bool isItem) =>
            (isItem = typeofItem.IsAssignableFrom(t)) || typeofMobile.IsAssignableFrom(t);

        private static bool IsConstructible(ConstructorInfo ctor) => ctor.IsDefined(typeof(ConstructibleAttribute), false);

        private static void DocumentConstructibleObjects()
        {
            var types = new List<TypeInfo>(m_Types.Values);
            types.Sort(new TypeComparer());

            var items = new List<(Type, ConstructorInfo[])>();
            var mobiles = new List<(Type, ConstructorInfo[])>();

            for (var i = 0; i < types.Count; ++i)
            {
                var t = types[i].m_Type;

                if (t.IsAbstract || !IsConstructible(t, out var isItem))
                {
                    continue;
                }

                var ctors = t.GetConstructors();
                var anyConstructible = false;

                for (var j = 0; !anyConstructible && j < ctors.Length; ++j)
                {
                    anyConstructible = IsConstructible(ctors[j]);
                }

                if (anyConstructible)
                {
                    (isItem ? items : mobiles).Add((t, ctors));
                }
            }

            using var html = GetWriter("docs/", "objects.html");
            html.WriteLine("<html>");
            html.WriteLine("   <head>");
            html.WriteLine("      <title>RunUO Documentation - Constructible Objects</title>");
            html.WriteLine("      <link rel=\"stylesheet\" type=\"text/css\" href=\"styles.css\" />");
            html.WriteLine("   </head>");
            html.WriteLine("   <body>");
            html.WriteLine("      <h4><a href=\"index.html\">Back to the index</a></h4>");
            html.WriteLine(
                "      <h2>Constructible <a href=\"#items\">Items</a> and <a href=\"#mobiles\">Mobiles</a></h2>"
            );

            html.WriteLine("      <a name=\"items\" />");
            html.WriteLine("      <table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
            html.WriteLine("      <tr><td class=\"tbl-border\">");
            html.WriteLine("      <table width=\"100%\" cellpadding=\"4\" cellspacing=\"1\">");
            html.WriteLine("         <tr><td class=\"header\">Item Name</td><td class=\"header\">Usage</td></tr>");

            items.ForEach(
                tuple =>
                {
                    var (type, constructors) = tuple;
                    DocumentConstructibleObject(html, type, constructors);
                }
            );

            html.WriteLine("      </table></td></tr></table><br><br>");

            html.WriteLine("      <a name=\"mobiles\" />");
            html.WriteLine("      <table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
            html.WriteLine("      <tr><td class=\"tbl-border\">");
            html.WriteLine("      <table width=\"100%\" cellpadding=\"4\" cellspacing=\"1\">");
            html.WriteLine("         <tr><td class=\"header\">Mobile Name</td><td class=\"header\">Usage</td></tr>");

            mobiles.ForEach(
                tuple =>
                {
                    var (type, constructors) = tuple;
                    DocumentConstructibleObject(html, type, constructors);
                }
            );

            html.WriteLine("      </table></td></tr></table>");

            html.WriteLine("   </body>");
            html.WriteLine("</html>");
        }

        private static void DocumentConstructibleObject(StreamWriter html, Type t, ConstructorInfo[] ctors)
        {
            html.Write("         <tr><td class=\"lentry\">{0}</td><td class=\"rentry\">", t.Name);

            var first = true;

            for (var i = 0; i < ctors.Length; ++i)
            {
                var ctor = ctors[i];

                if (!IsConstructible(ctor))
                {
                    continue;
                }

                if (!first)
                {
                    html.Write("<br>");
                }

                first = false;

                html.Write("{0}Add {1}", CommandSystem.Prefix, t.Name);

                var parms = ctor.GetParameters();

                for (var j = 0; j < parms.Length; ++j)
                {
                    html.Write(" <a ");

                    if (m_Types.TryGetValue(parms[j].ParameterType, out var typeInfo))
                    {
                        html.Write("href=\"types/{0}\" ", typeInfo.FileName);
                    }

                    html.Write("title=\"{0}\">{1}</a>", GetTooltipFor(parms[j]), parms[j].Name);
                }
            }

            html.WriteLine("</td></tr>");
        }

        private static string GetTooltipFor(ParameterInfo param)
        {
            var paramType = param.ParameterType;

            for (var i = 0; i < m_Tooltips.GetLength(0); ++i)
            {
                var checkType = (Type)m_Tooltips[i, 0];

                if (paramType == checkType)
                {
                    return string.Format((string)m_Tooltips[i, 1], HtmlNewLine);
                }
            }

            if (paramType.IsEnum)
            {
                var sb = new StringBuilder();

                sb.AppendFormat("Enumeration value or name. Possible named values include:{0}", HtmlNewLine);

                var names = Enum.GetNames(paramType);

                for (var i = 0; i < names.Length; ++i)
                {
                    sb.AppendFormat("{0}- {1}", HtmlNewLine, names[i]);
                }

                return sb.ToString();
            }

            if (paramType.IsDefined(typeofCustomEnum, false))
            {
                var attributes = paramType.GetCustomAttributes(typeofCustomEnum, false);

                if (attributes.Length > 0 && attributes[0] is CustomEnumAttribute attr)
                {
                    var sb = new StringBuilder();

                    sb.AppendFormat("Enumeration value or name. Possible named values include:{0}", HtmlNewLine);

                    var names = attr.Names;

                    for (var i = 0; i < names.Length; ++i)
                    {
                        sb.AppendFormat("{0}- {1}", HtmlNewLine, names[i]);
                    }

                    return sb.ToString();
                }
            }
            else if (paramType == typeofMap)
            {
                var sb = new StringBuilder();

                sb.AppendFormat("Enumeration value or name. Possible named values include:{0}", HtmlNewLine);

                var names = Map.GetMapNames();

                for (var i = 0; i < names.Length; ++i)
                {
                    sb.AppendFormat("{0}- {1}", HtmlNewLine, names[i]);
                }

                return sb.ToString();
            }

            return "";
        }

        private static void WriteEnum(TypeInfo info, StreamWriter typeHtml)
        {
            var type = info.m_Type;

            typeHtml.WriteLine("      <h2>{0} (Enum)</h2>", info.TypeName);

            var names = Enum.GetNames(type);

            var flags = type.IsDefined(typeof(FlagsAttribute), false);
            string format;

            if (flags)
            {
                format = "      {0:G} = 0x{1:X}{2}<br>";
            }
            else
            {
                format = "      {0:G} = {1:D}{2}<br>";
            }

            for (var i = 0; i < names.Length; ++i)
            {
                var value = Enum.Parse(type, names[i]);

                typeHtml.WriteLine(format, names[i], value, i < names.Length - 1 ? "," : "");
            }
        }

        private static void WriteType(TypeInfo info, StreamWriter typeHtml)
        {
            var type = info.m_Type;

            typeHtml.Write("      <h2>");

            var decType = info.m_Declaring;

            if (decType != null)
            {
                // We are a nested type

                typeHtml.Write('(');

                m_Types.TryGetValue(decType, out var decInfo);

                if (decInfo == null)
                {
                    typeHtml.Write(decType.Name);
                }
                else
                    // typeHtml.Write( "<a href=\"{0}\">{1}</a>", decInfo.m_FileName, decInfo.m_TypeName );
                {
                    typeHtml.Write(decInfo.LinkName(null));
                }

                typeHtml.Write(") - ");
            }

            typeHtml.Write(info.TypeName);

            var ifaces = info.m_Interfaces;
            var baseType = info.m_BaseType;

            var extendCount = 0;

            if (baseType != typeof(object) && baseType != typeof(ValueType) && baseType?.IsPrimitive == false)
            {
                typeHtml.Write(" : ");

                m_Types.TryGetValue(baseType, out var baseInfo);

                if (baseInfo == null)
                {
                    typeHtml.Write(baseType.Name);
                }
                else
                {
                    typeHtml.Write($"<!-- DBG-1 -->{baseInfo.LinkName(null)}");
                }

                ++extendCount;
            }

            if (ifaces.Length > 0)
            {
                if (extendCount == 0)
                {
                    typeHtml.Write(" : ");
                }

                for (var i = 0; i < ifaces.Length; ++i)
                {
                    var iface = ifaces[i];
                    m_Types.TryGetValue(iface, out var ifaceInfo);

                    if (extendCount != 0)
                    {
                        typeHtml.Write(", ");
                    }

                    ++extendCount;

                    if (ifaceInfo == null)
                    {
                        FormatGeneric(iface, out _, out _, out var linkName);
                        typeHtml.Write($"<!-- DBG-2.1 -->{linkName.Replace("@directory@", null)}");
                    }
                    else
                    {
                        typeHtml.Write($"<!-- DBG-2.2 -->{ifaceInfo.LinkName(null)}");
                    }
                }
            }

            typeHtml.WriteLine("</h2>");

            var derived = info.m_Derived;

            if (derived != null)
            {
                typeHtml.Write("<h4>Derived Types: ");

                derived.Sort(new TypeComparer());

                for (var i = 0; i < derived.Count; ++i)
                {
                    var derivedInfo = derived[i];

                    if (i != 0)
                    {
                        typeHtml.Write(", ");
                    }

                    // typeHtml.Write( "<a href=\"{0}\">{1}</a>", derivedInfo.m_FileName, derivedInfo.m_TypeName );
                    typeHtml.Write($"<!-- DBG-3 -->{derivedInfo.LinkName(null)}");
                }

                typeHtml.WriteLine("</h4>");
            }

            var nested = info.m_Nested;

            if (nested != null)
            {
                typeHtml.Write("<h4>Nested Types: ");

                nested.Sort(new TypeComparer());

                for (var i = 0; i < nested.Count; ++i)
                {
                    var nestedInfo = nested[i];

                    if (i != 0)
                    {
                        typeHtml.Write(", ");
                    }

                    // typeHtml.Write( "<a href=\"{0}\">{1}</a>", nestedInfo.m_FileName, nestedInfo.m_TypeName );
                    typeHtml.Write($"<!-- DBG-4 -->{nestedInfo.LinkName(null)}");
                }

                typeHtml.WriteLine("</h4>");
            }

            var membs = type.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.Instance | BindingFlags.DeclaredOnly
            );

            Array.Sort(membs, new MemberComparer());

            for (var i = 0; i < membs.Length; ++i)
            {
                var mi = membs[i];

                if (mi is PropertyInfo propertyInfo)
                {
                    WriteProperty(propertyInfo, typeHtml);
                }
                else if (mi is ConstructorInfo constructorInfo)
                {
                    WriteCtor(info.TypeName, constructorInfo, typeHtml);
                }
                else if (mi is MethodInfo methodInfo)
                {
                    WriteMethod(methodInfo, typeHtml);
                }
            }
        }

        private static void WriteProperty(PropertyInfo pi, StreamWriter html)
        {
            html.Write("      ");

            var getMethod = pi.GetGetMethod();
            var setMethod = pi.GetSetMethod();

            if (getMethod?.IsStatic == true || setMethod?.IsStatic == true)
            {
                html.Write(StaticString);
            }

            html.Write(GetPair(pi.PropertyType, pi.Name, false));
            html.Write('(');

            if (pi.CanRead)
            {
                html.Write(GetString);
            }

            if (pi.CanWrite)
            {
                html.Write(SetString);
            }

            html.WriteLine(" )<br>");
        }

        private static void WriteCtor(string name, ConstructorInfo ctor, StreamWriter html)
        {
            if (ctor.IsStatic)
            {
                return;
            }

            html.Write("      ");
            html.Write(CtorString);
            html.Write(name);
            html.Write('(');

            var parms = ctor.GetParameters();

            if (parms.Length > 0)
            {
                html.Write(' ');

                for (var i = 0; i < parms.Length; ++i)
                {
                    var pi = parms[i];

                    if (i != 0)
                    {
                        html.Write(", ");
                    }

                    if (pi.IsIn)
                    {
                        html.Write(InString);
                    }
                    else if (pi.IsOut)
                    {
                        html.Write(OutString);
                    }

                    html.Write(GetPair(pi.ParameterType, pi.Name, pi.IsOut));
                }

                html.Write(' ');
            }

            html.WriteLine(")<br>");
        }

        private static void WriteMethod(MethodInfo mi, StreamWriter html)
        {
            if (mi.IsSpecialName)
            {
                return;
            }

            html.Write("      ");

            if (mi.IsStatic)
            {
                html.Write(StaticString);
            }

            if (mi.IsVirtual)
            {
                html.Write(VirtString);
            }

            html.Write(GetPair(mi.ReturnType, mi.Name, false));
            html.Write('(');

            var parms = mi.GetParameters();

            if (parms.Length > 0)
            {
                html.Write(' ');

                for (var i = 0; i < parms.Length; ++i)
                {
                    var pi = parms[i];

                    if (i != 0)
                    {
                        html.Write(", ");
                    }

                    if (pi.IsIn)
                    {
                        html.Write(InString);
                    }
                    else if (pi.IsOut)
                    {
                        html.Write(OutString);
                    }

                    html.Write(GetPair(pi.ParameterType, pi.Name, pi.IsOut));
                }

                html.Write(' ');
            }

            html.WriteLine(")<br>");
        }

        private class MemberComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x == y)
                {
                    return 0;
                }

                var aCtor = x as ConstructorInfo;
                var bCtor = y as ConstructorInfo;

                var aProp = x as PropertyInfo;
                var bProp = y as PropertyInfo;

                var aMethod = x as MethodInfo;
                var bMethod = y as MethodInfo;

                var aStatic = GetStaticFor(aCtor, aProp, aMethod);
                var bStatic = GetStaticFor(bCtor, bProp, bMethod);

                if (aStatic && !bStatic)
                {
                    return -1;
                }

                if (!aStatic && bStatic)
                {
                    return 1;
                }

                var v = 0;

                if (aCtor != null)
                {
                    if (bCtor == null)
                    {
                        v = -1;
                    }
                }
                else if (bCtor != null)
                {
                    v = 1;
                }
                else if (aProp != null)
                {
                    if (bProp == null)
                    {
                        v = -1;
                    }
                }
                else if (bProp != null)
                {
                    v = 1;
                }

                if (v == 0)
                {
                    v = GetNameFrom(aCtor, aProp, aMethod).CompareTo(GetNameFrom(bCtor, bProp, bMethod));
                }

                if (v == 0 && aCtor != null && bCtor != null)
                {
                    v = aCtor.GetParameters().Length.CompareTo(bCtor.GetParameters().Length);
                }
                else if (v == 0 && aMethod != null && bMethod != null)
                {
                    v = aMethod.GetParameters().Length.CompareTo(bMethod.GetParameters().Length);
                }

                return v;
            }

            private bool GetStaticFor(ConstructorInfo ctor, PropertyInfo prop, MethodInfo method)
            {
                if (ctor != null)
                {
                    return ctor.IsStatic;
                }

                if (method != null)
                {
                    return method.IsStatic;
                }

                if (prop != null)
                {
                    var getMethod = prop.GetGetMethod();
                    var setMethod = prop.GetGetMethod();

                    return getMethod?.IsStatic == true || setMethod?.IsStatic == true;
                }

                return false;
            }

            private string GetNameFrom(ConstructorInfo ctor, PropertyInfo prop, MethodInfo method) =>
                ctor?.DeclaringType?.Name ?? prop?.Name ?? method?.Name ?? "";
        }

        private class TypeComparer : IComparer<TypeInfo>
        {
            public int Compare(TypeInfo x, TypeInfo y) =>
                x == null && y == null ? 0 :
                x == null ? -1 :
                y == null ? 1 :
                x.TypeName.CompareTo(y.TypeName);
        }

        private class TypeInfo
        {
            public readonly Type m_BaseType;
            public readonly Type m_Declaring;
            private readonly string m_FileName;
            public readonly Type[] m_Interfaces;
            private readonly string m_LinkName;
            public readonly Type m_Type;
            private readonly string m_TypeName;
            public List<TypeInfo> m_Derived, m_Nested;

            public TypeInfo(Type type)
            {
                m_Type = type;

                m_BaseType = type.BaseType;
                m_Declaring = type.DeclaringType;
                m_Interfaces = type.GetInterfaces();

                FormatGeneric(m_Type, out m_TypeName, out m_FileName, out m_LinkName);
            }

            public string FileName => m_FileName;
            public string TypeName => m_TypeName;

            public string LinkName(string dirRoot) => m_LinkName.Replace("@directory@", dirRoot);
        }

        private class SpeechEntry
        {
            public SpeechEntry(int index)
            {
                Index = index;
                Strings = new List<string>();
            }

            public int Index { get; }

            public List<string> Strings { get; }
        }

        private class SpeechEntrySorter : IComparer<SpeechEntry>
        {
            public int Compare(SpeechEntry x, SpeechEntry y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                return x?.Index.CompareTo(y?.Index) ?? 1;
            }
        }

        public class DocCommandEntry
        {
            public DocCommandEntry(AccessLevel accessLevel, string name, string[] aliases, string usage, string description)
            {
                AccessLevel = accessLevel;
                Name = name;
                Aliases = aliases;
                Usage = usage;
                Description = description;
            }

            public AccessLevel AccessLevel { get; }

            public string Name { get; }

            public string[] Aliases { get; }

            public string Usage { get; }

            public string Description { get; }
        }

        public class CommandEntrySorter : IComparer<DocCommandEntry>
        {
            public int Compare(DocCommandEntry a, DocCommandEntry b)
            {
                if (a == null && b == null)
                {
                    return 0;
                }

                var v = b?.AccessLevel.CompareTo(a?.AccessLevel) ?? 1;

                if (v != 0)
                {
                    return v;
                }

                return a?.Name.CompareTo(b?.Name) ?? 1;
            }
        }
    }

    public enum ModelBodyType
    {
        Invalid = -1,
        Monsters,
        Sea,
        Animals,
        Human,
        Equipment
    }

    public class BodyEntry
    {
        public BodyEntry(Body body, ModelBodyType bodyType, string name)
        {
            Body = body;
            BodyType = bodyType;
            Name = name;
        }

        public Body Body { get; }

        public ModelBodyType BodyType { get; }

        public string Name { get; }

        public override bool Equals(object obj)
        {
            var e = obj as BodyEntry;

            return Body == e?.Body && BodyType == e.BodyType && Name == e.Name;
        }

        public override int GetHashCode() => Body.BodyID ^ (int)BodyType ^ Name.GetHashCode();
    }

    public class BodyEntrySorter : IComparer<BodyEntry>
    {
        public int Compare(BodyEntry a, BodyEntry b)
        {
            if (a == null && b == null)
            {
                return 0;
            }

            var v = a?.BodyType.CompareTo(b?.BodyType) ?? 1;

            if (v == 0)
            {
                v = a?.Body.BodyID.CompareTo(b?.Body.BodyID) ?? 1;
            }

            if (v != 0)
            {
                return v;
            }

            return a?.Name.CompareTo(b?.Name) ?? 1;
        }
    }
}
