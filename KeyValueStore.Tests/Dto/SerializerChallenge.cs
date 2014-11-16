using System;
using System.Collections.Generic;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests.Dto
{
    public class SerializerChallengeBase
    {
        [Id]
        public virtual Guid Id { get; set; }
        public virtual string Text2 { get; set; }
        public virtual string NotWrite2 { get; private set; }
    }

    public class SerializerChallenge : SerializerChallengeBase
    {
        public virtual string Text { get; set; }
        public virtual string NoWrite { get; private set; }
        public virtual IList<string> NoWriteList { get; set; }
    }
}
