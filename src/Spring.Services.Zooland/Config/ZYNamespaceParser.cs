using Spring.Objects.Factory.Xml;

namespace Spring.Services.Zooyard.Config
{
    /// <summary>
    /// Namespace parser for the ZY namespace.
    /// </summary>
    /// <author>Bruno Baia</author>
    [
        NamespaceParser(
            Namespace = "http://www.springframework.net/zy",
            SchemaLocationAssemblyHint = typeof(ZYNamespaceParser),
            SchemaLocation = "/Spring.Services.Zooyard.Config/spring-zy-1.3.xsd")
    ]
    public sealed class ZYNamespaceParser : NamespaceParserSupport
    {
        private const string ZooyardFactoryElement = "zooyardFactory";
        private const string ServiceHostElement = "serviceHost";
        private const string ServiceExporterElement = "serviceExporter";

        /// <summary>
        /// Register the <see cref="IObjectDefinitionParser"/> for the ZY tags.
        /// </summary>
        public override void Init()
        {
            RegisterObjectDefinitionParser(ZooyardFactoryElement, new ZooyardFactoryObjectDefinitionParser());
        }
    }
}
