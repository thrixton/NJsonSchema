//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Manages the generated types and converts JSON types to TypeScript types. </summary>
    public class TypeScriptTypeResolver : TypeResolverBase
    {
        /// <summary>Initializes a new instance of the <see cref="TypeScriptTypeResolver" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public TypeScriptTypeResolver(TypeScriptGeneratorSettings settings)
            : base(settings)
        {
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; }

        /// <summary>Gets or sets the namespace of the generated classes.</summary>
        public string Namespace { get; set; }

        /// <summary>Resolves and possibly generates the specified schema. Returns the type name with a 'I' prefix if the feature is supported for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
        public string ResolveConstructorInterfaceName(JsonSchema schema, bool isNullable, string typeNameHint)
        {
            return Resolve(schema, typeNameHint, true);
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
        public override string Resolve(JsonSchema schema, bool isNullable, string typeNameHint)
        {
            return Resolve(schema, typeNameHint, false);
        }

        /// <summary>Gets a value indicating whether the schema supports constructor conversion.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The result.</returns>
        public bool SupportsConstructorConversion(JsonSchema schema)
        {
            return schema?.ActualSchema.ResponsibleDiscriminatorObject == null;
        }

        /// <summary>Checks whether the given schema should generate a type.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>True if the schema should generate a type.</returns>
        protected override bool IsDefinitionTypeSchema(JsonSchema schema)
        {
            if (schema.IsDictionary && !Settings.InlineNamedDictionaries)
            {
                return true;
            }

            return base.IsDefinitionTypeSchema(schema);
        }

        private string Resolve(JsonSchema schema, string typeNameHint, bool addInterfacePrefix)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            schema = GetResolvableSchema(schema);

            // Primitive schemas (no new type)

            if (schema.ActualTypeSchema.IsAnyType &&
                schema.InheritedSchema == null && // not in inheritance hierarchy
                schema.AllOf.Count == 0 &&
                !Types.Keys.Contains(schema) &&
                !schema.HasReference)
            {
                return "any";
            }

            var type = schema.ActualTypeSchema.Type;
            if (type == JsonObjectType.None && schema.ActualTypeSchema.IsEnumeration)
            {
                type = schema.ActualTypeSchema.Enumeration.All(v => v is int) ?
                    JsonObjectType.Integer :
                    JsonObjectType.String;
            }

            if (type.HasFlag(JsonObjectType.Number))
            {
                return "number";
            }

            if (type.HasFlag(JsonObjectType.Integer) && !schema.ActualTypeSchema.IsEnumeration)
            {
                return ResolveInteger(schema.ActualTypeSchema, typeNameHint);
            }

            if (type.HasFlag(JsonObjectType.Boolean))
            {
                return "boolean";
            }

            if (type.HasFlag(JsonObjectType.String) && !schema.ActualTypeSchema.IsEnumeration)
            {
                return ResolveString(schema.ActualTypeSchema, typeNameHint);
            }

            if (schema.IsBinary)
            {
                return "any";
            }

            // Type generating schemas

            if (schema.ActualTypeSchema.IsEnumeration)
            {
                return GetOrGenerateTypeName(schema, typeNameHint);
            }

            if (schema.Type.HasFlag(JsonObjectType.Array))
            {
                return ResolveArrayOrTuple(schema, typeNameHint, addInterfacePrefix);
            }

            if (schema.IsDictionary)
            {
                var prefix = addInterfacePrefix &&
                    SupportsConstructorConversion(schema.AdditionalPropertiesSchema) &&
                    schema.AdditionalPropertiesSchema?.ActualSchema.Type.HasFlag(JsonObjectType.Object) == true ? "I" : "";

                var valueType = prefix + ResolveDictionaryValueType(schema, "any");

                var defaultType = "string";
                var keyType = Settings.TypeScriptVersion >= 2.1m ? prefix + ResolveDictionaryKeyType(schema, defaultType) : defaultType;
                if (keyType != defaultType)
                {
                    return $"{{ [key in keyof typeof {keyType}]: {valueType}; }}";
                }

                return $"{{ [key: {keyType}]: {valueType}; }}";
            }

            return (addInterfacePrefix && !schema.ActualTypeSchema.IsEnumeration && SupportsConstructorConversion(schema) ? "I" : "") +
                GetOrGenerateTypeName(schema, typeNameHint);
        }

        private string ResolveString(JsonSchema schema, string typeNameHint)
        {
            // TODO: Make this more generic (see DataConversionGenerator.IsDate)
            if (Settings.DateTimeType == TypeScriptDateTimeType.Date)
            {
                if (schema.Format == JsonFormatStrings.Date)
                {
                    return "Date";
                }

                if (schema.Format == JsonFormatStrings.DateTime)
                {
                    return "Date";
                }

                if (schema.Format == JsonFormatStrings.Time)
                {
                    return "string";
                }

                if (schema.Format == JsonFormatStrings.TimeSpan)
                {
                    return "string";
                }
            }
            else if (Settings.DateTimeType == TypeScriptDateTimeType.MomentJS ||
                     Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS)
            {
                if (schema.Format == JsonFormatStrings.Date)
                {
                    return "moment.Moment";
                }

                if (schema.Format == JsonFormatStrings.DateTime)
                {
                    return "moment.Moment";
                }

                if (schema.Format == JsonFormatStrings.Time)
                {
                    return "moment.Moment";
                }

                if (schema.Format == JsonFormatStrings.TimeSpan)
                {
                    return "moment.Duration";
                }
            }

            return "string";
        }

        private string ResolveInteger(JsonSchema schema, string typeNameHint)
        {
            return "number";
        }

        private string ResolveArrayOrTuple(JsonSchema schema, string typeNameHint, bool addInterfacePrefix)
        {
            if (schema.Item != null)
            {
                var isObject = schema.Item?.ActualSchema.Type.HasFlag(JsonObjectType.Object) == true;
                var isDictionary = schema.Item?.ActualSchema.IsDictionary == true;

                var prefix = addInterfacePrefix && SupportsConstructorConversion(schema.Item) && isObject && !isDictionary ? "I" : "";
                var itemType = prefix + Resolve(schema.Item, true, typeNameHint);

                return string.Format("{0}[]", GetNullableItemType(schema, itemType)); // TODO: Make typeNameHint singular if possible
            }

            if (schema.Items != null && schema.Items.Count > 0)
            {
                var tupleTypes = schema.Items
                    .Select(s => GetNullableItemType(s, Resolve(s, false, null)))
                    .ToArray();

                return string.Format("[" + string.Join(", ", tupleTypes) + "]");
            }

            return "any[]";
        }

        private string GetNullableItemType(JsonSchema schema, string itemType)
        {
            if (Settings.SupportsStrictNullChecks && schema.Item.IsNullable(Settings.SchemaType))
            {
                return string.Format("({0} | {1})", itemType, Settings.NullValue.ToString().ToLowerInvariant());
            }

            return itemType;
        }
    }
}