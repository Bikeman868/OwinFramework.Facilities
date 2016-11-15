using System;
using Moq.Modules;
using OwinFramework.Interfaces.Builder;

namespace UnitTests
{
    /// <summary>
    /// This mock of IConfiguration supplies default values for all configuration
    /// </summary>
    public class MockConfiguration: ConcreteImplementationProvider<IConfiguration>, IConfiguration, IDisposable
    {
        protected override IConfiguration GetImplementation(IMockProducer mockProducer)
        {
            return this;
        }

        public IDisposable Register<T>(string path, Action<T> onChangeAction, T defaultValue)
        {
            onChangeAction(defaultValue);
            return this;
        }

        public void Dispose()
        {
        }
    }
}
