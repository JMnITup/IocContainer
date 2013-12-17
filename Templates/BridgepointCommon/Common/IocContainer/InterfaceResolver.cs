#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace Bridgepoint.Enterprise.Common.IocContainer {
    /// <summary>
    /// Instanced interface resolver used to register and resolve requests for interface implementations - used by Assembly container
    /// </summary>
    public class InterfaceResolver {
        protected readonly ConcurrentDictionary<Type, string> NameDictionary = new ConcurrentDictionary<Type, string>();
        protected readonly ConcurrentDictionary<string, Func<object>> ProviderDictionary = new ConcurrentDictionary<string, Func<object>>();

        /// <summary>
        ///     Clears all registrations
        /// </summary>
        public void ClearRegistrations() {
            this.NameDictionary.Clear();
            this.ProviderDictionary.Clear();
        }

        /// <summary>
        ///     Registers an interface to a concrete class resolution
        /// </summary>
        /// <typeparam name="TS">Interface to be registered</typeparam>
        /// <typeparam name="TC">Concrete class to resolve to</typeparam>
        /// <returns>Registration object allowing for fluent interface modification of registration</returns>
        public Registration Register<TS, TC>() where TC : TS {
            return this.Register<TS, TC>(typeof(TS).FullName);
        }

        /// <summary>
        ///     Registers an interface to a concrete class resolution using a specific name, allows for registering the same interface to different concrete classes based on context
        /// </summary>
        /// <param name="name">Name to use</param>
        /// <typeparam name="TS">Interface to be registered</typeparam>
        /// <typeparam name="TC">Concrete class to resolve to</typeparam>
        /// <returns>Registration object allowing for fluent interface modification of registration</returns>
        public Registration Register<TS, TC>(string name) where TC : TS {
            if (!this.NameDictionary.ContainsKey(typeof(TS))) {
                this.NameDictionary[typeof(TS)] = name;
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
            return (T) this.ProviderDictionary[name]();
        }

        /// <summary>
        ///     Resolves an interface request to the registered class
        /// </summary>
        /// <typeparam name="T">Interface to resolve</typeparam>
        /// <returns>Instance of object registered to interface</returns>
        public T Resolve<T>() where T : class {
            return this.Resolve<T>(this.NameDictionary[typeof(T)]);
        }

        #region Nested type: Registration

        /// <summary>
        ///     Return type allowing for fluent interface usage of resolve syntax - e.g. Container.Register<ISomeInterface>.AsInstance(MockInstance);
        /// </summary>
        public class Registration {
            private readonly Dictionary<string, Func<object>> _args;
            private readonly InterfaceResolver _interfaceResolver;
            private readonly string _name;

            internal Registration(InterfaceResolver interfaceResolver, string name, Type type) {
                this._interfaceResolver = interfaceResolver;
                this._name = name;

                ConstructorInfo c = type.GetConstructors().First();
                this._args = c.GetParameters()
                              .ToDictionary<ParameterInfo, string, Func<object>>(
                                  x => x.Name,
                                  x => (() => interfaceResolver.ProviderDictionary[interfaceResolver.NameDictionary[x.ParameterType]]())
                    );
                interfaceResolver.ProviderDictionary[name] = () => c.Invoke(this._args.Values.Select(x => x()).ToArray());
            }


            /// <summary>
            ///     Registeres a specific object instance to be returned when interface is resolved
            /// </summary>
            /// <param name="instance">Object instance to register</param>
            /// <returns>Registration</returns>
            public Registration AsInstance(object instance) {
                this._interfaceResolver.ProviderDictionary[this._name] = () => instance;
                return this;
            }

            /// <summary>
            ///     Registers the class as a singleton - first request will instantiate the class, all further resolves will return the same instance
            /// </summary>
            /// <returns>Registration</returns>
            public Registration AsSingleton() {
                object value = null;
                Func<object> service = this._interfaceResolver.ProviderDictionary[this._name];
                this._interfaceResolver.ProviderDictionary[this._name] = () => value ?? (value = service());
                return this;
            }

            /// <summary>
            ///     Defines internal dependency to use for resolution within object resolution
            /// </summary>
            /// <param name="parameter"></param>
            /// <param name="component"></param>
            /// <returns></returns>
            public Registration WithDependency(string parameter, string component) {
                this._args[parameter] = () => this._interfaceResolver.ProviderDictionary[component]();
                return this;
            }

            /// <summary>
            ///     Registers a class with parameters matching the First constructor of the concrete class - resolved instances will pass these parameters into constructor on resolution
            /// </summary>
            /// <param name="parameter"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public Registration WithConstructor(string parameter, object value) {
                this._args[parameter] = () => value;
                return this;
            }
        }

        #endregion
    }
}