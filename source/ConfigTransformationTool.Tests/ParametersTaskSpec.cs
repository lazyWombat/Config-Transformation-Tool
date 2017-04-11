// --------------------------------------------------------------------------------------------------------------------
// Outcold Solutions (http://outcoldman.com)
// --------------------------------------------------------------------------------------------------------------------

namespace OutcoldSolutions.ConfigTransformationTool.Suites
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using NUnit.Framework;

    [TestFixture]
    public class ParametersTaskSpec : BaseSpec
    {
        [Test]
        public void ApplyParameters_Sample()
        {
            const string expectedResult = @"
<value key=""Value CustomParameter1"" value=""False"" />
<value key=""Test2"" value=""Value CustomParameter2"" />
<value key=""Test3"" value=""False"" />";

            const string source = @"
<value key=""{CustomParameter1:Default value}"" value=""{TrueValueParameter:True}"" />
<value key=""Test2"" value=""{CustomParameter2:Default value of CustomParameter2}"" />
<value key=""Test3"" value=""{TrueValueParameter:True}"" />";

            var task = new ParametersTask();

            task.AddParameters(
                new Dictionary<string, string>
                    {
                        { "CustomParameter1", "Value CustomParameter1" },
                        { "TrueValueParameter", "False" },
                        { "CustomParameter2", "Value CustomParameter2" }
                    });
            var result = task.ApplyParameters(source);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void WithoutParameters()
        {
            const string source = @"
<value key=""{CustomParameter1}"" value=""{TrueValueParameter}"" />
<value key=""Test2"" value=""{CustomParameter2}"" />
<value key=""Test3"" value=""{TrueValueParameter}"" />";

            var task = new ParametersTask();
            var result = task.ApplyParameters(source);
            Assert.AreEqual(source, result);
        }

        [Test]
        public void WithoutParameters_But_With_Default_Values()
        {
            const string expectedResult = @"
<value key=""Default value"" value=""True"" />
<value key=""Test2"" value=""Default value of CustomParameter2"" />
<value key=""Test3"" value=""False"" />";

            const string source = @"
<value key=""{CustomParameter1:Default value}"" value=""{TrueValueParameter:True}"" />
<value key=""Test2"" value=""{CustomParameter2:Default value of CustomParameter2}"" />
<value key=""Test3"" value=""{TrueValueParameter:False}"" />";

            var task = new ParametersTask();
            var result = task.ApplyParameters(source);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Apply_With_Double_Colon_In_Definition()
        {
            const string expectedResult = @"
<value key=""Default:value"" value=""Val"" />";

            const string source = @"
<value key=""{Parameter1:Default:value}"" value=""Val"" />";

            var task = new ParametersTask();
            var result = task.ApplyParameters(source);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Apply_With_Escaped_Brackets()
        {
            const string expectedResult = @"
<value key=""Default:value"" value=""{TestParameter:Test}"" />";

            const string source = @"
<value key=""{Parameter1:Default:value}"" value=""\{TestParameter:Test\}"" />";

            var task = new ParametersTask();
            var result = task.ApplyParameters(source);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Apply_With_Escaped_Brackets_In_Default_Value()
        {
            const string expectedResult = @"
<value key=""Defa{ultva}lue"" value=""{TestParameter:Test}"" />";

            const string source = @"
<value key=""{Parameter1:Defa\{ultva\}lue}"" value=""\{TestParameter:Test\}"" />";

            var task = new ParametersTask();
            var result = task.ApplyParameters(source);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Apply_With_Parameter_At_End_Of_String()
        {
            const string expectedResult = @"
<value key=""Defa{ultva}lue"" value=""Test";

            const string source = @"
<value key=""{Parameter1:Defa\{ultva\}lue}"" value=""{TestParameter:Test}";

            var task = new ParametersTask();
            var result = task.ApplyParameters(source);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Apply_With_Parameter_At_Start_Of_String()
        {
            const string expectedResult = @"Defa{ultva}lue"" value=""{TestParameter:Test}"" />";

            const string source = @"{Parameter1:Defa\{ultva\}lue}"" value=""\{TestParameter:Test\}"" />";

            var task = new ParametersTask();
            var result = task.ApplyParameters(source);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Perfomance_Test()
        {
            var tester = new PerformanceTester(ApplyParameters_Sample);
            tester.MeasureExecTime(100000);
            Debug.Write(tester.TotalTime);
        }
    }
}