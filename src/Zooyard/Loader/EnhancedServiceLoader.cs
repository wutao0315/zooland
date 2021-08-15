using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Zooyard.Logging;

namespace Zooyard.Loader
{
    /// <summary>
    /// The type Enhanced service loader.
    /// 
    /// AssemblyLoadContext
    /// </summary>
    public class EnhancedServiceLoader
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(EnhancedServiceLoader));
        private const string SERVICES_DIRECTORY = "services";
        private const string ZOOTA_DIRECTORY = "zoota";
        private static ConcurrentDictionary<Type, IList<Type>> providers = new ();

        /// <summary>
        /// Specify classLoader to load the service provider
        /// </summary>
        /// @param <S>     the type parameter </param>
        /// <param name="service"> the service </param>
        /// <param name="loader">  the loader </param>
        /// <returns> s s </returns>
        /// <exception cref="EnhancedServiceNotFoundException"> the enhanced service not found exception </exception>
        public static S Load<S>()
        {
            return loadFile<S>(null);
        }


        /// <summary>
        /// load service provider
        /// </summary>
        /// @param <S>          the type parameter </param>
        /// <param name="service">      the service </param>
        /// <param name="activateName"> the activate name </param>
        /// <returns> s s </returns>
        /// <exception cref="EnhancedServiceNotFoundException"> the enhanced service not found exception </exception>
        public static S Load<S>(string activateName)
        {
            return loadFile<S>(activateName);
        }

        /// <summary>
        /// Load s.
        /// </summary>
        /// @param <S>          the type parameter </param>
        /// <param name="activateName"> the activate name </param>
        /// <param name="args">         the args </param>
        /// <returns> the s </returns>
        /// <exception cref="EnhancedServiceNotFoundException"> the enhanced service not found exception </exception>
        public static S Load<S>(string activateName, object[] args)
        {
            Type[] argsType = null;
            if (args != null && args.Length > 0)
            {
                argsType = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    argsType[i] = args[i].GetType();
                }
            }
            return loadFile<S>(activateName, argsType, args);
        }

        /// <summary>
        /// Load s.
        /// </summary>
        /// @param <S>          the type parameter </param>
        /// <param name="activateName"> the activate name </param>
        /// <param name="argsType">     the args type </param>
        /// <param name="args">         the args </param>
        /// <returns> the s </returns>
        /// <exception cref="EnhancedServiceNotFoundException"> the enhanced service not found exception </exception>
        public static S Load<S>(string activateName, Type[] argsType, object[] args)
        {
            return loadFile<S>(activateName, argsType, args);
        }

        /// <summary>
        /// get all implements
        /// </summary>
        /// @param <S>     the type parameter </param>
        /// <returns> list list </returns>
        public static IList<S> LoadAll<S>(Type[] argsType = null, object[] args = null)
        {
            IList<S> allInstances = new List<S>();
            IList<Type> allClazzs = getAllExtensionClass<S>();
            if ((allClazzs?.Count??0)<=0)
            {
                return allInstances;
            }
            try
            {
                foreach (Type clazz in allClazzs)
                {
                    allInstances.Add(initInstance<S>(clazz, argsType, args));
                }
            }
            catch (Exception t)
            {
                throw new EnhancedServiceNotFoundException(t);
            }
            return allInstances;
        }

        /// <summary>
        /// Get all the extension classes, follow <seealso cref="LoadLevel"/> defined and sort order
        /// </summary>
        /// @param <S>     the type parameter </param>
        /// <returns> all extension class </returns>
        public static IList<Type> getAllExtensionClass<S>()
        {
            return findAllExtensionClass<S>(null);
        }


        private static S loadFile<S>(string activateName)
        {
            try
            {
                return loadFile<S>(activateName, null, null);
            }
            catch (EnhancedServiceNotFoundException ex)
            {
                Logger().LogError(ex, ex.Message);
                return default;
            }
            catch (Exception) 
            {
                throw;
            }
        }

        private static S loadFile<S>(string activateName, Type[] argTypes, object[] args)
        {
            Type service = typeof(S);
            try
            {
                bool foundFromCache = true;
                
                if (!providers.TryGetValue(service, out IList<Type> extensions))
                {
                    lock (service)
                    {
                        extensions = findAllExtensionClass<S>(activateName);
                        foundFromCache = false;
                        providers.TryAdd(service, extensions);
                    }
                }
                if (!string.IsNullOrEmpty(activateName))
                {
                    loadFile(service, Path.Combine(ZOOTA_DIRECTORY, activateName), extensions);

                    var activateExtensions = new List<Type>();
                    foreach (Type clz in extensions)
                    {
                        LoadLevel activate = (LoadLevel)clz.GetCustomAttribute(typeof(LoadLevel));
                        if (activate != null && activateName.Equals(activate.name, StringComparison.OrdinalIgnoreCase))
                        {
                            activateExtensions.Add(clz);
                        }
                    }

                    extensions = activateExtensions;
                }

                if (extensions.Count == 0)
                {
                    throw new EnhancedServiceNotFoundException("not found service provider for : " + service.FullName + "[" + activateName + "] ");
                }
                Type extension = extensions[extensions.Count - 1];
                S result = initInstance<S>(extension, argTypes, args);
                if (!foundFromCache)
                {
                    Logger().LogInformation("load " + service.Name + "[" + activateName + "] extension by class[" + extension.FullName + "]");
                }
                return result;
            }
            catch (Exception e)
            {
                if (e is EnhancedServiceNotFoundException)
                {
                    throw (EnhancedServiceNotFoundException)e;
                }
                else
                {
                    throw new EnhancedServiceNotFoundException("not found service provider for : " + service.FullName + " caused by " + e.StackTrace);
                }
            }
        }

        private static IList<Type> findAllExtensionClass<S>(string activateName)
        {
            Type service = typeof(S);
            var extensions = new List<Type>();
            try
            {
                loadFile(service, SERVICES_DIRECTORY, extensions);
                loadFile(service, ZOOTA_DIRECTORY, extensions);
            }
            catch (IOException e)
            {
                throw new EnhancedServiceNotFoundException(e);
            }

            if (extensions.Count == 0)
            {
                return extensions;
            }
            extensions.Sort(new ComparatorAnonymousInnerClass());

            return extensions;
        }

        private class ComparatorAnonymousInnerClass : IComparer<Type>
        {
            public virtual int Compare(Type c1, Type c2)
            {
                int o1 = 0;
                int o2 = 0;
                
                LoadLevel a1 = (LoadLevel)c1.GetCustomAttribute(typeof(LoadLevel));
                LoadLevel a2 = (LoadLevel)c2.GetCustomAttribute(typeof(LoadLevel));

                if (a1 != null)
                {
                    o1 = a1.order;
                }

                if (a2 != null)
                {
                    o2 = a2.order;
                }

                return o1.CompareTo(o2);
            }
        }

        private static void loadFile(Type service, string dir, IList<Type> extensions)
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", dir, service.FullName);
            try
            {
                if (File.Exists(fileName)) 
                {
                    using var s = File.OpenRead(fileName);
                    using var reader = new StreamReader(s, Constants.DEFAULT_CHARSET);
                    string line = null;
                    while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                    {
                        int ci = line.IndexOf('#');
                        if (ci >= 0)
                        {
                            line = line.Substring(0, ci);
                        }
                        line = line.Trim();
                        if (line.Length > 0)
                        {
                            try
                            {
                                extensions.Add(Type.GetType(line, true, true));
                            }
                            catch (Exception e)
                            {
                                Logger().LogWarning(e, $"load [{line}] class fail. {e.Message}");

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// init instance
        /// </summary>
        /// @param <S>       the type parameter </param>
        /// <param name="implClazz"> the impl clazz </param>
        /// <param name="argTypes">  the arg types </param>
        /// <param name="args">      the args </param>
        /// <returns> s s </returns>
        protected internal static S initInstance<S>(Type implClazz, Type[] argTypes, object[] args)
        {
            S s = default;
            if (argTypes != null && args != null)
            {
                // Constructor with arguments
                var constructor = implClazz.GetConstructor(argTypes);
                s = (S)constructor.Invoke(args);
            }
            else
            {
                // default Constructor
                s = (S)implClazz.GetConstructor(new Type[] { }).Invoke(new object[] { });
            }
            if (s is Initialize init)
            {
                init.Init();
            }
            return s;
        }
    }
}
