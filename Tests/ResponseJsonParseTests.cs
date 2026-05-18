using RimMind.Dialogue.Core;
using Xunit;

namespace RimMind.Dialogue.Tests
{
    public class ResponseJsonParseTests
    {
        [Fact]
        public void DialogueResponse_ParsesAllThreeFields()
        {
            var json = "{\"reply\":\"你好呀\",\"thought\":{\"tag\":\"CONNECTED\",\"description\":\"想与同伴亲近\"},\"relation_delta\":1}";

            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("你好呀", replyText);
            Assert.Equal("CONNECTED", thoughtTag);
            Assert.Equal("想与同伴亲近", thoughtDesc);
            Assert.Equal(1, relationDelta);
        }

        [Fact]
        public void MonologueResponse_IgnoresRelationDelta()
        {
            var json = "{\"reply\":\"好累啊……\",\"thought\":{\"tag\":\"STRESSED\",\"description\":\"感到疲惫\"},\"relation_delta\":1}";

            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson(json, true, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("好累啊……", replyText);
            Assert.Equal("STRESSED", thoughtTag);
            Assert.Equal("感到疲惫", thoughtDesc);
            Assert.Equal(0, relationDelta);
        }

        [Fact]
        public void MonologueResponse_NoRelationDeltaField()
        {
            var json = "{\"reply\":\"这矿洞真冷……\",\"thought\":{\"tag\":\"STRESSED\",\"description\":\"感到疲惫\"}}";

            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson(json, true, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("这矿洞真冷……", replyText);
            Assert.Equal("STRESSED", thoughtTag);
            Assert.Equal(0, relationDelta);
        }

        [Fact]
        public void ResponseNoThought()
        {
            var json = "{\"reply\":\"嗯。\"}";

            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("嗯。", replyText);
            Assert.Null(thoughtTag);
            Assert.Null(thoughtDesc);
            Assert.Equal(0, relationDelta);
        }

        [Fact]
        public void ResponseThoughtNone()
        {
            var json = "{\"reply\":\"早啊，有什么事吗？\",\"thought\":{\"tag\":\"NONE\",\"description\":\"平淡问候\"}}";

            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("早啊，有什么事吗？", replyText);
            Assert.Equal("NONE", thoughtTag);
            Assert.Equal("平淡问候", thoughtDesc);
            Assert.Equal(0, relationDelta);
        }

        [Fact]
        public void NonJsonString_ReturnsDefaults()
        {
            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson("just plain text", false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("", replyText);
            Assert.Null(thoughtTag);
            Assert.Null(thoughtDesc);
            Assert.Equal(0, relationDelta);
        }

        [Fact]
        public void EmptyAndNull_ReturnsDefaults()
        {
            string replyText = "orig";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson("", false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);
            Assert.Equal("orig", replyText);

            ResponseJsonParser.TryParseResponseJson(null, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);
            Assert.Equal("orig", replyText);
        }

        [Fact]
        public void DialogueNegativeRelationDelta()
        {
            var json = "{\"reply\":\"滚开！\",\"thought\":{\"tag\":\"IRRITATED\",\"description\":\"对方让你烦躁\"},\"relation_delta\":-3}";

            string replyText = "";
            string? thoughtTag = null;
            string? thoughtDesc = null;
            int relationDelta = 0;

            ResponseJsonParser.TryParseResponseJson(json, false, ref replyText, ref thoughtTag, ref thoughtDesc, ref relationDelta);

            Assert.Equal("滚开！", replyText);
            Assert.Equal("IRRITATED", thoughtTag);
            Assert.Equal(-3, relationDelta);
        }
    }
}
