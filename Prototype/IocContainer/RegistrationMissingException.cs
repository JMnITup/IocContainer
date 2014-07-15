#region Using

using System;

#endregion

namespace OIM2.Core.IocContainer {
    public class RegistrationMissingException : Exception {
        public RegistrationMissingException(string message, Exception innerException) : base(message, innerException) {}
    }
}