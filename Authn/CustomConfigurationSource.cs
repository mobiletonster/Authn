using Microsoft.Extensions.Configuration;

namespace Authn
{
    public class CustomConfigurationSource : IConfigurationSource
    {
        public CustomConfigurationSource() { }

        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new CustomConfigurationProvider();
    }
}
