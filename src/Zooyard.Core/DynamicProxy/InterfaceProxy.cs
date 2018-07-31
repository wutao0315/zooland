namespace Zooyard.Core.DynamicProxy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using Zooyard.Core.Utils;

    /// <summary>
    /// Dynamic Proxy Interface
    /// </summary>
    public partial class InterfaceProxy
    {
        private class Map
        {
            public Type New
            {
                get;
                set;
            }

            public Type Org
            {
                get;
                set;
            }
        }

        private static IList<Map> maps = null;

        public static T New<T>(IInterceptor hanlder) where T : class
        {
            object value = New(typeof(T), hanlder);
            if (value == null)
            {
                return null;
            }
            return (T)value;
        }

        public static object New(Type clazz, IInterceptor hanlder)
        {
            if (clazz == null || !clazz.IsInterface)
            {
                throw new ArgumentException("clazz");
            }
            if (hanlder == null)
            {
                throw new ArgumentException("hanlder");
            }
            lock (maps)
            {
                var type = GetType(clazz);
                if (type == null)
                {
                    type = CreateType(clazz);
                    maps.Add(new Map() { New = type, Org = clazz });
                }

                return Activator.CreateInstance(type, hanlder);
            }
        }
    }

    public partial class InterfaceProxy
    {
        private const MethodAttributes METHOD_ATTRIBUTES = MethodAttributes.Public | MethodAttributes.NewSlot |
            MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig;

        private const TypeAttributes TYPE_ATTRIBUTES = TypeAttributes.Public | TypeAttributes.Sealed |
            TypeAttributes.Serializable;

        private const FieldAttributes FIELD_ATTRIBUTES = FieldAttributes.Private;

        private const CallingConventions CALLING_CONVENTIONS = CallingConventions.HasThis;

        private const PropertyAttributes PROPERTY_ATTRIBUTES = PropertyAttributes.SpecialName;

        private static ModuleBuilder MODULE_BUILDER = null;

        static InterfaceProxy()
        {
            maps = new List<Map>();
            var an = new AssemblyName("?");
            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
            MODULE_BUILDER = ab.DefineDynamicModule(an.Name);
        }

        private static Type GetType(Type clazz)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map.Org == clazz)
                {
                    return map.New;
                }
            }
            return null;
        }

        private static void CreateConstructor(TypeBuilder tb, FieldBuilder fb)
        {
            var args = new Type[] { typeof(IInterceptor)};
            var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, args);
            var il = ctor.GetILGenerator();
            //
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fb);
            

            il.Emit(OpCodes.Ret);
        }

        private static FieldBuilder CreateField(TypeBuilder tb)
        {
            return tb.DefineField("handler", typeof(IInterceptor), FIELD_ATTRIBUTES);
        }
        

        private static Type CreateType(Type clazz)
        {
            var tb = MODULE_BUILDER.DefineType($"Zooyard.Proxy.{clazz.Namespace}_{clazz.Name}");
            tb.AddInterfaceImplementation(clazz);
            //
            var fb = CreateField(tb);
            //
            CreateConstructor(tb, fb);
            CreateMethods(clazz, tb, fb);
            //
            return tb.CreateType();
        }


        private static void CreateMethods(Type clazz, TypeBuilder tb, FieldBuilder fb)
        {
            var methods = clazz.GetMethods();
            foreach (var met in clazz.GetMethods())
            {
                CreateMethod(met, tb, fb);
            }
        }

        private static Type[] GetParameters(ParameterInfo[] pis)
        {
            Type[] buffer = new Type[pis.Length];
            for (int i = 0; i < pis.Length; i++)
            {
                buffer[i] = pis[i].ParameterType;
            }
            return buffer;
        }

        private static MethodBuilder CreateMethod(MethodInfo met, TypeBuilder tb, FieldBuilder fb)
        {
            ParameterInfo[] args = met.GetParameters();
            MethodBuilder mb = tb.DefineMethod(met.Name, METHOD_ATTRIBUTES, met.ReturnType, GetParameters(args));
            ILGenerator il = mb.GetILGenerator();
            il.DeclareLocal(typeof(object[]));

            if (met.ReturnType != typeof(void))
            {
                il.DeclareLocal(met.ReturnType);
            }

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldc_I4, args.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc_0);

            for (int i = 0; i < args.Length; i++)
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, (1 + i));
                il.Emit(OpCodes.Box, args[i].ParameterType);
                il.Emit(OpCodes.Stelem_Ref);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fb);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, met.Name);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, typeof(IInterceptor).GetMethod("Intercept", BindingFlags.Instance | BindingFlags.Public));
            
            if (met.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                il.Emit(OpCodes.Unbox_Any, met.ReturnType);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Ldloc_1);
            }
            il.Emit(OpCodes.Ret);
            //
            return mb;
        }
        
    }
}
