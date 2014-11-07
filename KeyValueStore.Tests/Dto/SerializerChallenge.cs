using System;
using System.Collections.Generic;
using PoorMan.KeyValueStore.Annotation;

namespace KeyValueStore.Tests.Dto
{
    public class SerializerChallengeBase
    {
        [Id]
        public Guid Id { get; set; }
        public string Text2 { get; set; }
        public string NotWrite2 { get; private set; }
    }

    public class SerializerChallenge : SerializerChallengeBase
    {
        public string Text { get; set; }
        public string NoWrite { get; private set; }
        public IList<string> NoWriteList { get; set; }
    }
}
