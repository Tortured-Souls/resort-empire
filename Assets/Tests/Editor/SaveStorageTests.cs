using System.IO;
using NUnit.Framework;
using ResortEmpire.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace ResortEmpire.Tests
{
    /// <summary>Edit Mode tests for the local save file (SCRUM-87).</summary>
    public class SaveStorageTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), $"resort-empire-save-{System.Guid.NewGuid():N}.json");
        }

        [TearDown]
        public void TearDown()
        {
            SaveStorage.Delete(_path);
        }

        [Test]
        public void WrittenSave_IsReadBackUnchanged()
        {
            var saved = new SaveData { coins = 2500, gems = 7, energy = 3, completedWaves = 6 };
            saved.boughtPlots.Add(1);
            saved.buildings.Add(new SavedBuilding(5, 2));

            SaveStorage.Write(saved, _path);

            Assert.IsTrue(SaveStorage.Exists(_path));
            Assert.IsTrue(SaveStorage.TryRead(_path, out var loaded));
            Assert.AreEqual(2500, loaded.coins);
            Assert.AreEqual(7, loaded.gems);
            Assert.AreEqual(3, loaded.energy);
            Assert.AreEqual(6, loaded.completedWaves);
            CollectionAssert.AreEqual(new[] { 1 }, loaded.boughtPlots);
            Assert.AreEqual(5, loaded.buildings[0].spotIndex);
            Assert.AreEqual(2, loaded.buildings[0].level);
        }

        [Test]
        public void SecondWrite_ReplacesTheFirst()
        {
            SaveStorage.Write(new SaveData { coins = 100 }, _path);
            SaveStorage.Write(new SaveData { coins = 200 }, _path);

            Assert.IsTrue(SaveStorage.TryRead(_path, out var loaded));
            Assert.AreEqual(200, loaded.coins);
        }

        [Test]
        public void MissingSave_IsNotRead()
        {
            Assert.IsFalse(SaveStorage.Exists(_path));
            Assert.IsFalse(SaveStorage.TryRead(_path, out var loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void CorruptSave_IsNotRead()
        {
            File.WriteAllText(_path, "{ not json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("could not be read"));

            Assert.IsFalse(SaveStorage.TryRead(_path, out _));
        }

        [Test]
        public void DeletedSave_IsGone()
        {
            SaveStorage.Write(new SaveData(), _path);

            SaveStorage.Delete(_path);

            Assert.IsFalse(SaveStorage.Exists(_path));
        }
    }
}
