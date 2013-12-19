#region Using declarations

using System;
using Bridgepoint.Enterprise.Common.IocContainer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace IocContainer.Tests {
    [TestClass]
    public class ContainerChaining {
        [TestMethod]
        [TestCategory("Unit")]
        public void ConstructChainedResolverClassWithResolver() {
            // Arrange
            var resolver = new InterfaceResolver();

            // Act
            var x = new ChainedResolverClass(resolver);

            // Assert
            Assert.IsNotNull(x);
        }


        [TestMethod]
        [TestCategory("Unit")]
        public void ConstructChainedResolverClassWithoutResolver() {
            // Arrange

            // Act
            var chainedResolverClass = new ChainedResolverClass();

            // Assert
            Assert.IsNotNull(chainedResolverClass);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ChainedResolverClassWithDefaultConstructorResolves() {
            // Arrange
            IChainedResolverClass chainedResolverClass = new ChainedResolverClass();

            // Act
            IChainedResolverClass newResolver = chainedResolverClass.GetNewChainedResolverClass();

            // Assert
            Assert.IsNotNull(newResolver);
            Assert.AreNotSame(chainedResolverClass, newResolver);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [ExpectedException(typeof(RegistrationMissingException))]
        public void ChainedResolverClassWithResolverWithUnregisteredDependencyFails() {
            // Arrange
            var resolver = new InterfaceResolver();
            IChainedResolverClass chainedResolverClass = new ChainedResolverClass(resolver);

            // Act
            IChainedResolverClass newResolver = chainedResolverClass.GetNewChainedResolverClass();

            // Assert
            Assert.Fail("Exception expected");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ChainedResolverClassWithResolverConstructorResolves() {
            // Arrange
            var resolver = new InterfaceResolver();
            resolver.Register<IChainedResolverClass, ChainedResolverClass>();
            IChainedResolverClass chainedResolverClass = new ChainedResolverClass(resolver);

            // Act
            IChainedResolverClass newResolver = chainedResolverClass.GetNewChainedResolverClass();

            // Assert
            Assert.IsNotNull(newResolver);
            Assert.AreNotSame(chainedResolverClass, newResolver);
        }


        [TestMethod]
        [TestCategory("Unit")]
        public void ChainedResolverClassWithDefaultConstructorPropegatesResolverDownward() {
            // Arrange
            IChainedResolverClass chainedResolverClass = new ChainedResolverClass();

            // Act
            IChainedResolverClass newResolver =
                chainedResolverClass.GetNewChainedResolverClass().GetNewChainedResolverClass();

            // Assert
            Assert.AreSame(chainedResolverClass.GetResolver(), newResolver.GetResolver());
        }


        [TestMethod]
        [TestCategory("Unit")]
        public void ChainedResolverClassWithResolverConstructorPropegatesResolverDownward() {
            // Arrange
            var resolver = new InterfaceResolver();
            resolver.Register<IChainedResolverClass, ChainedResolverClass>();
            IChainedResolverClass chainedResolverClass = new ChainedResolverClass(resolver);

            // Act
            /*IChainedResolverClass newResolver =
                chainedResolverClass.GetNewChainedResolverClass().GetNewChainedResolverClass();*/
            IChainedResolverClass newResolver =
                chainedResolverClass.GetNewChainedResolverClass();
            newResolver = newResolver.GetNewChainedResolverClass();

            // Assert
            Assert.AreSame(resolver, newResolver.GetResolver());
            Assert.AreSame(chainedResolverClass.GetResolver(), newResolver.GetResolver());
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void UsesDefaultConstructorWhenNotFirst() {
            // Arrange
            var resolver = new InterfaceResolver();
            resolver.Register<IConstructorTestClass, ConstructorTestClassWithManyConstructors>();

            // Act
            var instance = resolver.Resolve<IConstructorTestClass>();

            // Assert
            Assert.AreEqual("default", instance.Value);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void UsesInterfaceResolverConstructorOverDefault()
        {
            // Arrange
            var resolver = new InterfaceResolver();
            resolver.Register<IConstructorTestClass, ConstructorTestClassWithDefaultAndResolver>();

            // Act
            var instance = resolver.Resolve<IConstructorTestClass>();

            // Assert
            Assert.AreEqual("InterfaceResolver", instance.Value);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NonFirstConstructorWithMultipleArgumentsResolves()
        {
            // Arrange
            var resolver = new InterfaceResolver();
            resolver.Register<IConstructorTestClass, ConstructorTestClassWithManyConstructors>()
                    .WithConstructor(new Type[] {typeof(string), typeof(int)}, new object[] {"something", 3});
            
            // Act
            var instance = resolver.Resolve<IConstructorTestClass>();

            // Assert
            Assert.AreEqual("string,int", instance.Value);
        }

        /************************************************* Nested classes ***********************************************/

        #region Nested type: ChainedResolverClass

        public class ChainedResolverClass : IChainedResolverClass {
            protected readonly InterfaceResolver Resolver;


            public ChainedResolverClass(InterfaceResolver resolver) {
                Resolver = resolver;
            }

            public ChainedResolverClass() {
                Resolver = new InterfaceResolver();
                Resolver.Register<IChainedResolverClass, ChainedResolverClass>();
            }

            #region Implementation of IChainedResolverClass

            public IChainedResolverClass GetNewChainedResolverClass() {
                return Resolver.Resolve<IChainedResolverClass>();
            }

            public InterfaceResolver GetResolver() {
                return Resolver;
            }

            #endregion
        }

        #endregion

        #region Nested type: ConstructorTestClassWithManyConstructors

        public class ConstructorTestClassWithManyConstructors : IConstructorTestClass {
            public ConstructorTestClassWithManyConstructors(int int1) {
                Value = "int(first)";
            }

            public ConstructorTestClassWithManyConstructors(int int1, int int2)
            {
                Value = "int,int";
            }

            public ConstructorTestClassWithManyConstructors() {
                Value = "default";
            }

            public ConstructorTestClassWithManyConstructors(string string1, int int1)
            {
                Value = "string,int";
            }

            public ConstructorTestClassWithManyConstructors(int int1, string string1)
            {
                Value = "int,string";
            }

            #region Implementation of IConstructorTestClass

            #endregion

            #region IConstructorTestClass Members

            public string Value { get; set; }

            #endregion
        }

        #endregion

        public class ConstructorTestClassWithNoDefault : IConstructorTestClass
        {
            public string Value { get; set; }
            public ConstructorTestClassWithNoDefault(string string1, int int1)
            {
                Value = "string,int(first)";
            }

            public ConstructorTestClassWithNoDefault(int int1, string string1)
            {
                Value = "int,string";
            }
        }

        public class ConstructorTestClassOnlyDefault : IConstructorTestClass
        {
            public string Value { get; set; }
            public ConstructorTestClassOnlyDefault()
            {
                Value = "default(first)";
            }
        }

        public class ConstructorTestClassWithDefaultAndResolver : IConstructorTestClass
        {
            public string Value { get; set; }
            public ConstructorTestClassWithDefaultAndResolver()
            {
                Value = "default(first)";
            }

            public ConstructorTestClassWithDefaultAndResolver(InterfaceResolver resolver)
            {
                Value = "InterfaceResolver";
            }
        }


        #region Nested type: IChainedResolverClass

        public interface IChainedResolverClass {
            IChainedResolverClass GetNewChainedResolverClass();
            InterfaceResolver GetResolver();
        }

        #endregion

        #region Nested type: IConstructorTestClass

        public interface IConstructorTestClass {
            string Value { get; set; }
        }

        #endregion
        
    }
}