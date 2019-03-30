﻿using System.Collections.Generic;
using TCC.TeraCommon.Game.Messages;
using TCC.TeraCommon.Game.Services;

namespace TCC.Parsing.Messages
{
    public class S_DUNGEON_COOL_TIME_LIST : ParsedMessage
    {
        public readonly Dictionary<uint, short> DungeonCooldowns;
        public S_DUNGEON_COOL_TIME_LIST(TeraMessageReader reader) : base(reader)
        {
            DungeonCooldowns = new Dictionary<uint, short>();

            try
            {
                var count = reader.ReadUInt16();
                reader.Skip(2); //var offset = reader.ReadUInt16();
                reader.Skip(4);
                for (var i = 0; i < count; i++)
                {
                    reader.Skip(2);
                    var next = reader.ReadUInt16();
                    var id = reader.ReadUInt32();
                    reader.Skip(10);
                    var entries = reader.ReadInt16();
                    DungeonCooldowns.Add(id, entries);
                    if (next == 0) return;
                    reader.RepositionAt(next);
                }

            }
            catch (System.Exception)
            {
                Log.F($"[S_DUNGEON_COOL_TIME_LIST] Failed to parse packet");
            }
        }
    }
}
