using Consonance.Protocol;
using LibSharpHelp;
using SQLite.Net;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Globalization;
using SQLite.Net.Interop;
using System.Threading;

namespace Consonance
{
    // The trouble is with converting the predicates to sql, which is difficult to do without
    // direction from the router.  Perhaps the mapping should be just to a dictionary always...
    // If the predicates are on dicts, should be able to generate the queries?
    class NoModelRouter : IModelRouter
    {
        public bool GetTableIdentifier<T>(out int id)
        {
            id = 0; return false;
        }

        public bool GetTableRoute<T>(out string tabl, out string[] columns, out Type[] colTypes)
        {
            columns = null; colTypes = null; tabl = null; return false;
        }
    }


    class SqliteKeyToProxy<F,T> : IKeyTo<T> where T : class, IPrimaryKey where F : class, IPrimaryKey
    {
        readonly SqliteKeyTo<F,T> proxy;
        public SqliteKeyToProxy(F from_this, int from_cid, int from_pid, int to_cid, SQLiteConnection conn, TableMapping tmap, TableMapping fmap, ReaderWriterLockSlim Sync)
        {
            proxy = new SqliteKeyTo<F,T>(
                () => from_this.id,
                t => t.id,
                from_pid,
                from_cid,
                to_cid,
                conn,tmap,fmap,Sync
                );
        }
        public void Add(IEnumerable<T> values) => proxy.Add(values); 
        public void Clear() => proxy.Clear();
        public void Commit() => proxy.Commit();
        public IEnumerable<T> Get() => proxy.Get();
        public void Remove(IEnumerable<T> values) => proxy.Remove(values);
        public void Replace(IEnumerable<T> values) => proxy.Replace(values);
    }



    class SqliteDal : IDAL
    {
        ReaderWriterLockSlim Sync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        AttributeForwarderGenerator agen;
        SQLiteConnection cconn;
        public SqliteDal(IPlatform platform)
        {
            agen = new AttributeForwarderGenerator(new Dictionary<Type, Func<Attribute>>
                {
                    { typeof(Protocol.AutoIncrementAttribute), () => new SQLite.Net.Attributes.AutoIncrementAttribute() },
                    { typeof(Protocol.ColumnAccessorAttribute), () => new SQLite.Net.Attributes.ColumnAccessorAttribute() },
                    { typeof(Protocol.IgnoreAttribute), () => new SQLite.Net.Attributes.IgnoreAttribute() },
                    { typeof(Protocol.IndexedAttribute), () => new SQLite.Net.Attributes.IndexedAttribute() },
                    { typeof(Protocol.PrimaryKeyAttribute), () => new SQLite.Net.Attributes.PrimaryKeyAttribute() },
                }
            );
            var datapath = platform.filesystem.AppData;
            platform.filesystem.CreateDirectory(datapath);
            var maindbpath = Path.Combine(datapath, "Consonance.db");
            //platform.filesystem.Delete(maindbpath);
            //byte[] file = platform.filesystem.ReadFile(maindbpath);
            cconn = new SQLiteConnection(platform.sqlite, maindbpath, false);
        }
        SqliteDal() { } // Inner.

        #region routing   

        public IDAL Routed(IModelRouter router)
        {
            return new SqliteDal { cconn = this.cconn, router = router, agen = this.agen, Sync = this.Sync };
        }

        // Check against the custom table mapped type.
        IModelRouter router;
        public class AttributeForwarderGenerator
        {
            IDictionary<Type, Func<Attribute>> forwards;
            public AttributeForwarderGenerator(IDictionary<Type, Func<Attribute>> forwards)
            {
                this.forwards = forwards;
            }

            public ITypeInfo Generate(PropertyInfo toProxy)
            {
                return new PropertyInfoProxy(toProxy, forwards);
            }

            class PropertyInfoProxy : ITypeInfo
            {
                IDictionary<Type, Func<Attribute>> forwards;
                PropertyInfo o;
                public PropertyInfoProxy(PropertyInfo o, IDictionary<Type, Func<Attribute>> forwards)
                {
                    this.o = o;
                    this.forwards = forwards;
                }

                public MethodInfo GetMethod { get { return o.GetMethod; } }
                public Type DeclaringType { get { return o.DeclaringType; } }
                public string Name { get { return o.Name; } }
                public Type Type { get { return o.PropertyType; } }
                public IEnumerable<T> GetCustomAttributes<T>(bool inherit = false) where T : Attribute
                {
                    var fwd = from a in o.GetCustomAttributes(inherit)
                              select forwards?[a.GetType()]() ?? a;
                    return from a in fwd where a is T select (T)a;
                }
                public object GetValue(object instance)
                {
                    return o.GetValue(instance);
                }
                public void SetValue(object instance, object value)
                {
                    o.SetValue(instance, value);
                }
            }
        }

        class FakedProperty : ITypeInfo
        {
            public FakedProperty(string name, Type type, Type decl, Func<object, object> get, Action<object, object> set)
            {
                DeclaringType = decl;
                Type = type;
                Name = name;
                g = get;
                s = set;
            }
            public MethodInfo GetMethod { get { return g.Method; } }
            public Type DeclaringType { get; set; }
            public string Name { get; set; }
            public Type Type { get; set; }
            public IEnumerable<T> GetCustomAttributes<T>(bool inherit = false) where T : Attribute {yield break;}
            readonly Func<object, object> g;
            public object GetValue(object instance) => g(instance);
            readonly Action<object, object> s;
            public void SetValue(object instance, object value) => s(instance, value);
        }

