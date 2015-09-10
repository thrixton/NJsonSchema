﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class CSharpGeneratorTests
    {
        [TestMethod]
        public void When_namespace_is_set_then_it_should_appear_in_output()
        {
            //// Arrange
            var generator = CreateGenerator();
            generator.Namespace = "MyNamespace";
            
            //// Act
            var output = generator.GenerateFile();
            
            //// Assert
            Assert.IsTrue(output.Contains("namespace MyNamespace"));
            Assert.IsTrue(output.Contains("Dictionary<string, string>"));
        }

        [TestMethod]
        public void When_property_name_does_not_match_property_name_then_attribute_is_correct()
        {
            //// Arrange
            var generator = CreateGenerator();
            generator.Namespace = "MyNamespace";

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"[JsonProperty(""lastName"""));
            Assert.IsTrue(output.Contains(@"public string LastName"));
        }

        private static CSharpClassGenerator CreateGenerator()
        {
            var schema = JsonSchema4.FromType<Person>();
            var schemaData = schema.ToJson();
            var generator = new CSharpClassGenerator(schema);
            return generator;
        }
    }

    public class Person
    {
        [Required]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        public DateTime Birthday { get; set; }

        public Sex Sex { get; set; }
        
        public Address Address { get; set; }

        public List<string> Array { get; set; } 

        public Dictionary<string, string> Dictionary { get; set; } 
    }

    public class Address
    {
        public string Street { get; set; }

        public string City { get; set; }
    }

    public enum Sex
    {
        Male, 
        Female
    }
}