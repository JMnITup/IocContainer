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
        public void RegisteringClassResolvesCorrectly() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeThree>();

            // Act
            var resolve = c.Resolve<IRootType>();

            // Assert
            Assert.AreEqual(typeof(ConcreteTypeThree), resolve.GetType());
        }


        [TestMethod]
        [TestCategory("Unit")]
        public void ClearRegistrationClearsPreviouslyRegisteredClass() {
            // Arrange
            var c = new InterfaceResolver();
            c.Register<IRootType, ConcreteTypeThree>();
            c.ClearRegistrations();

            // Act
            var resolve = c.Resolve<IRootType>();

            // Assert
            Assert.AreEqual(typeof(ConcreteTypeThree), resolve.GetType());
            // TODO: This test should fail, because this assert should not be true.  This is a temporary commit prior to fixing that problem
            Assert.Fail();
        }

        /************************************************* Nested classes ***********************************************/

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