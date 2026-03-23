using System;
using RevitGen.Attributes;
using Xunit;

namespace RevitGen.Tests
{
    /// <summary>
    /// Unit tests for <see cref="CommandHandlerAttribute"/>.
    /// </summary>
    public class CommandHandlerAttributeTests
    {
        [Fact]
        public void CanBeInstantiated()
        {
            var attr = new CommandHandlerAttribute();
            Assert.NotNull(attr);
        }

        [Fact]
        public void AttributeUsage_TargetsMethodsOnly()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(CommandHandlerAttribute), typeof(AttributeUsageAttribute))!;

            Assert.Equal(AttributeTargets.Method, usage.ValidOn);
            Assert.False(usage.Inherited);
            Assert.False(usage.AllowMultiple);
        }
    }
}
