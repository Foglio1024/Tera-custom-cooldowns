﻿


namespace TeraPacketParser.Messages
{
    public class S_RESULT_ITEM_BIDDING : ParsedMessage
    {
        public S_RESULT_ITEM_BIDDING(TeraMessageReader reader) : base(reader)
        {
            //used only to end rolling phase, so not actually parsed
        }
    }
}
