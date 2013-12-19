#region Using declarations

using System;
using Bridgepoint.Enterprise.Common.IocContainer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace IocContainer.Tests {
    [TestClass]
    public class IocContainerUnitTests {
        [TestMethod]
        [TestCategory("Unit")]
        public void VerifyAnonymousRegistration() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeOne>();

            // Act
            var m = c.Resolve<IRootType>();

            // Assert
            Assert.AreEqual(0, m.GetFinalValue());
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void VerifyNamedRegistration() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeOne>("named");

            // Act
            var m = c.Resolve<IRootType>("named");

            // Assert
            Assert.AreEqual(0, m.GetFinalValue());
        }


        [TestMethod]
        [TestCategory("Unit")]
        public void VerifyAnonymousSubDependency() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeOne>();
            c.Register<IDisplay, NodeDisplay>();

            // Act
            var m = c.Resolve<IDisplay>();

            // Assert
            Assert.AreEqual("$0.00", m.Format("C2"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void WithConstructorBuildsWithNamedParameter() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeTwo>("named").WithConstructor("internalValue", 5);

            // Act
            int i = c.Resolve<IRootType>("named").GetFinalValue();

            // Assert
            Assert.AreEqual(5, i);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NamedSubDependencyFunctions() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeTwo>("seven").WithConstructor("internalValue", 7);
            c.Register<IRootType, ConcreteTypeTwo>("nine").WithConstructor("internalValue", 9);
            c.Register<IRootType, Combine>("add").WithDependency("m1", "seven").WithDependency("m2", "nine");

            // Act
            int i = c.Resolve<IRootType>("add").GetFinalValue();

            // Assert
            Assert.AreEqual(16, i);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NamedSubDependencyOutOfOrderWorks() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, Combine>("add").WithDependency("m1", "five").WithDependency("m2", "six");
            c.Register<IRootType, ConcreteTypeTwo>("five").WithConstructor("internalValue", 5);
            c.Register<IRootType, ConcreteTypeTwo>("six").WithConstructor("internalValue", 6);

            // Act
            int i = c.Resolve<IRootType>("add").GetFinalValue();

            // Assert
            Assert.AreEqual(11, i);
        }


        [TestMethod]
        [TestCategory("Unit")]
        public void AnonymousNonSingletonDoNotResolveToSameObject() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeOne>().AsSingleton();

            // Act
            var resolve1 = c.Resolve<IRootType>();
            var resolve2 = c.Resolve<IRootType>();

            // Assert
            Assert.AreSame(resolve1, resolve2);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnonymousSingletonResolvesToSameObject() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeOne>().AsSingleton();

            // Act
            var resolve1 = c.Resolve<IRootType>();
            var resolve2 = c.Resolve<IRootType>();

            // Assert
            Assert.AreSame(resolve1, resolve2);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NamedSingletonResolvesToSameObject() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeOne>("namedRegistration").AsSingleton();

            // Act
            var resolve1 = c.Resolve<IRootType>("namedRegistration");
            var resolve2 = c.Resolve<IRootType>("namedRegistration");

            // Assert
            Assert.AreSame(resolve1, resolve2);
        }


        [TestMethod]
        [TestCategory("Unit")]
        public void AnonymousInstanceResolvesToProvidedObject() {
            // Arrange
            var instance = new ConcreteTypeThree(28);
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeThree>().AsInstance(instance);
            instance.ChangeValue(55);

            // Act
            var resolve1 = c.Resolve<IRootType>();
            var resolve2 = c.Resolve<IRootType>();

            // Assert
            Assert.AreSame(resolve1, resolve2);
            Assert.AreSame(resolve1, instance);
            Assert.AreEqual(55, resolve1.GetFinalValue());
        }

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

        #region Nested type: Combine

        public class Combine : IRootType {
            private readonly IRootType _m1;
            private readonly IRootType _m2;

            public Combine(IRootType m1, IRootType m2) {
                _m1 = m1;
                _m2 = m2;
            }

            #region IRootType Members

            public int GetFinalValue() {
                return _m1.GetFinalValue() + _m2.GetFinalValue();
            }

            public void ChangeValue(int newValue) {
                throw new Exception("Pointless but purposeful disallowing of ChangeValue");
            }

            #endregion
        }

        #endregion

        #region Nested type: ConcreteTypeOne

        public class ConcreteTypeOne : IRootType {
            #region IRootType Members

            public int GetFinalValue() {
                return 0;
            }

            public void ChangeValue(int newValue) {
                throw new Exception("Pointless but purposeful disallowing of ChangeValue");
            }

            #endregion
        }

        #endregion

        #region Nested type: ConcreteTypeThree

        public class ConcreteTypeThree : IRootType {
            private int _internalValue;

            public ConcreteTypeThree(int internalValue) {
                _internalValue = internalValue;
            }

            #region IRootType Members

            public int GetFinalValue() {
                return _internalValue;
            }

            public void ChangeValue(int newValue) {
                _internalValue = newValue;
            }

            #endregion
        }

        #endregion

        #region Nested type: ConcreteTypeTwo

        public class ConcreteTypeTwo : IRootType {
            private readonly int _internalValue;

            public ConcreteTypeTwo(int internalValue) {
                _internalValue = internalValue;
            }

            #region IRootType Members

            public int GetFinalValue() {
                return _internalValue;
            }

            public void ChangeValue(int newValue) {
                throw new Exception("Pointless but purposeful disallowing of ChangeValue");
            }

            #endregion
        }

        #endregion

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

        #region Nested type: IDisplay

        public interface IDisplay {
            string Format(string format);
        }

        #endregion

        #region Nested type: IRootType

        public interface IRootType {
            int GetFinalValue();
            void ChangeValue(int newValue);
        }

        #endregion

        #region Nested type: NodeDisplay

        public class NodeDisplay : IDisplay {
            private readonly IRootType _nodeType;

            public NodeDisplay(IRootType nodeType) {
                _nodeType = nodeType;
            }

            #region IDisplay Members

            public string Format(string format) {
                return _nodeType.GetFinalValue().ToString(format);
            }

            #endregion
        }

        #endregion
    }
}