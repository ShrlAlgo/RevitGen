using System;
using RevitGen.Attributes;
using Xunit;

namespace RevitGen.Tests
{
    /// <summary>
    /// Unit tests for <see cref="RevitCommandAttribute"/>.
    /// </summary>
    public class RevitCommandAttributeTests
    {
        [Fact]
        public void Constructor_WithValidText_SetsTextProperty()
        {
            var attr = new RevitCommandAttribute("My Command");
            Assert.Equal("My Command", attr.Text);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyOrWhitespaceText_ThrowsArgumentNullException(string text)
        {
            Assert.Throws<ArgumentNullException>(() => new RevitCommandAttribute(text));
        }

        [Fact]
        public void Constructor_WithNullText_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RevitCommandAttribute(null!));
        }

        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var attr = new RevitCommandAttribute("Test");

            Assert.Equal("RevitGen", attr.TabName);
            Assert.Equal("Commands", attr.PanelName);
            Assert.Equal("", attr.Icon);
            Assert.Equal("", attr.ToolTip);
            // Transactions are enabled by default to match source generator behavior
            Assert.True(attr.UsingTransaction);
        }

        [Fact]
        public void Properties_CanBeSet()
        {
            var attr = new RevitCommandAttribute("Test")
            {
                TabName = "MyTab",
                PanelName = "MyPanel",
                Icon = "icon.png",
                ToolTip = "My tooltip",
                UsingTransaction = true
            };

            Assert.Equal("MyTab", attr.TabName);
            Assert.Equal("MyPanel", attr.PanelName);
            Assert.Equal("icon.png", attr.Icon);
            Assert.Equal("My tooltip", attr.ToolTip);
            Assert.True(attr.UsingTransaction);
        }

        [Fact]
        public void AttributeUsage_IsCorrectlyDefined()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(RevitCommandAttribute), typeof(AttributeUsageAttribute))!;

            Assert.Equal(AttributeTargets.Class, usage.ValidOn);
            Assert.False(usage.Inherited);
            Assert.False(usage.AllowMultiple);
        }
    }
}
