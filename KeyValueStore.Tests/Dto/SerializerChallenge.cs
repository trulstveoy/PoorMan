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
        public virtual Content Content { get; set; }
    }

    public class Content
    {
        public virtual string Text { get; set; }
        public virtual List<InnerContent> InnerContents { get; set; }
    }

    public class InnerContent
    {
        public virtual string Prop1 { get; set; }
        public virtual string Prop2 { get; set; }
    }
}
