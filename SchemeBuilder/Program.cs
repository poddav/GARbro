using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using GameRes.Compression;

namespace GameRes
{
    partial class SchemeBuilder
    {
        static int Main (string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine ("Serialize GameRes schemes into given file");
                Console.WriteLine ("Usage: {0} FILENAME", AppDomain.CurrentDomain.FriendlyName);
                return 1;
            }
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                if (0 == args.Length)
                {
                    Console.WriteLine ("Formats.dat filename required.");
                    return 1;
                }
                Console.WriteLine ("Serializing schemes...");
                int version = assembly.GetName().Version.Revision;
                var db = new SchemeDataBase {
                    Version = version,
                    SchemeMap = Scheme,
                    GameMap = GameMap,
                };
                using (var file = File.Create (args[0]))
                    FormatCatalog.Instance.SerializeScheme (file, db);
                return 0;
            }
            catch (Exception X)
            {
                Console.Error.WriteLine (X.Message);
                return 1;
            }
        }

        internal static Stream OpenResource (string name)
        {
            var assembly = typeof(SchemeBuilder).Assembly;
            string qualified_name = "GameRes.Resources." + name;
            Stream stream = assembly.GetManifestResourceStream (qualified_name);
            if (null == stream)
            {
                Console.WriteLine ("{0}: resouce not found", name);
                throw new FileNotFoundException ("Resource not found", name);
            }
            return stream;
        }

        internal static byte[] LoadResource (string name)
        {
            using (var stream = OpenResource (name))
            {
                var res = new byte[stream.Length];
                stream.Read (res, 0, res.Length);
                return res;
            }
        }

        static Dictionary<byte[], byte[]> s_ByteArrayCache;
        static Dictionary<byte[], byte[]> ByteArrayCache { get {
            return s_ByteArrayCache ?? (s_ByteArrayCache = new Dictionary<byte[], byte[]> (new ArrayValueComparer()));
        } }

        internal static byte[] ByteArrayRef (params byte[] data)
        {
            byte[] stored;
            if (!ByteArrayCache.TryGetValue (data, out stored))
            {
                stored = ByteArrayCache[data] = data;
            }
            return stored;
        }
    }

    internal class ArrayValueComparer : IEqualityComparer<byte[]>
    {
        public bool Equals (byte[] a1, byte[] a2)
        {
            if (a1 == null)
                return a2 == null;
            if (a2 == null)
                return false;
            if (a1.Length != a2.Length)
                return false;
            for (int i = 0; i < a1.Length; ++i)
            {
                if (a1[i] != a2[i])
                    return false;
            }
            return true;
        }

        public int GetHashCode (byte[] data)
        {
            const int p = 16777619;
            int hash = unchecked((int)2166136261);

            for (int i = 0; i < data.Length; ++i)
                hash = (hash ^ data[i]) * p;

            hash += hash << 13;
            hash ^= hash >> 7;
            hash += hash << 3;
            hash ^= hash >> 17;
            hash += hash << 5;
            return hash;
        }
    }
}
