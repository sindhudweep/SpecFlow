﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Gherkin;
using NUnit.Framework;
using TechTalk.SpecFlow.Parser;
using TechTalk.SpecFlow.Parser.Compatibility;
using TechTalk.SpecFlow.Parser.SyntaxElements;

namespace TechTalk.SpecFlow.Specs.Drivers.Parser
{
    public class ParserDriver
    {
        public string FileContent { get; set; }
        public SpecFlowFeature ParsedFeature { get; private set; }
        public ParserException[] ParsingErrors { get; private set; }

        private readonly SpecFlowGherkinParser parser = new SpecFlowGherkinParser(new CultureInfo("en-US"));

        public void ParseFile()
        {
            var contentReader = new StringReader(FileContent);
            ParsedFeature = null;
            ParsingErrors = new ParserException[0];

            try
            {
                ParsedFeature = parser.Parse(contentReader, "sample.feature");
                Assert.IsNotNull(ParsedFeature);
            }
            catch (ParserException ex)
            {
                ParsingErrors = ex.GetParserExceptions();
                Console.WriteLine("-> parsing errors");
                foreach (var error in ParsingErrors)
                {
                    Console.WriteLine("-> {0}:{1} {2}", error.Location == null ? 0 : error.Location.Line, error.Location == null ? 0 : error.Location.Column, error.Message);
                }
            }
        }

        public void AssertParsedFeatureEqualTo(string parsedFeatureXml)
        {
            const string NS1 = "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"";
            const string NS2 = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";

            string expected = parsedFeatureXml.Replace("\r", "").Replace(NS1, "").Replace(NS2, "");
            string got = SerializeFeature(ParsedFeature).Replace("\r", "").Replace(NS1, "").Replace(NS2, "");

            Assert.AreEqual(expected, got);
        }

        public void AssertErrors(List<ExpectedError> expectedErrors)
        {
            Assert.Greater(expectedErrors.Count, 0, "please specify expected errors");

            CollectionAssert.IsNotEmpty(ParsingErrors, "The parsing was successful");

            foreach (var expectedError in expectedErrors)
            {
                string message = expectedError.Error.ToLower();

                var errorDetail =
                    ParsingErrors.FirstOrDefault(ed => ed.Location != null && ed.Location.Line == expectedError.Line &&
                        ed.Message.ToLower().Contains(message));

                Assert.IsNotNull(errorDetail, "no such error: {0}", message);
            }
        }

        private void SerializeFeature(SpecFlowFeature feature, TextWriter writer)
        {
            var oldFeature = CompatibleAstConverter.ConvertToCompatibleFeature(feature);
            oldFeature.SourceFile = null;
            XmlSerializer serializer = new XmlSerializer(typeof(Feature));
            serializer.Serialize(writer, oldFeature);
        }

        public void SaveSerializedFeatureTo(string fileName)
        {
            Assert.IsNotNull(ParsedFeature, "The parsing was not successful");
            SerializeFeature(ParsedFeature, fileName);
        }

        private void SerializeFeature(SpecFlowFeature feature, string fileName)
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                SerializeFeature(feature, writer);
            }
        }

        private string SerializeFeature(SpecFlowFeature feature)
        {
            using (var writer = new Utf8StringWriter())
            {
                SerializeFeature(feature, writer);
                return writer.ToString();
            }
        }
    }

    public class ExpectedError
    {
        public int? Line { get; set; }
        public string Error { get; set; }
    }
}