        Dictionary<Type, TableMapping> cache = new Dictionary<Type, TableMapping>();
        TableMapping GetTableMap<T>()
        {
            var t = typeof(T);
            if (cache.ContainsKey(t))
                return cache[t];

            var rp = t.GetRuntimeProperties();
            var forwarded = rp.Select(d => agen.Generate(d)).ToArray();

            String tn; String[] c; Type[] ct;
            if (router == null || !router.GetTableRoute<T>(out tn, out c, out ct))
                return new TableMapping(t, forwarded);

            var holder = rp.Where(p => p.GetCustomAttributes().Any(a => a is Protocol.ColumnAccessorAttribute)).First();

            // make the poisoned map%
            return new TableMapping(t,
                forwarded.Concat(Enumerable.Range(0, c.Length).Select(i =>
                {
                    var hh = new HolderHelper(holder, c[i]);
                    return new FakedProperty(c[i], ct[i], t, hh.GetVal, hh.SetVal);
                })),
                tn
            );
        }

        // So that the columnaccessor getset methods are not equal directly.
        class HolderHelper
        {
            readonly PropertyInfo hpi;
            readonly string k;
            public HolderHelper(PropertyInfo hpi, string k)
            {
                this.hpi = hpi;
                this.k = k;
            }
            public Object GetVal(Object f) => hpi.GetValue(f, new[] { k });
            public void SetVal(Object f, Object val) => hpi.SetValue(f, val,new[] { k });
        }

        #endregion

        public void CreateTable<T>() where T : class
        {
            using(Sync.WriteLock())
                cconn.CreateTable(GetTableMap<T>());
        }
        public void DropTable<T>() where T : class
        {
            using (Sync.WriteLock())
                cconn.DropTable(GetTableMap<T>());
        }
        public void Delete<T>(Expression<Func<T, bool>> pred) where T : class
        {
            using (Sync.ReadLock())
            {
                if (pred == null) cconn.DeleteAll(GetTableMap<T>());
                else cconn.Table<T>(GetTableMap<T>()).Delete(pred);
            }
        }

        public void Commit<T>(T item) where T : class
        {
            using (Sync.ReadLock())
            {
                var tm = GetTableMap<T>();
                if (cconn.Find(tm.PK?.GetValue(item), tm) != null)
                    cconn.Update(item, tm);
                else
                    cconn.Insert(item, tm);
            }
        }

        public IEnumerable<T> Get<T>(Expression<Func<T, bool>> where) where T : class
        {
            var tq = cconn.Table<T>(GetTableMap<T>());
            if (where != null) tq=tq.Where(where);
            return new LockedEnumerable<T>(tq, Sync);
        }

        public int Update<T,X>(Expression<Func<T,X>> selector, X value, Expression<Func<T, bool>> where = null) where T : class
        {
            using (Sync.ReadLock())
            {
                var tq = cconn.Table<T>(GetTableMap<T>());
                List<Object> query_args_output = new List<object>();
                Expression<Func<X>> exp_value = () => value;
                TableQuery<T>.CompileResult 
                    selector_command = tq.CompileExpr(selector.Body, query_args_output),
                    value_command = tq.CompileExpr(exp_value.Body, query_args_output),
                    where_command = new TableQuery<T>.CompileResult { Value = null, CommandText = "" }; 
                if (where != null) where_command = tq.CompileExpr(where.Body, query_args_output); // add more args
                var update_command = cconn.CreateCommand(
                    String.Format("UPDATE {0} SET {1} = {2} WHERE {3}",
                        tq.Table.TableName,
                        selector_command.CommandText,
                        value_command.CommandText,
                        where_command.CommandText),
                    query_args_output.ToArray()
                );
                return update_command.ExecuteNonQuery();
            }
        }

        public int Count<T>(Expression<Func<T, bool>> where = null) where T : class
        {
            using (Sync.ReadLock())
            {
                var tq = cconn.Table<T>(GetTableMap<T>());
                if (where == null) return tq.Count();
                else return tq.Count(where);
            }
        }

        int FindFMid<T>()
        {
            var tt = typeof(T);
            int tid = 0;
            if (router != null && router.GetTableIdentifier<T>(out tid)) return tid;
            var tat = tt.GetCustomAttribute<TableIdentifierAttribute>(true) as TableIdentifierAttribute;
            if (tat == null) throw new ArgumentException("TableIdentifier Attribute missing on " + tt.Name);
            return tat.id;
        }

        public IKeyTo<T> CreateOneToMany<F,T>(F from_this, int from_pid)
            where T : class, IPrimaryKey
            where F : class, IPrimaryKey
        {
            using (Sync.WriteLock())
            {
                cconn.CreateTable(GetTableMap<F>());
                cconn.CreateTable(GetTableMap<T>());
            }
            var f_cid = FindFMid<F>();
            var t_cid = FindFMid<T>();
            return new SqliteKeyToProxy<F, T>(from_this, f_cid, from_pid, t_cid, cconn, GetTableMap<T>(), GetTableMap<F>(), Sync);
        }
    }
}