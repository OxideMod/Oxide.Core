using System;
using System.IO;
using Xunit;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the SaveInfo class.
    /// </summary>
    public class SaveInfoTests : IDisposable
    {
        private readonly string tempFile;
        private readonly string nonExistentFile;

        /// <summary>
        /// Initializes the test by ensuring the Oxide core is set up and creating a temporary file.
        /// </summary>
        public SaveInfoTests()
        {
            // Initialize the Oxide core so that libraries (like Time) are registered.
            Interface.Initialize();
            Interface.Oxide.Load();

            tempFile = Path.Combine(Path.GetTempPath(), "TestSaveInfo.txt");
            File.WriteAllText(tempFile, "dummy content");
            
            nonExistentFile = Path.Combine(Path.GetTempPath(), "NonExistentFile.txt");
            if (File.Exists(nonExistentFile))
                File.Delete(nonExistentFile);
        }

        /// <summary>
        /// Verifies that SaveInfo.Create returns a valid SaveInfo object for an existing file.
        /// </summary>
        [Fact]
        public void Create_ReturnsSaveInfo_ForExistingFile()
        {
            // Act
            var info = SaveInfo.Create(tempFile);

            // Assert
            Assert.NotNull(info);
            Assert.Equal(Path.GetFileNameWithoutExtension(tempFile), info.SaveName);
            Assert.True(info.CreationTimeUnix > 0);
        }
        
        /// <summary>
        /// Verifies that SaveInfo.Create returns null for a non-existing file.
        /// </summary>
        [Fact]
        public void Create_ReturnsNull_ForNonExistingFile()
        {
            // Act
            var info = SaveInfo.Create(nonExistentFile);
            
            // Assert
            Assert.Null(info);
        }

        /// <summary>
        /// Verifies that Refresh updates the creation time.
        /// </summary>
        [Fact]
        public void Refresh_UpdatesCreationTime()
        {
            // Arrange
            var info = SaveInfo.Create(tempFile);
            DateTime original = info.CreationTime;
            System.Threading.Thread.Sleep(1000);

            // Act: Change the file creation time.
            File.SetCreationTime(tempFile, DateTime.Now);
            info.Refresh();

            // Assert: The creation time should be updated.
            Assert.True(info.CreationTime > original);
        }
        
        /// <summary>
        /// Verifies that Refresh handles non-existent files gracefully.
        /// </summary>
        [Fact]
        public void Refresh_HandlesNonExistentFile()
        {
            // Arrange
            var info = SaveInfo.Create(tempFile);
            File.Delete(tempFile);
            DateTime original = info.CreationTime;
            
            // Act
            info.Refresh();
            
            // Assert: The creation time should not change
            Assert.Equal(original, info.CreationTime);
        }

        /// <summary>
        /// Cleans up the temporary file.
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            
            if (File.Exists(nonExistentFile))
                File.Delete(nonExistentFile);
        }
    }
}
