using System;
using System.Collections.Generic;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class Serialization
    {
        [TestMethod]
        public void ChallengingType()
        {
            var context = new Configuration(Constants.Connectionstring).WithDocuments(typeof(SerializerChallenge)).Create();
            context.EnsureNewDatabase();

            var id = Guid.NewGuid();
            var challenge = new SerializerChallenge {Id = id,  Text = "abc", Text2 = "cde"};
            challenge.Content = new Content();
            challenge.Content.Text = "def";
            challenge.Content.InnerContents = new List<InnerContent>();
            challenge.Content.InnerContents.Add(new InnerContent {Prop1 = "a", Prop2 = "b"});
            
            context.Insert(challenge);

            var result = context.Read<SerializerChallenge>(id);
            Assert.IsNotNull(result);
            Assert.AreEqual("abc", result.Text);
            Assert.AreEqual("cde", result.Text2);
        }
    }
}