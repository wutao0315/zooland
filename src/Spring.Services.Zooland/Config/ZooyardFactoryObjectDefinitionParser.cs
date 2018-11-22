#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

#region Imports

using System;
using System.Xml;

using Spring.Util;
using Spring.Objects.Factory.Xml;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Core.TypeResolution;
using Spring.Objects;
using static Spring.Objects.Factory.Config.ConstructorArgumentValues;

#endregion

namespace Spring.Services.Zooyard.Config
{
    /// <summary>
    /// The <see cref="IObjectDefinitionParser"/> for the <code>&lt;zy:zooyardFactory&gt;</code> tag.
    /// </summary>
    /// <author>Bruno Baia</author>
    public class ZooyardFactoryObjectDefinitionParser : ObjectsNamespaceParser, IObjectDefinitionParser
    {
        private static readonly string ZooyardTypeAttribute = "zooyardType";
        //private static readonly string EndpointConfigurationNameAttribute = "endpointConfigurationName";
        
        #region IObjectDefinitionParser Members

        /// <summary>
        /// Parse the specified XmlElement and register the resulting
        /// ObjectDefinitions with the <see cref="ParserContext.Registry"/> IObjectDefinitionRegistry
        /// embedded in the supplied <see cref="ParserContext"/>
        /// </summary>
        /// <param name="element">The element to be parsed.</param>
        /// <param name="parserContext">The object encapsulating the current state of the parsing process.
        /// Provides access to a IObjectDefinitionRegistry</param>
        /// <returns>The primary object definition.</returns>
        /// <remarks>
        /// <p>
        /// This method is never invoked if the parser is namespace aware
        /// and was called to process the root node.
        /// </p>
        /// </remarks>
        IObjectDefinition IObjectDefinitionParser.ParseElement(XmlElement element, ParserContext parserContext)
        {
            AssertUtils.ArgumentNotNull(parserContext, "parserContext");

            var id = element.GetAttribute(ObjectDefinitionConstants.IdAttribute);
            var unresolvedZooyardType = element.GetAttribute(ZooyardTypeAttribute);
            //var endpointConfigurationName = element.GetAttribute(EndpointConfigurationNameAttribute);

            IObjectDefinition zooyardFactoryDefinition;
            try
            {
                var zooyardType = TypeResolutionUtils.ResolveType(unresolvedZooyardType);
                var zooyardFactoryType = typeof(ZooyardFactoryObject<>).MakeGenericType(new Type[1] { zooyardType });
                zooyardFactoryDefinition = new RootObjectDefinition(zooyardFactoryType);
            }
            catch
            {
                // Try to resolve type later (Can be a type alias)
                zooyardFactoryDefinition = new RootObjectDefinition(
                    $"Spring.Services.Zooyard.ZooyardFactoryObject<{unresolvedZooyardType}>, Spring.Services.Zooyard", 
                    new ConstructorArgumentValues(), 
                    new MutablePropertyValues());
            }

            if (!StringUtils.HasText(id))
            {
                id = parserContext.ReaderContext.GenerateObjectName(zooyardFactoryDefinition);
            }

            var all = base.ParseConstructorArgSubElements(id, element, parserContext);

            zooyardFactoryDefinition.ConstructorArgumentValues.AddAll(all);

            //zooyardFactoryDefinition.ConstructorArgumentValues.AddNamedArgumentValue("endpointConfigurationName", endpointConfigurationName);

            foreach (PropertyValue pv in base.ParsePropertyElements(id, element, parserContext))
            {
                zooyardFactoryDefinition.PropertyValues.Add(pv);
            }

            parserContext.Registry.RegisterObjectDefinition(id, zooyardFactoryDefinition);

            return null;
        }

        #endregion
    }
}
