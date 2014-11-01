using System;
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
            var context = new Configuration(Constants.Connectionstring).Create();
            context.EnsureNewDatabase();

            var challenge = new SerializerChallenge {Text = "abc", Text2 = "cde"};

            var id = Guid.NewGuid();
            context.Create(id, challenge);

            var result = context.Read<SerializerChallenge>(id);
            Assert.IsNotNull(result);
            Assert.AreEqual("abc", result.Text);
            Assert.AreEqual("cde", result.Text2);
        }
    }
}