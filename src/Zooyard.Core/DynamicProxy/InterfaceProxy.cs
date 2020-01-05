using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Core.Utils;

namespace Zooyard.Core.DynamicProxy
{


    /// <summary>
    /// Dynamic Proxy Interface
    /// </summary>
    public partial class InterfaceProxy
    {
        private class Map
        {
            public Type New { get; set; }

            public Type Org { get; set; }
        }

        private static IList<Map> maps = null;

        public static T New<T>(IInterceptor hanlder)
        {
            var value = New(typeof(T), hanlder);
            if (value == null)
            {
                return default(T);
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
                    type = CreateImplType(clazz);
                    maps.Add(new Map() { New = type, Org = clazz });
                }

                return Activator.CreateInstance(type, hanlder);
            }
        }
    }

    public partial class InterfaceProxy
    {
        private const MethodAttributes METHOD_ATTRIBUTES = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig;

        //private const FieldAttributes FIELD_ATTRIBUTES = FieldAttributes.Private;


        private const string ProxyAssemblyName = "Zooyard.Core.DynamicProxy.Generator";
        private static ModuleBuilder MODULE_BUILDER = null;

        static InterfaceProxy()
        {
            maps = new List<Map>();
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ProxyAssemblyName), AssemblyBuilderAccess.RunAndCollect);
            MODULE_BUILDER = asmBuilder.DefineDynamicModule("core");
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

        //private static void CreateConstructor(TypeBuilder tb, FieldBuilder fb)
        //{
        //    var args = new Type[] { typeof(IInterceptor)};
        //    var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, args);
        //    var il = ctor.GetILGenerator();
        //    //
        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Ldarg_1);
        //    il.Emit(OpCodes.Stfld, fb);
            

        //    il.Emit(OpCodes.Ret);
        //}

        //private static FieldBuilder CreateField(TypeBuilder tb)
        //{
        //    return tb.DefineField("_handler", typeof(IInterceptor), FIELD_ATTRIBUTES);
        //}
        

        //private static TypeInfo CreateType(Type clazz)
        //{
        //    var tb = MODULE_BUILDER.DefineType($"Zooyard.Proxy.{clazz.Namespace}_{clazz.Name}");
        //    tb.AddInterfaceImplementation(clazz);
        //    //
        //    var fb = CreateField(tb);
        //    //
        //    CreateConstructor(tb, fb);
        //    CreateMethods(clazz, tb, fb);
        //    //
        //    return tb.CreateTypeInfo();
        //}




        //private static void CreateMethods(Type clazz, TypeBuilder tb, FieldBuilder fb)
        //{
        //    var methods = clazz.GetMethods();
        //    foreach (var met in clazz.GetMethods())
        //    {
        //        CreateMethod(met, tb, fb);
        //    }
        //}

        //private static Type[] GetParameters(ParameterInfo[] pis)
        //{
        //    Type[] buffer = new Type[pis.Length];
        //    for (int i = 0; i < pis.Length; i++)
        //    {
        //        buffer[i] = pis[i].ParameterType;
        //    }
        //    return buffer;
        //}

        //private static MethodBuilder CreateMethod(MethodInfo met, TypeBuilder tb, FieldBuilder fb)
        //{
        //    ParameterInfo[] args = met.GetParameters();
        //    MethodBuilder mb = tb.DefineMethod(met.Name, METHOD_ATTRIBUTES, met.ReturnType, GetParameters(args));
        //    ILGenerator il = mb.GetILGenerator();
        //    il.DeclareLocal(typeof(object[]));

        //    if (met.ReturnType != typeof(void))
        //    {
        //        il.DeclareLocal(met.ReturnType);
        //    }

        //    il.Emit(OpCodes.Nop);
        //    il.Emit(OpCodes.Ldc_I4, args.Length);
        //    il.Emit(OpCodes.Newarr, typeof(object));
        //    il.Emit(OpCodes.Stloc_0);

        //    for (int i = 0; i < args.Length; i++)
        //    {
        //        il.Emit(OpCodes.Ldloc_0);
        //        il.Emit(OpCodes.Ldc_I4, i);
        //        il.Emit(OpCodes.Ldarg, (1 + i));
        //        il.Emit(OpCodes.Box, args[i].ParameterType);
        //        il.Emit(OpCodes.Stelem_Ref);
        //    }

        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Ldfld, fb);
        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Ldstr, met.Name);
        //    il.Emit(OpCodes.Ldloc_0);
        //    il.Emit(OpCodes.Call, typeof(IInterceptor).GetMethod("Intercept", BindingFlags.Instance | BindingFlags.Public));
            
        //    if (met.ReturnType == typeof(void))
        //    {
        //        il.Emit(OpCodes.Pop);
        //    }
        //    else
        //    {
        //        il.Emit(OpCodes.Unbox_Any, met.ReturnType);
        //        il.Emit(OpCodes.Stloc_1);
        //        il.Emit(OpCodes.Ldloc_1);
        //    }
        //    il.Emit(OpCodes.Ret);
        //    //
        //    return mb;
        //}



        private static TypeInfo CreateImplType(Type interfaceType) 
        {
            var interfaceTypes = new Type[] { interfaceType };
            var implTypeBuilder = MODULE_BUILDER.DefineType($"Zooyard.Proxy.{interfaceType.Namespace}_{interfaceType.Name}", TypeAttributes.Public, typeof(object), interfaceTypes);

            var fields = FieldBuilderUtils.DefineFields(implTypeBuilder);

            var typeDesc = new TypeDesc(interfaceType, implTypeBuilder, fields, new MethodConstantTable(implTypeBuilder));
            //define constructor
            DefineConstructor(typeDesc);
            //define methods
            DefineMethods(interfaceType, typeDesc);

            return typeDesc.Compile();
        }


        private static void DefineConstructor(TypeDesc typeDesc)
        {
            var constructorBuilder = typeDesc.Builder.DefineConstructor(MethodAttributes.Public, MethodUtils.ObjectCtor.CallingConvention, new Type[] { typeof(IInterceptor) });

            constructorBuilder.DefineParameter(1, ParameterAttributes.None, FieldBuilderUtils.Handle);

            var ilGen = constructorBuilder.GetILGenerator();
            ilGen.EmitThis();
            ilGen.Emit(OpCodes.Call, MethodUtils.ObjectCtor);

            ilGen.EmitThis();
            ilGen.EmitLoadArg(1);
            ilGen.Emit(OpCodes.Stfld, typeDesc.Fields[FieldBuilderUtils.Handle]);

            ilGen.Emit(OpCodes.Ret);
        }



        private static void DefineMethods(Type clazz, TypeDesc typeDesc)
        {
            var methods = clazz.GetMethods();
            foreach (var met in clazz.GetMethods())
            {
                DefineMethod(met, typeDesc);
            }
        }


        private static MethodBuilder DefineMethod(MethodInfo met, TypeDesc typeDesc)
        {
            var args = met.GetParameterTypes();
            MethodBuilder mb = typeDesc.Builder.DefineMethod(met.Name, METHOD_ATTRIBUTES, met.ReturnType, args);
            ILGenerator il = mb.GetILGenerator();

            var argsLocal = il.DeclareLocal(typeof(object[]));
            var returnLocal = il.DeclareLocal(typeof(object));

            il.EmitInt(args.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            for (int i = 0; i < args.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.EmitInt(i);
                il.EmitLoadArg(i + 1);
                if (args[i].IsByRef)
                {
                    il.EmitLdRef(args[i]);
                    il.EmitConvertToObject(args[i].GetElementType());
                }
                else
                {
                    il.EmitConvertToObject(args[i]);
                }
                il.Emit(OpCodes.Stelem_Ref);
            }
            il.Emit(OpCodes.Stloc, argsLocal);

            il.EmitThis();
            il.Emit(OpCodes.Ldfld, typeDesc.Fields[FieldBuilderUtils.Handle]);
            il.EmitThis();
            il.EmitString(met.Name);
            il.Emit(OpCodes.Ldloc, argsLocal);
            il.Emit(OpCodes.Callvirt, MethodUtils.InterceptInvoke);
            il.Emit(OpCodes.Stloc, returnLocal);

            if (met.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, returnLocal);
                il.EmitConvertFromObject(met.ReturnType);
            }
            il.Emit(OpCodes.Ret);

            return mb;
        }
        private class ParameterBuilderUtils
        {
            public static void DefineParameters(MethodInfo targetMethod, MethodBuilder methodBuilder)
            {
                var parameters = targetMethod.GetParameters();
                if (parameters.Length > 0)
                {
                    var paramOffset = 1;   // 1
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        var parameterBuilder = methodBuilder.DefineParameter(i + paramOffset, parameter.Attributes, parameter.Name);
                        if (parameter.HasDefaultValue)
                        {
                            if (!(parameter.ParameterType.GetTypeInfo().IsValueType && parameter.DefaultValue == null))
                                parameterBuilder.SetConstant(parameter.DefaultValue);
                        }
                        foreach (var attribute in parameter.CustomAttributes)
                        {
                            parameterBuilder.SetCustomAttribute(CustomAttributeBuildeUtils.DefineCustomAttribute(attribute));
                        }
                    }
                }

                var returnParamter = targetMethod.ReturnParameter;
                var returnParameterBuilder = methodBuilder.DefineParameter(0, returnParamter.Attributes, returnParamter.Name);
                foreach (var attribute in returnParamter.CustomAttributes)
                {
                    returnParameterBuilder.SetCustomAttribute(CustomAttributeBuildeUtils.DefineCustomAttribute(attribute));
                }
            }
        }
        private class CustomAttributeBuildeUtils
        {
            public static CustomAttributeBuilder DefineCustomAttribute(CustomAttributeData customAttributeData)
            {
                if (customAttributeData.NamedArguments != null)
                {
                    var attributeTypeInfo = customAttributeData.AttributeType.GetTypeInfo();
                    var constructor = customAttributeData.Constructor;
                    //var constructorArgs = customAttributeData.ConstructorArguments.Select(c => c.Value).ToArray();
                    var constructorArgs = new object[customAttributeData.ConstructorArguments.Count];
                    for (var i = 0; i < constructorArgs.Length; i++)
                    {
                        if (customAttributeData.ConstructorArguments[i].ArgumentType.IsArray)
                        {
                            constructorArgs[i] = ((IEnumerable<CustomAttributeTypedArgument>)customAttributeData.ConstructorArguments[i].Value).
                        Select(x => x.Value).ToArray();
                        }
                        else
                        {
                            constructorArgs[i] = customAttributeData.ConstructorArguments[i].Value;
                        }

                    }
                    var namedProperties = customAttributeData.NamedArguments
                            .Where(n => !n.IsField)
                            .Select(n => attributeTypeInfo.GetProperty(n.MemberName))
                            .ToArray();
                    var propertyValues = customAttributeData.NamedArguments
                             .Where(n => !n.IsField)
                             .Select(n => n.TypedValue.Value)
                             .ToArray();
                    var namedFields = customAttributeData.NamedArguments.Where(n => n.IsField)
                             .Select(n => attributeTypeInfo.GetField(n.MemberName))
                             .ToArray();
                    var fieldValues = customAttributeData.NamedArguments.Where(n => n.IsField)
                             .Select(n => n.TypedValue.Value)
                             .ToArray();
                    return new CustomAttributeBuilder(customAttributeData.Constructor, constructorArgs
                       , namedProperties
                       , propertyValues, namedFields, fieldValues);
                }
                else
                {
                    return new CustomAttributeBuilder(customAttributeData.Constructor,
                        customAttributeData.ConstructorArguments.Select(c => c.Value).ToArray());
                }
            }
        }
        private class FieldBuilderUtils
        {
            public const string Handle = "_handle";

            public static FieldTable DefineFields(TypeBuilder typeBuilder)
            {
                var fieldTable = new FieldTable();
                fieldTable[Handle] = typeBuilder.DefineField(Handle, typeof(IInterceptor), FieldAttributes.Private);
                return fieldTable;
            }
        }
        private class MethodConstantTable
        {
            private readonly TypeBuilder _nestedTypeBuilder;
            private readonly ConstructorBuilder _constructorBuilder;
            private readonly ILGenerator _ilGen;
            private readonly Dictionary<string, FieldBuilder> _fields;

            public MethodConstantTable(TypeBuilder typeBuilder)
            {
                _fields = new Dictionary<string, FieldBuilder>();
                _nestedTypeBuilder = typeBuilder.DefineNestedType("MethodConstant", TypeAttributes.NestedPrivate);
                _constructorBuilder = _nestedTypeBuilder.DefineTypeInitializer();
                _ilGen = _constructorBuilder.GetILGenerator();
            }

            public void AddMethod(string name, MethodInfo method)
            {
                if (!_fields.ContainsKey(name))
                {
                    var field = _nestedTypeBuilder.DefineField(name, typeof(MethodInfo), FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.Assembly);
                    _fields.Add(name, field);
                    if (method != null)
                    {
                        _ilGen.EmitMethod(method);
                        _ilGen.Emit(OpCodes.Stsfld, field);
                    }
                }
            }

            public void LoadMethod(ILGenerator ilGen, string name)
            {
                if (_fields.TryGetValue(name, out FieldBuilder field))
                {
                    ilGen.Emit(OpCodes.Ldsfld, field);
                    return;
                }
                throw new InvalidOperationException($"Failed to find the method associated with the specified key {name}.");
            }

            public void Compile()
            {
                _ilGen.Emit(OpCodes.Ret);
                _nestedTypeBuilder.CreateTypeInfo();
            }
        }
        private class FieldTable
        {
            private readonly Dictionary<string, FieldBuilder> _table = new Dictionary<string, FieldBuilder>();

            public FieldBuilder this[string fieldName]
            {
                get
                {
                    return _table[fieldName];
                }
                set
                {
                    _table[value.Name] = value;
                }
            }
        }
        private class TypeDesc
        {
            public TypeBuilder Builder { get; }

            public FieldTable Fields { get; }

            public MethodConstantTable MethodConstants { get; }

            public Dictionary<string, object> Properties { get; }

            public Type ServiceType { get; }

            public TypeDesc(Type serviceType, TypeBuilder typeBuilder, FieldTable fields, MethodConstantTable methodConstants)
            {
                ServiceType = serviceType;
                Builder = typeBuilder;
                Fields = fields;
                MethodConstants = methodConstants;
                Properties = new Dictionary<string, object>();
            }

            public TypeInfo Compile()
            {
                MethodConstants.Compile();
                return Builder.CreateTypeInfo();
            }

            public T GetProperty<T>()
            {
                return (T)Properties[typeof(T).Name];
            }
        }
    }
    internal static class MethodUtils
    {
        internal static readonly ConstructorInfo ObjectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.Single();
        internal static readonly MethodInfo InterceptInvoke = GetMethod<IInterceptor>(nameof(IInterceptor.Intercept));

        private static MethodInfo GetMethod<T>(Expression<T> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression == null)
            {
                throw new InvalidCastException("Cannot be converted to MethodCallExpression");
            }
            return methodCallExpression.Method;
        }

        private static MethodInfo GetMethod<T>(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return typeof(T).GetTypeInfo().GetMethod(name);
        }
    }
    public static class MethodExtensions
    {
        internal static Type[] GetParameterTypes(this MethodBase method)
        {
            return method.GetParameters().Select(x => x.ParameterType).ToArray();
        }
        internal static MethodInfo GetMethod<T>(Expression<T> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression == null)
            {
                throw new InvalidCastException("Cannot be converted to MethodCallExpression");
            }
            return methodCallExpression.Method;
        }
    }
    public static class ILGeneratorExtensions
    {
        public static void EmitLoadArg(this ILGenerator ilGenerator, int index)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }

            switch (index)
            {
                case 0:
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    ilGenerator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (index <= byte.MaxValue) ilGenerator.Emit(OpCodes.Ldarg_S, (byte)index);
                    else ilGenerator.Emit(OpCodes.Ldarg, index);
                    break;
            }
        }

        public static void EmitLoadArgA(this ILGenerator ilGenerator, int index)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }

            if (index <= byte.MaxValue) ilGenerator.Emit(OpCodes.Ldarga_S, (byte)index);
            else ilGenerator.Emit(OpCodes.Ldarga, index);
        }

        public static void EmitConvertToObject(this ILGenerator ilGenerator, Type typeFrom)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (typeFrom == null)
            {
                throw new ArgumentNullException(nameof(typeFrom));
            }

            if (typeFrom.GetTypeInfo().IsGenericParameter)
            {
                ilGenerator.Emit(OpCodes.Box, typeFrom);
            }
            else
            {
                ilGenerator.EmitConvertToType(typeFrom, typeof(object), true);
            }
        }

        public static void EmitConvertFromObject(this ILGenerator ilGenerator, Type typeTo)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (typeTo == null)
            {
                throw new ArgumentNullException(nameof(typeTo));
            }

            if (typeTo.GetTypeInfo().IsGenericParameter)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeTo);
            }
            else
            {
                ilGenerator.EmitConvertToType(typeof(object), typeTo, true);
            }
        }

        public static void EmitThis(this ILGenerator ilGenerator)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }

            ilGenerator.EmitLoadArg(0);
        }

        public static void EmitType(this ILGenerator ilGenerator, Type type)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            ilGenerator.Emit(OpCodes.Ldtoken, type);
            ilGenerator.Emit(OpCodes.Call, MethodInfoConstant.GetTypeFromHandle);
        }

        public static void EmitMethod(this ILGenerator ilGenerator, MethodInfo method)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            EmitMethod(ilGenerator, method, method.DeclaringType);
        }

        public static void EmitMethod(this ILGenerator ilGenerator, MethodInfo method, Type declaringType)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (declaringType == null)
            {
                throw new ArgumentNullException(nameof(declaringType));
            }

            ilGenerator.Emit(OpCodes.Ldtoken, method);
            ilGenerator.Emit(OpCodes.Ldtoken, method.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, MethodInfoConstant.GetMethodFromHandle);
            ilGenerator.EmitConvertToType(typeof(MethodBase), typeof(MethodInfo));
        }

        public static void EmitConvertToType(this ILGenerator ilGenerator, Type typeFrom, Type typeTo, bool isChecked = true)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (typeFrom == null)
            {
                throw new ArgumentNullException(nameof(typeFrom));
            }
            if (typeTo == null)
            {
                throw new ArgumentNullException(nameof(typeTo));
            }

            var typeFromInfo = typeFrom.GetTypeInfo();
            var typeToInfo = typeTo.GetTypeInfo();

            var nnExprType = typeFromInfo.GetNonNullableType();
            var nnType = typeToInfo.GetNonNullableType();

            if (TypeInfoUtils.AreEquivalent(typeFromInfo, typeToInfo))
            {
                return;
            }

            if (typeFromInfo.IsInterface || // interface cast
              typeToInfo.IsInterface ||
               typeFrom == typeof(object) || // boxing cast
               typeTo == typeof(object) ||
               typeFrom == typeof(System.Enum) ||
               typeFrom == typeof(System.ValueType) ||
               TypeInfoUtils.IsLegalExplicitVariantDelegateConversion(typeFromInfo, typeToInfo))
            {
                ilGenerator.EmitCastToType(typeFromInfo, typeToInfo);
            }
            else if (typeFromInfo.IsNullableType() || typeToInfo.IsNullableType())
            {
                ilGenerator.EmitNullableConversion(typeFromInfo, typeToInfo, isChecked);
            }
            else if (!(typeFromInfo.IsConvertible() && typeToInfo.IsConvertible()) // primitive runtime conversion
                     &&
                     (nnExprType.GetTypeInfo().IsAssignableFrom(nnType) || // down cast
                     nnType.GetTypeInfo().IsAssignableFrom(nnExprType))) // up cast
            {
                ilGenerator.EmitCastToType(typeFromInfo, typeToInfo);
            }
            else if (typeFromInfo.IsArray && typeToInfo.IsArray)
            {
                // See DevDiv Bugs #94657.
                ilGenerator.EmitCastToType(typeFromInfo, typeToInfo);
            }
            else
            {
                ilGenerator.EmitNumericConversion(typeFromInfo, typeToInfo, isChecked);
            }
        }

        public static void EmitCastToType(this ILGenerator ilGenerator, TypeInfo typeFrom, TypeInfo typeTo)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (!typeFrom.IsValueType && typeTo.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeTo.AsType());
            }
            else if (typeFrom.IsValueType && !typeTo.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, typeFrom.AsType());
                if (typeTo.AsType() != typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Castclass, typeTo.AsType());
                }
            }
            else if (!typeFrom.IsValueType && !typeTo.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Castclass, typeTo.AsType());
            }
            else
            {
                throw new InvalidCastException($"Caanot cast {typeFrom} to {typeTo}.");
            }
        }

        public static void EmitHasValue(this ILGenerator ilGenerator, Type nullableType)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            MethodInfo mi = nullableType.GetTypeInfo().GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, mi);
        }

        public static void EmitGetValueOrDefault(this ILGenerator ilGenerator, Type nullableType)
        {
            MethodInfo mi = nullableType.GetTypeInfo().GetMethod("GetValueOrDefault", Type.EmptyTypes);
            ilGenerator.Emit(OpCodes.Call, mi);
        }

        public static void EmitGetValue(this ILGenerator ilGenerator, Type nullableType)
        {
            MethodInfo mi = nullableType.GetTypeInfo().GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, mi);
        }

        public static void EmitConstant(this ILGenerator ilGenerator, object value, Type valueType)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (valueType == null)
            {
                throw new ArgumentNullException(nameof(valueType));
            }
            if (value == null)
            {
                EmitDefault(ilGenerator, valueType);
                return;
            }

            if (ilGenerator.TryEmitILConstant(value, valueType))
            {
                return;
            }

            var t = value as Type;
            if (t != null)
            {
                ilGenerator.EmitType(t);
                if (valueType != typeof(Type))
                {
                    ilGenerator.Emit(OpCodes.Castclass, valueType);
                }
                return;
            }

            var mb = value as MethodBase;
            if (mb != null)
            {
                ilGenerator.EmitMethod((MethodInfo)mb);
                return;
            }

            if (valueType.GetTypeInfo().IsArray)
            {
                var array = (Array)value;
                ilGenerator.EmitArray(array, valueType.GetElementType());
            }

            throw new InvalidOperationException("Code supposed to be unreachable.");
        }

        public static void EmitDefault(this ILGenerator ilGenerator, Type type)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                case TypeCode.DateTime:
                    if (type.GetTypeInfo().IsValueType)
                    {
                        // Type.GetTypeCode on an enum returns the underlying
                        // integer TypeCode, so we won't get here.
                        // This is the IL for default(T) if T is a generic type
                        // parameter, so it should work for any type. It's also
                        // the standard pattern for structs.
                        LocalBuilder lb = ilGenerator.DeclareLocal(type);
                        ilGenerator.Emit(OpCodes.Ldloca, lb);
                        ilGenerator.Emit(OpCodes.Initobj, type);
                        ilGenerator.Emit(OpCodes.Ldloc, lb);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Ldnull);
                    }
                    break;

                case TypeCode.Empty:
                case TypeCode.String:
                    ilGenerator.Emit(OpCodes.Ldnull);
                    break;

                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    break;

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.Emit(OpCodes.Conv_I8);
                    break;

                case TypeCode.Single:
                    ilGenerator.Emit(OpCodes.Ldc_R4, default(Single));
                    break;

                case TypeCode.Double:
                    ilGenerator.Emit(OpCodes.Ldc_R8, default(Double));
                    break;

                case TypeCode.Decimal:
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.Emit(OpCodes.Newobj, typeof(Decimal).GetTypeInfo().GetConstructor(new Type[] { typeof(int) }));
                    break;

                default:
                    throw new InvalidOperationException("Code supposed to be unreachable.");
            }
        }

        public static void EmitDecimal(this ILGenerator ilGenerator, decimal value)
        {
            if (Decimal.Truncate(value) == value)
            {
                if (Int32.MinValue <= value && value <= Int32.MaxValue)
                {
                    int intValue = Decimal.ToInt32(value);
                    ilGenerator.EmitInt(intValue);
                    ilGenerator.EmitNew(typeof(Decimal).GetTypeInfo().GetConstructor(new Type[] { typeof(int) }));
                }
                else if (Int64.MinValue <= value && value <= Int64.MaxValue)
                {
                    long longValue = Decimal.ToInt64(value);
                    ilGenerator.EmitLong(longValue);
                    ilGenerator.EmitNew(typeof(Decimal).GetTypeInfo().GetConstructor(new Type[] { typeof(long) }));
                }
                else
                {
                    ilGenerator.EmitDecimalBits(value);
                }
            }
            else
            {
                ilGenerator.EmitDecimalBits(value);
            }
        }

        public static void EmitNew(this ILGenerator ilGenerator, ConstructorInfo ci)
        {
            ilGenerator.Emit(OpCodes.Newobj, ci);
        }

        public static void EmitNull(this ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldnull);
        }

        public static void EmitString(this ILGenerator ilGenerator, string value)
        {
            ilGenerator.Emit(OpCodes.Ldstr, value);
        }

        public static void EmitBoolean(this ILGenerator ilGenerator, bool value)
        {
            if (value)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
        }

        public static void EmitChar(this ILGenerator ilGenerator, char value)
        {
            ilGenerator.EmitInt(value);
            ilGenerator.Emit(OpCodes.Conv_U2);
        }

        public static void EmitByte(this ILGenerator ilGenerator, byte value)
        {
            ilGenerator.EmitInt(value);
            ilGenerator.Emit(OpCodes.Conv_U1);
        }

        public static void EmitSByte(this ILGenerator ilGenerator, sbyte value)
        {
            ilGenerator.EmitInt(value);
            ilGenerator.Emit(OpCodes.Conv_I1);
        }

        public static void EmitShort(this ILGenerator ilGenerator, short value)
        {
            ilGenerator.EmitInt(value);
            ilGenerator.Emit(OpCodes.Conv_I2);
        }

        public static void EmitUShort(this ILGenerator ilGenerator, ushort value)
        {
            ilGenerator.EmitInt(value);
            ilGenerator.Emit(OpCodes.Conv_U2);
        }

        public static void EmitInt(this ILGenerator ilGenerator, int value)
        {
            OpCode c;
            switch (value)
            {
                case -1:
                    c = OpCodes.Ldc_I4_M1;
                    break;
                case 0:
                    c = OpCodes.Ldc_I4_0;
                    break;
                case 1:
                    c = OpCodes.Ldc_I4_1;
                    break;
                case 2:
                    c = OpCodes.Ldc_I4_2;
                    break;
                case 3:
                    c = OpCodes.Ldc_I4_3;
                    break;
                case 4:
                    c = OpCodes.Ldc_I4_4;
                    break;
                case 5:
                    c = OpCodes.Ldc_I4_5;
                    break;
                case 6:
                    c = OpCodes.Ldc_I4_6;
                    break;
                case 7:
                    c = OpCodes.Ldc_I4_7;
                    break;
                case 8:
                    c = OpCodes.Ldc_I4_8;
                    break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        ilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Ldc_I4, value);
                    }
                    return;
            }
            ilGenerator.Emit(c);
        }

        public static void EmitUInt(this ILGenerator ilGenerator, uint value)
        {
            ilGenerator.EmitInt((int)value);
            ilGenerator.Emit(OpCodes.Conv_U4);
        }

        public static void EmitLong(this ILGenerator ilGenerator, long value)
        {
            ilGenerator.Emit(OpCodes.Ldc_I8, value);

            //
            // Now, emit convert to give the constant type information.
            //
            // Otherwise, it is treated as unsigned and overflow is not
            // detected if it's used in checked ops.
            //
            ilGenerator.Emit(OpCodes.Conv_I8);
        }

        public static void EmitULong(this ILGenerator ilGenerator, ulong value)
        {
            ilGenerator.Emit(OpCodes.Ldc_I8, (long)value);
            ilGenerator.Emit(OpCodes.Conv_U8);
        }

        public static void EmitDouble(this ILGenerator ilGenerator, double value)
        {
            ilGenerator.Emit(OpCodes.Ldc_R8, value);
        }

        public static void EmitSingle(this ILGenerator ilGenerator, float value)
        {
            ilGenerator.Emit(OpCodes.Ldc_R4, value);
        }

        public static void EmitArray(this ILGenerator ilGenerator, Array items, Type elementType)
        {
            ilGenerator.EmitInt(items.Length);
            ilGenerator.Emit(OpCodes.Newarr, elementType);
            for (int i = 0; i < items.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.EmitInt(i);
                ilGenerator.EmitConstant(items.GetValue(i), elementType);
                ilGenerator.EmitStoreElement(elementType);
            }
        }

        public static void EmitStoreElement(this ILGenerator ilGenerator, Type type)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                ilGenerator.Emit(OpCodes.Stelem, type);
                return;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    ilGenerator.Emit(OpCodes.Stelem_I1);
                    break;
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    ilGenerator.Emit(OpCodes.Stelem_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    ilGenerator.Emit(OpCodes.Stelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    ilGenerator.Emit(OpCodes.Stelem_I8);
                    break;
                case TypeCode.Single:
                    ilGenerator.Emit(OpCodes.Stelem_R4);
                    break;
                case TypeCode.Double:
                    ilGenerator.Emit(OpCodes.Stelem_R8);
                    break;
                default:
                    if (type.GetTypeInfo().IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Stelem, type);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Stelem_Ref);
                    }
                    break;
            }
        }

        public static void EmitLoadElement(this ILGenerator ilGenerator, Type type)
        {
            if (!type.GetTypeInfo().IsValueType)
            {
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                ilGenerator.Emit(OpCodes.Ldelem, type);
            }
            else
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                        ilGenerator.Emit(OpCodes.Ldelem_I1);
                        break;
                    case TypeCode.Byte:
                        ilGenerator.Emit(OpCodes.Ldelem_U1);
                        break;
                    case TypeCode.Int16:
                        ilGenerator.Emit(OpCodes.Ldelem_I2);
                        break;
                    case TypeCode.Char:
                    case TypeCode.UInt16:
                        ilGenerator.Emit(OpCodes.Ldelem_U2);
                        break;
                    case TypeCode.Int32:
                        ilGenerator.Emit(OpCodes.Ldelem_I4);
                        break;
                    case TypeCode.UInt32:
                        ilGenerator.Emit(OpCodes.Ldelem_U4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        ilGenerator.Emit(OpCodes.Ldelem_I8);
                        break;
                    case TypeCode.Single:
                        ilGenerator.Emit(OpCodes.Ldelem_R4);
                        break;
                    case TypeCode.Double:
                        ilGenerator.Emit(OpCodes.Ldelem_R8);
                        break;
                    default:
                        ilGenerator.Emit(OpCodes.Ldelem, type);
                        break;
                }
            }
        }

        public static void EmitLdRef(this ILGenerator ilGenerator, Type type)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (type == typeof(short))
            {
                ilGenerator.Emit(OpCodes.Ldind_I1);
            }
            else if (type == typeof(Int16))
            {
                ilGenerator.Emit(OpCodes.Ldind_I2);
            }
            else if (type == typeof(Int32))
            {
                ilGenerator.Emit(OpCodes.Ldind_I4);
            }
            else if (type == typeof(Int64))
            {
                ilGenerator.Emit(OpCodes.Ldind_I8);
            }
            else if (type == typeof(float))
            {
                ilGenerator.Emit(OpCodes.Ldind_R4);
            }
            else if (type == typeof(double))
            {
                ilGenerator.Emit(OpCodes.Ldind_R8);
            }
            else if (type == typeof(ushort))
            {
                ilGenerator.Emit(OpCodes.Ldind_U1);
            }
            else if (type == typeof(UInt16))
            {
                ilGenerator.Emit(OpCodes.Ldind_U2);
            }
            else if (type == typeof(UInt32))
            {
                ilGenerator.Emit(OpCodes.Ldind_U4);
            }
            else if (type.GetTypeInfo().IsValueType)
            {
                ilGenerator.Emit(OpCodes.Ldobj);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldind_Ref);
            }
        }

        public static void EmitStRef(this ILGenerator ilGenerator, Type type)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (type == typeof(short))
            {
                ilGenerator.Emit(OpCodes.Stind_I1);
            }
            else if (type == typeof(Int16))
            {
                ilGenerator.Emit(OpCodes.Stind_I2);
            }
            else if (type == typeof(Int32))
            {
                ilGenerator.Emit(OpCodes.Stind_I4);
            }
            else if (type == typeof(Int64))
            {
                ilGenerator.Emit(OpCodes.Stind_I8);
            }
            else if (type == typeof(float))
            {
                ilGenerator.Emit(OpCodes.Stind_R4);
            }
            else if (type == typeof(double))
            {
                ilGenerator.Emit(OpCodes.Stind_R8);
            }
            else if (type.GetTypeInfo().IsValueType)
            {
                ilGenerator.Emit(OpCodes.Stobj);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Stind_Ref);
            }
        }

        #region private
        private static void EmitNullableConversion(this ILGenerator ilGenerator, TypeInfo typeFrom, TypeInfo typeTo, bool isChecked)
        {
            bool isTypeFromNullable = TypeInfoUtils.IsNullableType(typeFrom);
            bool isTypeToNullable = TypeInfoUtils.IsNullableType(typeTo);
            if (isTypeFromNullable && isTypeToNullable)
                ilGenerator.EmitNullableToNullableConversion(typeFrom, typeTo, isChecked);
            else if (isTypeFromNullable)
                ilGenerator.EmitNullableToNonNullableConversion(typeFrom, typeTo, isChecked);
            else
                ilGenerator.EmitNonNullableToNullableConversion(typeFrom, typeTo, isChecked);
        }

        private static void EmitNullableToNullableConversion(this ILGenerator ilGenerator, TypeInfo typeFrom, TypeInfo typeTo, bool isChecked)
        {
            Label labIfNull = default(Label);
            Label labEnd = default(Label);
            LocalBuilder locFrom = null;
            LocalBuilder locTo = null;
            locFrom = ilGenerator.DeclareLocal(typeFrom.AsType());
            ilGenerator.Emit(OpCodes.Stloc, locFrom);
            locTo = ilGenerator.DeclareLocal(typeTo.AsType());
            // test for null
            ilGenerator.Emit(OpCodes.Ldloca, locFrom);
            ilGenerator.EmitHasValue(typeFrom.AsType());
            labIfNull = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Brfalse_S, labIfNull);
            ilGenerator.Emit(OpCodes.Ldloca, locFrom);
            ilGenerator.EmitGetValueOrDefault(typeFrom.AsType());
            Type nnTypeFrom = TypeInfoUtils.GetNonNullableType(typeFrom);
            Type nnTypeTo = TypeInfoUtils.GetNonNullableType(typeTo);
            ilGenerator.EmitConvertToType(nnTypeFrom, nnTypeTo, isChecked);
            // construct result type
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            ilGenerator.Emit(OpCodes.Newobj, ci);
            ilGenerator.Emit(OpCodes.Stloc, locTo);
            labEnd = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Br_S, labEnd);
            // if null then create a default one
            ilGenerator.MarkLabel(labIfNull);
            ilGenerator.Emit(OpCodes.Ldloca, locTo);
            ilGenerator.Emit(OpCodes.Initobj, typeTo.AsType());
            ilGenerator.MarkLabel(labEnd);
            ilGenerator.Emit(OpCodes.Ldloc, locTo);
        }

        private static void EmitNullableToNonNullableConversion(this ILGenerator ilGenerator, TypeInfo typeFrom, TypeInfo typeTo, bool isChecked)
        {
            if (typeTo.IsValueType)
                ilGenerator.EmitNullableToNonNullableStructConversion(typeFrom, typeTo, isChecked);
            else
                ilGenerator.EmitNullableToReferenceConversion(typeFrom);
        }

        private static void EmitNullableToNonNullableStructConversion(this ILGenerator ilGenerator, TypeInfo typeFrom, TypeInfo typeTo, bool isChecked)
        {
            LocalBuilder locFrom = null;
            locFrom = ilGenerator.DeclareLocal(typeFrom.AsType());
            ilGenerator.Emit(OpCodes.Stloc, locFrom);
            ilGenerator.Emit(OpCodes.Ldloca, locFrom);
            ilGenerator.EmitGetValue(typeFrom.AsType());
            Type nnTypeFrom = TypeInfoUtils.GetNonNullableType(typeFrom);
            ilGenerator.EmitConvertToType(nnTypeFrom, typeTo.AsType(), isChecked);
        }

        private static void EmitNullableToReferenceConversion(this ILGenerator ilGenerator, TypeInfo typeFrom)
        {
            // We've got a conversion from nullable to Object, ValueType, Enum, etc.  Just box it so that
            // we get the nullable semantics.  
            ilGenerator.Emit(OpCodes.Box, typeFrom.AsType());
        }

        private static void EmitNonNullableToNullableConversion(this ILGenerator ilGenerator, TypeInfo typeFrom, TypeInfo typeTo, bool isChecked)
        {
            LocalBuilder locTo = null;
            locTo = ilGenerator.DeclareLocal(typeTo.AsType());
            Type nnTypeTo = TypeInfoUtils.GetNonNullableType(typeTo);
            ilGenerator.EmitConvertToType(typeFrom.AsType(), nnTypeTo, isChecked);
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            ilGenerator.Emit(OpCodes.Newobj, ci);
            ilGenerator.Emit(OpCodes.Stloc, locTo);
            ilGenerator.Emit(OpCodes.Ldloc, locTo);
        }

        private static void EmitNumericConversion(this ILGenerator ilGenerator, TypeInfo typeFrom, TypeInfo typeTo, bool isChecked)
        {
            bool isFromUnsigned = TypeInfoUtils.IsUnsigned(typeFrom);
            bool isFromFloatingPoint = TypeInfoUtils.IsFloatingPoint(typeFrom);
            if (typeTo.AsType() == typeof(Single))
            {
                if (isFromUnsigned)
                    ilGenerator.Emit(OpCodes.Conv_R_Un);
                ilGenerator.Emit(OpCodes.Conv_R4);
            }
            else if (typeTo.AsType() == typeof(Double))
            {
                if (isFromUnsigned)
                    ilGenerator.Emit(OpCodes.Conv_R_Un);
                ilGenerator.Emit(OpCodes.Conv_R8);
            }
            else
            {
                TypeCode tc = Type.GetTypeCode(typeTo.AsType());
                if (isChecked)
                {
                    // Overflow checking needs to know if the source value on the IL stack is unsigned or not.
                    if (isFromUnsigned)
                    {
                        switch (tc)
                        {
                            case TypeCode.SByte:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_I1_Un);
                                break;
                            case TypeCode.Int16:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_I2_Un);
                                break;
                            case TypeCode.Int32:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_I4_Un);
                                break;
                            case TypeCode.Int64:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_I8_Un);
                                break;
                            case TypeCode.Byte:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_U1_Un);
                                break;
                            case TypeCode.UInt16:
                            case TypeCode.Char:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_U2_Un);
                                break;
                            case TypeCode.UInt32:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_U4_Un);
                                break;
                            case TypeCode.UInt64:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_U8_Un);
                                break;
                            default:
                                throw new InvalidCastException();
                        }
                    }
                    else
                    {
                        switch (tc)
                        {
                            case TypeCode.SByte:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_I1);
                                break;
                            case TypeCode.Int16:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_I2);
                                break;
                            case TypeCode.Int32:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_I4);
                                break;
                            case TypeCode.Int64:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_I8);
                                break;
                            case TypeCode.Byte:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_U1);
                                break;
                            case TypeCode.UInt16:
                            case TypeCode.Char:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_U2);
                                break;
                            case TypeCode.UInt32:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_U4);
                                break;
                            case TypeCode.UInt64:
                                ilGenerator.Emit(OpCodes.Conv_Ovf_U8);
                                break;
                            default:
                                throw new InvalidCastException();
                        }
                    }
                }
                else
                {
                    switch (tc)
                    {
                        case TypeCode.SByte:
                            ilGenerator.Emit(OpCodes.Conv_I1);
                            break;
                        case TypeCode.Byte:
                            ilGenerator.Emit(OpCodes.Conv_U1);
                            break;
                        case TypeCode.Int16:
                            ilGenerator.Emit(OpCodes.Conv_I2);
                            break;
                        case TypeCode.UInt16:
                        case TypeCode.Char:
                            ilGenerator.Emit(OpCodes.Conv_U2);
                            break;
                        case TypeCode.Int32:
                            ilGenerator.Emit(OpCodes.Conv_I4);
                            break;
                        case TypeCode.UInt32:
                            ilGenerator.Emit(OpCodes.Conv_U4);
                            break;
                        case TypeCode.Int64:
                            if (isFromUnsigned)
                            {
                                ilGenerator.Emit(OpCodes.Conv_U8);
                            }
                            else
                            {
                                ilGenerator.Emit(OpCodes.Conv_I8);
                            }
                            break;
                        case TypeCode.UInt64:
                            if (isFromUnsigned || isFromFloatingPoint)
                            {
                                ilGenerator.Emit(OpCodes.Conv_U8);
                            }
                            else
                            {
                                ilGenerator.Emit(OpCodes.Conv_I8);
                            }
                            break;
                        default:
                            throw new InvalidCastException();
                    }
                }
            }
        }

        private static bool ShouldLdtoken(Type t)
        {
            return t.IsGenericParameter || t.GetTypeInfo().IsVisible;
        }

        private static bool TryEmitILConstant(this ILGenerator ilGenerator, object value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    ilGenerator.EmitBoolean((bool)value);
                    return true;
                case TypeCode.SByte:
                    ilGenerator.EmitSByte((sbyte)value);
                    return true;
                case TypeCode.Int16:
                    ilGenerator.EmitShort((short)value);
                    return true;
                case TypeCode.Int32:
                    ilGenerator.EmitInt((int)value);
                    return true;
                case TypeCode.Int64:
                    ilGenerator.EmitLong((long)value);
                    return true;
                case TypeCode.Single:
                    ilGenerator.EmitSingle((float)value);
                    return true;
                case TypeCode.Double:
                    ilGenerator.EmitDouble((double)value);
                    return true;
                case TypeCode.Char:
                    ilGenerator.EmitChar((char)value);
                    return true;
                case TypeCode.Byte:
                    ilGenerator.EmitByte((byte)value);
                    return true;
                case TypeCode.UInt16:
                    ilGenerator.EmitUShort((ushort)value);
                    return true;
                case TypeCode.UInt32:
                    ilGenerator.EmitUInt((uint)value);
                    return true;
                case TypeCode.UInt64:
                    ilGenerator.EmitULong((ulong)value);
                    return true;
                case TypeCode.Decimal:
                    ilGenerator.EmitDecimal((decimal)value);
                    return true;
                case TypeCode.String:
                    ilGenerator.EmitString((string)value);
                    return true;
                default:
                    return false;
            }
        }

        private static void EmitDecimalBits(this ILGenerator ilGenerator, decimal value)
        {
            int[] bits = Decimal.GetBits(value);
            ilGenerator.EmitInt(bits[0]);
            ilGenerator.EmitInt(bits[1]);
            ilGenerator.EmitInt(bits[2]);
            ilGenerator.EmitBoolean((bits[3] & 0x80000000) != 0);
            ilGenerator.EmitByte((byte)(bits[3] >> 16));
            ilGenerator.EmitNew(typeof(decimal).GetTypeInfo().GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) }));
        }

        private static bool CanEmitILConstant(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.String:
                    return true;
            }
            return false;
        }
        #endregion
    }
    internal static class TypeInfoUtils
    {
        internal static bool AreEquivalent(TypeInfo t1, TypeInfo t2)
        {
            return t1 == t2 || t1.IsEquivalentTo(t2.AsType());
        }

        internal static bool IsNullableType(this TypeInfo type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        internal static Type GetNonNullableType(this TypeInfo type)
        {
            if (IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type.AsType();
        }

        internal static bool IsLegalExplicitVariantDelegateConversion(TypeInfo source, TypeInfo dest)
        {
            if (!IsDelegate(source) || !IsDelegate(dest) || !source.IsGenericType || !dest.IsGenericType)
                return false;

            var genericDelegate = source.GetGenericTypeDefinition();

            if (dest.GetGenericTypeDefinition() != genericDelegate)
                return false;

            var genericParameters = genericDelegate.GetTypeInfo().GetGenericArguments();
            var sourceArguments = source.GetGenericArguments();
            var destArguments = dest.GetGenericArguments();

            for (int iParam = 0; iParam < genericParameters.Length; ++iParam)
            {
                var sourceArgument = sourceArguments[iParam].GetTypeInfo();
                var destArgument = destArguments[iParam].GetTypeInfo();

                if (AreEquivalent(sourceArgument, destArgument))
                {
                    continue;
                }

                var genericParameter = genericParameters[iParam].GetTypeInfo();

                if (IsInvariant(genericParameter))
                {
                    return false;
                }

                if (IsCovariant(genericParameter))
                {
                    if (!HasReferenceConversion(sourceArgument, destArgument))
                    {
                        return false;
                    }
                }
                else if (IsContravariant(genericParameter))
                {
                    if (sourceArgument.IsValueType || destArgument.IsValueType)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool IsDelegate(TypeInfo t)
        {
            return t.IsSubclassOf(typeof(System.MulticastDelegate));
        }

        private static bool IsInvariant(TypeInfo t)
        {
            return 0 == (t.GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        }

        private static bool IsCovariant(this TypeInfo t)
        {
            return 0 != (t.GenericParameterAttributes & GenericParameterAttributes.Covariant);
        }

        internal static bool HasReferenceConversion(TypeInfo source, TypeInfo dest)
        {
            // void -> void conversion is handled elsewhere
            // (it's an identity conversion)
            // All other void conversions are disallowed.
            if (source.AsType() == typeof(void) || dest.AsType() == typeof(void))
            {
                return false;
            }

            var nnSourceType = TypeInfoUtils.GetNonNullableType(source).GetTypeInfo();
            var nnDestType = TypeInfoUtils.GetNonNullableType(dest).GetTypeInfo();

            // Down conversion
            if (nnSourceType.IsAssignableFrom(nnDestType))
            {
                return true;
            }
            // Up conversion
            if (nnDestType.IsAssignableFrom(nnSourceType))
            {
                return true;
            }
            // Interface conversion
            if (source.IsInterface || dest.IsInterface)
            {
                return true;
            }
            // Variant delegate conversion
            if (IsLegalExplicitVariantDelegateConversion(source, dest))
                return true;

            // Object conversion
            if (source.AsType() == typeof(object) || dest.AsType() == typeof(object))
            {
                return true;
            }
            return false;
        }

        private static bool IsContravariant(TypeInfo t)
        {
            return 0 != (t.GenericParameterAttributes & GenericParameterAttributes.Contravariant);
        }

        internal static bool IsConvertible(this TypeInfo typeInfo)
        {
            var type = GetNonNullableType(typeInfo);
            if (typeInfo.IsEnum)
            {
                return true;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsUnsigned(TypeInfo typeInfo)
        {
            var type = GetNonNullableType(typeInfo);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.Char:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsFloatingPoint(TypeInfo typeInfo)
        {
            var type = GetNonNullableType(typeInfo);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }
    }
    internal static class MethodInfoConstant
    {
        internal static readonly MethodInfo GetTypeFromHandle = MethodExtensions.GetMethod<Func<RuntimeTypeHandle, Type>>(handle => Type.GetTypeFromHandle(handle));

        internal static readonly MethodInfo GetMethodFromHandle = MethodExtensions.GetMethod<Func<RuntimeMethodHandle, RuntimeTypeHandle, MethodBase>>((h1, h2) => MethodBase.GetMethodFromHandle(h1, h2));

        internal static readonly ConstructorInfo ArgumentNullExceptionCtor = typeof(ArgumentNullException).GetTypeInfo().GetConstructor(new Type[] { typeof(string) });

        internal static readonly ConstructorInfo ObjectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.Single();
    }
}
