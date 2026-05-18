using RimMind.Dialogue.Core;
using Xunit;

namespace RimMind.Dialogue.Tests
{
    public class NpcResponseHandlerParseTests
    {
        [Fact]
        public void Parse_ValidJson_ExtractsReply()
        {
            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            string json = "{\"reply\":\"Hello there\",\"thought\":{\"tag\":\"happy\",\"description\":\"Feeling good\"},\"relation_delta\":5}";

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("Hello there", replyText);
            Assert.Equal("happy", thoughtTag);
            Assert.Equal("Feeling good", thoughtDesc);
            Assert.Equal(5, relationDelta);
        }

        [Fact]
        public void Parse_ReplyOnly()
        {
            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            string json = "{\"reply\":\"Just a simple reply\"}";

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("Just a simple reply", replyText);
            Assert.Null(thoughtTag);
            Assert.Equal(0, relationDelta);
        }

        [Fact]
        public void Parse_Monologue_NoRelationDelta()
        {
            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            string json = "{\"reply\":\"Self talk\",\"relation_delta\":10}";

            ResponseJsonParser.TryParseResponseJson(json, true, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("Self talk", replyText);
            Assert.Equal(0, relationDelta);
        }

        [Fact]
        public void Parse_NonDialogue_RelationDeltaParsed()
        {
            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            string json = "{\"reply\":\"Nice to meet you\",\"relation_delta\":3}";

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal(3, relationDelta);
        }

        [Fact]
        public void Parse_PlainText_NotStartingWithBrace_NoChange()
        {
            string replyText = "original text";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson("plain text response", false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("original text", replyText);
            Assert.Null(thoughtTag);
            Assert.Equal(0, relationDelta);
        }

        [Fact]
        public void Parse_NullInput_NoChange()
        {
            string replyText = "original";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson(null, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("original", replyText);
        }

        [Fact]
        public void Parse_EmptyReplyInJson_KeepsOriginal()
        {
            string replyText = "original";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            string json = "{\"reply\":\"\",\"thought\":{\"tag\":\"sad\"}}";

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("original", replyText);
            Assert.Equal("sad", thoughtTag);
        }

        [Fact]
        public void Parse_InvalidJson_KeepsOriginal()
        {
            string replyText = "original";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson("{invalid json", false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("original", replyText);
        }

        [Fact]
        public void Parse_EmptyString_NoChange()
        {
            string replyText = "original";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson("", false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("original", replyText);
        }

        [Fact]
        public void Parse_NegativeRelationDelta()
        {
            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            string json = "{\"reply\":\"Go away!\",\"relation_delta\":-3}";

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal(-3, relationDelta);
        }
    }
}
