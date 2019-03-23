using STEP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IFC.Tests
{
    public class STEPTests
    {
        private readonly ITestOutputHelper output;

        public STEPTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("../../../models/example_1.ifc", 335, 0)]
        [InlineData("../../../models/example_2.ifc", 250482, 0)]
        [InlineData("../../../models/example_3.ifc", 234654, 0)]
        [InlineData("../../../models/Regression_49.ifc", 2, 0)]
        [InlineData("../../../models/Regression_50.ifc", 5, 0)]
        public void DeserializeFromSTEP(string modelPath, int expectedInstanceCount, int expectedErrorCount)
        {
            List<STEPError> errors;
            var model = new Document(modelPath, out errors);
            ReportErrors(modelPath, errors);
            Assert.Equal(expectedErrorCount, errors.Count);
            Assert.Equal(expectedInstanceCount, model.AllEntities.Count());

            // Serialize the model to STEP.
            errors.Clear();
            var outputPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(modelPath));
            output.WriteLine($"Exporting IFC to {outputPath}.");
            File.WriteAllText(outputPath, model.ToSTEP(outputPath));
             
            ReportErrors(outputPath, errors);
            Assert.Equal(0, errors.Count);

            // Reload the new version of the model
            errors.Clear();
            var newModel = new Document(outputPath, out errors);
            Assert.Equal(expectedInstanceCount, newModel.AllEntities.Count());
            Assert.Equal(0, errors.Count);
        }

        private void ReportErrors(string filePath, IEnumerable<STEPError> errors)
        {
            if (!errors.Any())
            {
                return;
            }

            Console.WriteLine($"The following errors occurred while parsing {filePath}:");
            foreach (var e in errors)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
