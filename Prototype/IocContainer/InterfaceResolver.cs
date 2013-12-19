#region Using declarations

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using IocContainer;

#endregion

namespace Bridgepoint.Enterprise.Common.IocContainer {
    /// <summary>
    ///     Instanced interface resolver used to register and resolve requests for interface implementations - used by Assembly container
    /// </summary>
    public class InterfaceResolver {
        protected readonly ConcurrentDictionary<Type, string> NameDictionary = new ConcurrentDictionary<Type, string>();

        protected readonly ConcurrentDictionary<string, Func<object>> ProviderDictionary =
            new ConcurrentDictionary<string, Func<object>>();

        /// <summary>
        ///     Clears all registrations
        /// </summary>
        public void ClearRegistrations() {
            NameDictionary.Clear();
            ProviderDictionary.Clear();
        }

        /// <summary>
        ///     Registers an interface to a concrete class resolution
        /// </summary>
        /// <typeparam name="TS">Interface to be registered</typeparam>
        /// <typeparam name="TC">Concrete class to resolve to</typeparam>
        /// <returns>Registration object allowing for fluent interface modification of registration</returns>
        public Registration Register<TS, TC>() where TC : TS {
            return Register<TS, TC>(typeof(TS).FullName);
        }

        /// <summary>
        ///     Registers an interface to a concrete class resolution using a specific name, allows for registering the same interface to different concrete classes based on context
        /// </summary>
        /// <param name="name">Name to use</param>
        /// <typeparam name="TS">Interface to be registered</typeparam>
        /// <typeparam name="TC">Concrete class to resolve to</typeparam>
        /// <returns>Registration object allowing for fluent interface modification of registration</returns>
        public Registration Register<TS, TC>(string name) where TC : TS {
            if (!NameDictionary.ContainsKey(typeof(TS))) {
                NameDictionary[typeof(TS)] = name;
            }
            return new Registration(this, name, typeof(TC));
        }


        /// <summary>
        ///     Resolves an interface request to the registered class, based on named registration
        /// </summary>
        /// <typeparam name="T">Interface to resolve</typeparam>
        /// <param name="name">Named registration to use</param>
        /// <returns>Instance of object registered to interface and name</returns>
        public T Resolve<T>(string name) where T : class {
            return (T) ProviderDictionary[name]();
        }

        /// <summary>
        ///     Resolves an interface request to the registered class
        /// </summary>
        /// <typeparam name="T">Interface to resolve</typeparam>
        /// <exception cref="RegistrationMissingException"></exception>
        /// <returns>Instance of object registered to interface</returns>
        public T Resolve<T>() where T : class {
            // TODO: performance test catching this versus throwing as a KeyNotFoundException.  If difference is significant enough, it might be worth just throwing without handling
            try {
                return Resolve<T>(NameDictionary[typeof(T)]);
            } catch (KeyNotFoundException ex) {
                throw new RegistrationMissingException(
                    "Interface " + typeof(T).FullName + " not registered, cannot resolve", ex);
            }
        }

        #region Nested type: Registration

        /// <summary>
        ///     Return type allowing for fluent interface usage of resolve syntax - e.g. Container.Register<ISomeInterface>.AsInstance(MockInstance);
        /// </summary>
        public class Registration {
            private readonly Dictionary<string, Func<object>> _args;
            private readonly InterfaceResolver _interfaceResolver;
            private readonly string _name;
            private readonly Type _type;

            internal Registration(InterfaceResolver interfaceResolver, string name, Type type) {
                _interfaceResolver = interfaceResolver;
                _name = name;
                _type = type;

                // TODO: Old line - ConstructorInfo c = type.GetConstructors().First();

                ConstructorInfo c;
                c = type.GetConstructor(new[] {typeof(InterfaceResolver)});
                if (c == null) {
                    c = type.GetConstructor(new Type[] {});
                }
                if (c == null) {
                    c = type.GetConstructors().First();
                }
                Debug.Assert(c != null, "Cannot resolve object with no constructors");

                _args = c.GetParameters()
                         .ToDictionary<ParameterInfo, string, Func<object>>(
                             x => x.Name,
                             x =>
                             (() => {
                                  Type pType = x.ParameterType;
                                  if (pType == typeof(InterfaceResolver)) {
                                      return interfaceResolver;
                                  }
                                  string nameMap = interfaceResolver.NameDictionary[pType];
                                  object provider = interfaceResolver.ProviderDictionary[nameMap]();
                                  return provider;
                              }
                             )
                    );
                interfaceResolver.ProviderDictionary[name] = () => c.Invoke(_args.Values.Select(x => x()).ToArray());
            }


            /// <summary>
            ///     Registeres a specific object instance to be returned when interface is resolved
            /// </summary>
            /// <param name="instance">Object instance to register</param>
            /// <returns>Registration</returns>
            public Registration AsInstance(object instance) {
                _interfaceResolver.ProviderDictionary[_name] = () => instance;
                return this;
            }

            /// <summary>
            ///     Registers the class as a singleton - first request will instantiate the class, all further resolves will return the same instance
            /// </summary>
            /// <returns>Registration</returns>
            public Registration AsSingleton() {
                object value = null;
                Func<object> service = _interfaceResolver.ProviderDictionary[_name];
                _interfaceResolver.ProviderDictionary[_name] = () => value ?? (value = service());
                return this;
            }

            /// <summary>
            ///     Defines internal dependency to use for resolution within object resolution
            /// </summary>
            /// <param name="parameter"></param>
            /// <param name="component"></param>
            /// <returns></returns>
            public Registration WithDependency(string parameter, string component) {
                _args[parameter] = () => _interfaceResolver.ProviderDictionary[component]();
                return this;
            }

            /// <summary>
            ///     Registers a class with parameters matching the First constructor of the concrete class - resolved instances will pass these parameters into constructor on resolution
            /// </summary>
            /// <param name="parameter"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public Registration WithConstructor(string parameter, object value) {
                _args[parameter] = () => value;
                return this;
            }

            public Registration WithConstructor(Type[] types, object[] values) {
                Type t = _type;
                ConstructorInfo c = t.GetConstructor(types);
                if (c == null) {
                    throw new RegistrationMissingException("Attempt to initialize " + _type + ":" + _name + " with non-existant public constructor: " + types.ToString(), null);
                }
                _interfaceResolver.ProviderDictionary[_name] = () => c.Invoke(values);
                return this;
            }
        }

        #endregion
    }
}